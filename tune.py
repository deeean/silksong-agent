import json
import os
from pathlib import Path
from typing import Any, Dict

from dotenv import load_dotenv

load_dotenv(Path(__file__).parent / ".env")

import optuna
from optuna.pruners import MedianPruner
from optuna.samplers import TPESampler
import torch
import torch.nn as nn
from stable_baselines3 import PPO
from stable_baselines3.common.callbacks import EvalCallback
from stable_baselines3.common.vec_env import VecNormalize

from train import create_vec_env, reset_env_id_counter
from silksong import MultiHeadFeatureExtractor


def get_hyperparameters(trial: optuna.Trial) -> Dict[str, Any]:
    learning_rate = trial.suggest_float("learning_rate", 1e-5, 1e-3, log=True)
    n_steps = trial.suggest_categorical("n_steps", [512, 1024, 2048, 4096])
    batch_size = trial.suggest_categorical("batch_size", [64, 128, 256, 512])
    n_epochs = trial.suggest_int("n_epochs", 3, 10)
    gamma = trial.suggest_float("gamma", 0.95, 0.999)
    gae_lambda = trial.suggest_float("gae_lambda", 0.9, 0.99)
    clip_range = trial.suggest_float("clip_range", 0.1, 0.3)
    ent_coef = trial.suggest_float("ent_coef", 0.001, 0.1, log=True)
    vf_coef = trial.suggest_float("vf_coef", 0.3, 0.7)
    max_grad_norm = trial.suggest_float("max_grad_norm", 0.3, 1.0)
    features_dim = trial.suggest_categorical("features_dim", [128, 256, 512])
    pi_layers = trial.suggest_categorical("pi_layers", [64, 128, 256])
    vf_layers = trial.suggest_categorical("vf_layers", [64, 128, 256])

    return {
        "learning_rate": learning_rate,
        "n_steps": n_steps,
        "batch_size": batch_size,
        "n_epochs": n_epochs,
        "gamma": gamma,
        "gae_lambda": gae_lambda,
        "clip_range": clip_range,
        "ent_coef": ent_coef,
        "vf_coef": vf_coef,
        "max_grad_norm": max_grad_norm,
        "features_dim": features_dim,
        "pi_layers": pi_layers,
        "vf_layers": vf_layers,
    }


