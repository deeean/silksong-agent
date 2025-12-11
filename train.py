import os
from functools import partial
from pathlib import Path
from typing import Union

from dotenv import load_dotenv

load_dotenv(Path(__file__).parent / ".env")

import numpy as np
import torch
import torch.nn as nn
from sb3_contrib import RecurrentPPO
from stable_baselines3.common.callbacks import CheckpointCallback
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.vec_env import DummyVecEnv, SubprocVecEnv, VecNormalize

from silksong import SilksongBossEnv, MultiHeadFeatureExtractor, TensorboardCallback


def _make_env(env_id: int, time_scale: float = 1.0):
    env = SilksongBossEnv(env_id, time_scale=time_scale)
    env = Monitor(env)
    return env


def create_vec_env(n_envs: int = 1, time_scale: float = 1.0):
    if n_envs < 1 or n_envs > 4:
        raise ValueError(f"n_envs must be between 1 and 4, got {n_envs}")

    env_fns = [partial(_make_env, env_id=i+1, time_scale=time_scale) for i in range(n_envs)]

    if n_envs > 1:
        return SubprocVecEnv(env_fns, start_method='spawn')
    else:
        return DummyVecEnv(env_fns)


def train(
    total_timesteps: int = 10_000_000,
    learning_rate: float = 3e-4,
    n_steps: int = 2048,
    batch_size: int = 256,
    n_epochs: int = 5,
    gamma: float = 0.99,
    gae_lambda: float = 0.95,
    clip_range: float = 0.1,
    ent_coef: float = 0.05,
    vf_coef: float = 0.5,
    max_grad_norm: float = 0.3,
    log_dir: str = "./logs",
    save_dir: str = "./models",
    checkpoint_path: str = None,
    n_envs: int = 1,
    time_scale: float = 4.0,
    device: Union[torch.device, str] = "cuda" if torch.cuda.is_available() else "cpu",
):
    resuming = checkpoint_path and os.path.exists(checkpoint_path)

    os.makedirs(log_dir, exist_ok=True)
    os.makedirs(save_dir, exist_ok=True)

    if resuming:
        print("\n" + "=" * 60)
        print("RESUMING TRAINING FROM CHECKPOINT")
        print("=" * 60)
        print(f"Checkpoint: {checkpoint_path}")
    else:
        print("\n" + "=" * 60)
        print("STARTING NEW TRAINING")
        print("=" * 60)

    print(f"Total timesteps: {total_timesteps:,}")
    print(f"Learning rate: {learning_rate}")
    print(f"Parallel environments: {n_envs}")
    print(f"Time scale: {time_scale}")
    print(f"Log directory: {log_dir}")
    print(f"Save directory: {save_dir}")
    print("=" * 60)

    print(f"\nLaunching {n_envs} game instance(s)...")
    env = create_vec_env(n_envs=n_envs, time_scale=time_scale)

    vecnormalize_path = checkpoint_path.replace(".zip", "_vecnormalize.pkl") if checkpoint_path else None
    if resuming and vecnormalize_path and os.path.exists(vecnormalize_path):
        print(f"Loading VecNormalize from: {vecnormalize_path}")
        env = VecNormalize.load(vecnormalize_path, env)
    else:
        env = VecNormalize(env, norm_obs=False, norm_reward=True)

    policy_kwargs = dict(
        features_extractor_class=MultiHeadFeatureExtractor,
        features_extractor_kwargs=dict(features_dim=128),
        lstm_hidden_size=128,
        n_lstm_layers=1,
        shared_lstm=False,
        enable_critic_lstm=True,
        net_arch=dict(pi=[128], vf=[128]),
        activation_fn=nn.ReLU,
    )

    if resuming:
        print(f"\nLoading model from checkpoint: {checkpoint_path}")
        model = RecurrentPPO.load(
            checkpoint_path,
            env=env,
            learning_rate=learning_rate,
            n_steps=n_steps,
            batch_size=batch_size,
            n_epochs=n_epochs,
            gamma=gamma,
            gae_lambda=gae_lambda,
            clip_range=clip_range,
            ent_coef=ent_coef,
            vf_coef=vf_coef,
            max_grad_norm=max_grad_norm,
            verbose=1,
            tensorboard_log=log_dir,
            device=device,
        )
    else:
        print("\nInitializing new RecurrentPPO model...")
        model = RecurrentPPO(
            policy="MlpLstmPolicy",
            env=env,
            learning_rate=learning_rate,
            n_steps=n_steps,
            batch_size=batch_size,
            n_epochs=n_epochs,
            gamma=gamma,
            gae_lambda=gae_lambda,
            clip_range=clip_range,
            ent_coef=ent_coef,
            vf_coef=vf_coef,
            max_grad_norm=max_grad_norm,
            verbose=1,
            tensorboard_log=log_dir,
            device=device,
            policy_kwargs=policy_kwargs,
        )

    print(f"Using device: {model.device}")
    print(f"\nModel architecture:")
    print(model.policy)

    checkpoint_callback = CheckpointCallback(
        save_freq=int(n_steps / 2),
        save_path=save_dir,
        name_prefix="rl_model",
        save_vecnormalize=True,
    )
    tensorboard_callback = TensorboardCallback()

    print("\n" + "=" * 60)
    print("Starting training...")
    print(f"  tensorboard --logdir {log_dir}")
    print("=" * 60 + "\n")

    try:
        model.learn(
            total_timesteps=total_timesteps,
            callback=[checkpoint_callback, tensorboard_callback],
            progress_bar=True,
        )

        final_model_path = os.path.join(save_dir, "rl_model_final")
        model.save(final_model_path)
        env.save(os.path.join(save_dir, "vecnormalize_final.pkl"))

        print("\n" + "=" * 60)
        print("Training completed!")
        print(f"Final model saved to: {final_model_path}")
        print("=" * 60)

    except KeyboardInterrupt:
        print("\n\nTraining interrupted by user.")
        interrupt_model_path = os.path.join(save_dir, "rl_model_interrupted")
        model.save(interrupt_model_path)
        env.save(os.path.join(save_dir, "vecnormalize_interrupted.pkl"))
        print(f"Model saved to: {interrupt_model_path}")

    finally:
        env.close()