class TrialEvalCallback(EvalCallback):
    """Callback for evaluating and pruning trials."""

    def __init__(self, trial: optuna.Trial, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.trial = trial
        self.eval_idx = 0
        self.is_pruned = False

    def _on_step(self) -> bool:
        result = super()._on_step()

        if self.eval_idx > 0 and self.last_mean_reward is not None:
            self.trial.report(self.last_mean_reward, self.eval_idx)

            if self.trial.should_prune():
                self.is_pruned = True
                return False

        return result

    def _on_event(self) -> None:
        super()._on_event()
        self.eval_idx += 1


def objective(
    trial: optuna.Trial,
    n_envs: int,
    timesteps_per_trial: int,
    eval_freq: int,
    n_eval_episodes: int,
    time_scale: float,
) -> float:
    """Optuna objective function."""

    reset_env_id_counter()
    params = get_hyperparameters(trial)

    print(f"\n{'='*60}")
    print(f"Trial {trial.number}")
    print(f"{'='*60}")
    for key, value in params.items():
        print(f"  {key}: {value}")
    print(f"{'='*60}\n")

    # Create environments
    env = create_vec_env(n_envs=n_envs, time_scale=time_scale, nofx=True)
    env = VecNormalize(env, norm_obs=False, norm_reward=True)

    eval_env = create_vec_env(n_envs=1, time_scale=time_scale, nofx=True)
    eval_env = VecNormalize(eval_env, norm_obs=False, norm_reward=False, training=False)

    # Policy kwargs
    policy_kwargs = dict(
        features_extractor_class=MultiHeadFeatureExtractor,
        features_extractor_kwargs=dict(features_dim=params["features_dim"]),
        net_arch=dict(
            pi=[params["pi_layers"]],
            vf=[params["vf_layers"]],
        ),
        activation_fn=nn.ReLU,
    )

    model = PPO(
        policy="MlpPolicy",
        env=env,
        learning_rate=params["learning_rate"],
        n_steps=params["n_steps"],
        batch_size=params["batch_size"],
        n_epochs=params["n_epochs"],
        gamma=params["gamma"],
        gae_lambda=params["gae_lambda"],
        clip_range=params["clip_range"],
        ent_coef=params["ent_coef"],
        vf_coef=params["vf_coef"],
        max_grad_norm=params["max_grad_norm"],
        verbose=0,
        device="cpu",
        policy_kwargs=policy_kwargs,
    )

    eval_callback = TrialEvalCallback(
        trial=trial,
        eval_env=eval_env,
        n_eval_episodes=n_eval_episodes,
        eval_freq=eval_freq,
        deterministic=True,
        verbose=0,
    )

    try:
        model.learn(
            total_timesteps=timesteps_per_trial,
            callback=eval_callback,
            progress_bar=True,
        )
    except Exception as e:
        print(f"Trial {trial.number} failed: {e}")
        env.close()
        eval_env.close()
        raise optuna.TrialPruned()

    env.close()
    eval_env.close()

    if eval_callback.is_pruned:
        raise optuna.TrialPruned()

    mean_reward = eval_callback.best_mean_reward
    print(f"\nTrial {trial.number} finished with mean reward: {mean_reward:.4f}")

    return mean_reward


def tune(
    n_trials: int = 30,
    n_envs: int = 1,
    timesteps_per_trial: int = 200_000,
    eval_freq: int = 50_000,
    n_eval_episodes: int = 5,
    time_scale: float = 4.0,
    study_name: str = "ppo_silksong",
    storage: str = None,
    output_dir: str = "./hyperparameters",
):
    os.makedirs(output_dir, exist_ok=True)

    sampler = TPESampler(n_startup_trials=5, seed=42)
    pruner = MedianPruner(n_startup_trials=5, n_warmup_steps=2)

    study = optuna.create_study(
        study_name=study_name,
        storage=storage,
        sampler=sampler,
        pruner=pruner,
        direction="maximize",
        load_if_exists=True,
    )

    print(f"\n{'='*60}")
    print("HYPERPARAMETER TUNING")
    print(f"{'='*60}")
    print(f"Study name: {study_name}")
    print(f"Number of trials: {n_trials}")
    print(f"Timesteps per trial: {timesteps_per_trial:,}")
    print(f"Parallel environments: {n_envs}")
    print(f"Time scale: {time_scale}")
    print(f"{'='*60}\n")

    try:
        study.optimize(
            lambda trial: objective(
                trial,
                n_envs=n_envs,
                timesteps_per_trial=timesteps_per_trial,
                eval_freq=eval_freq,
                n_eval_episodes=n_eval_episodes,
                time_scale=time_scale,
            ),
            n_trials=n_trials,
            show_progress_bar=True,
        )
    except KeyboardInterrupt:
        print("\n\nTuning interrupted by user.")

    # Print results
    print(f"\n{'='*60}")
    print("TUNING RESULTS")
    print(f"{'='*60}")
    print(f"Number of finished trials: {len(study.trials)}")

    if len(study.trials) > 0:
        print(f"\nBest trial:")
        best_trial = study.best_trial
        print(f"  Value (mean reward): {best_trial.value:.4f}")
        print(f"  Params:")
        for key, value in best_trial.params.items():
            print(f"    {key}: {value}")

        # Save best params
        best_params_path = os.path.join(output_dir, "best_params.json")
        with open(best_params_path, "w") as f:
            json.dump(best_trial.params, f, indent=2)
        print(f"\nBest params saved to: {best_params_path}")

        # Save all trials
        trials_path = os.path.join(output_dir, "all_trials.json")
        trials_data = []
        for trial in study.trials:
            if trial.state == optuna.trial.TrialState.COMPLETE:
                trials_data.append({
                    "number": trial.number,
                    "value": trial.value,
                    "params": trial.params,
                })
        with open(trials_path, "w") as f:
            json.dump(trials_data, f, indent=2)
        print(f"All trials saved to: {trials_path}")

    print(f"{'='*60}\n")

    return study


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Hyperparameter tuning for PPO")
    parser.add_argument("--n_trials", type=int, default=30, help="Number of trials")
    parser.add_argument("--n_envs", type=int, default=1, help="Number of parallel environments")
    parser.add_argument("--timesteps", type=int, default=200_000, help="Timesteps per trial")
    parser.add_argument("--eval_freq", type=int, default=50_000, help="Evaluation frequency")
    parser.add_argument("--n_eval_episodes", type=int, default=10, help="Episodes per evaluation")
    parser.add_argument("--time_scale", type=float, default=4.0)
    parser.add_argument("--study_name", type=str, default="ppo_silksong")
    parser.add_argument("--storage", type=str, default=None, help="Optuna storage URL (e.g., sqlite:///study.db)")
    parser.add_argument("--output_dir", type=str, default="./hyperparameters")

    args = parser.parse_args()

    tune(
        n_trials=args.n_trials,
        n_envs=args.n_envs,
        timesteps_per_trial=args.timesteps,
        eval_freq=args.eval_freq,
        n_eval_episodes=args.n_eval_episodes,
        time_scale=args.time_scale,
        study_name=args.study_name,
        storage=args.storage,
        output_dir=args.output_dir,
    )