def evaluate(model_path: str, n_episodes: int = 10, time_scale: float = 1.0):
    print(f"\nEvaluating model: {model_path}")
    print(f"Time scale: {time_scale}")

    env = DummyVecEnv([partial(_make_env, env_id=1, time_scale=time_scale)])

    vecnormalize_path = model_path.replace(".zip", "_vecnormalize.pkl")
    if os.path.exists(vecnormalize_path):
        print(f"Loading VecNormalize from: {vecnormalize_path}")
        env = VecNormalize.load(vecnormalize_path, env)
        env.training = False
        env.norm_reward = False
    else:
        env = VecNormalize(env, norm_obs=False, norm_reward=False, training=False)

    print(f"Loading RecurrentPPO model from: {model_path}")
    model = RecurrentPPO.load(model_path, env=env)
    print("Model loaded successfully!")

    episode_rewards = []
    episode_lengths = []

    for episode in range(n_episodes):
        obs = env.reset()
        done = False
        episode_reward = 0
        episode_length = 0

        lstm_states = None
        episode_start = np.ones((1,), dtype=bool)

        while not done:
            action, lstm_states = model.predict(
                obs,
                state=lstm_states,
                episode_start=episode_start,
                deterministic=True
            )
            obs, reward, done, info = env.step(action)
            episode_reward += reward[0]
            episode_length += 1
            episode_start = done

        episode_rewards.append(episode_reward)
        episode_lengths.append(episode_length)
        print(f"Episode {episode + 1}/{n_episodes}: Reward = {episode_reward:.2f}, Length = {episode_length}")

    env.close()

    print("\n" + "=" * 60)
    print("Evaluation Results:")
    print(f"Mean reward: {np.mean(episode_rewards):.2f} +/- {np.std(episode_rewards):.2f}")
    print(f"Mean length: {np.mean(episode_lengths):.2f} +/- {np.std(episode_lengths):.2f}")
    print("=" * 60)


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--eval", action="store_true")
    parser.add_argument("--checkpoint", type=str)
    parser.add_argument("--n_envs", type=int, default=1, choices=[1, 2, 3, 4])

    args = parser.parse_args()

    if args.eval:
        if not args.checkpoint:
            parser.error("--eval requires --checkpoint")
        evaluate(args.checkpoint, n_episodes=10, time_scale=1.0)
    else:
        train(
            total_timesteps=10_000_000,
            learning_rate=1e-4,
            n_steps=8192,
            batch_size=4096,
            n_epochs=4,
            gamma=0.99,
            gae_lambda=0.95,
            clip_range=0.1,
            ent_coef=0.01,
            vf_coef=0.5,
            max_grad_norm=0.3,
            checkpoint_path=args.checkpoint,
            device="cuda",
            n_envs=args.n_envs,
            time_scale=4.0,
        )
