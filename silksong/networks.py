from collections import deque

import numpy as np
import torch
import torch.nn as nn
from stable_baselines3.common.callbacks import BaseCallback
from stable_baselines3.common.torch_layers import BaseFeaturesExtractor

from silksong.constants import STATE_DIM, RAYCAST_DIM


class MultiHeadFeatureExtractor(BaseFeaturesExtractor):
    def __init__(self, observation_space, features_dim: int = 256):
        super().__init__(observation_space, features_dim)

        self.state_branch = nn.Sequential(
            nn.Linear(STATE_DIM, 128),
            nn.ReLU(),
            nn.LayerNorm(128),
            nn.Linear(128, 128),
            nn.ReLU(),
        )

        self.raycast_branch = nn.Sequential(
            nn.Linear(RAYCAST_DIM, 128),
            nn.ReLU(),
            nn.LayerNorm(128),
            nn.Linear(128, 128),
            nn.ReLU(),
        )

        combined_dim = 256
        self.combined = nn.Sequential(
            nn.Linear(combined_dim, 256),
            nn.ReLU(),
            nn.LayerNorm(256),
            nn.Linear(256, features_dim),
            nn.ReLU(),
        )

    def forward(self, observations: torch.Tensor) -> torch.Tensor:
        state_obs = observations[:, :STATE_DIM]
        raycast_obs = observations[:, STATE_DIM:]

        state_features = self.state_branch(state_obs)
        raycast_features = self.raycast_branch(raycast_obs)

        combined = torch.cat([state_features, raycast_features], dim=-1)
        return self.combined(combined)


class TensorboardCallback(BaseCallback):
    def __init__(self, verbose=0, buffer_size: int = 100):
        super().__init__(verbose)
        self.highest_reward = float('-inf')
        self.buffer_size = buffer_size
        self.attack_counts = deque(maxlen=buffer_size)
        self.hurt_counts = deque(maxlen=buffer_size)
        self.lowest_boss_hps = deque(maxlen=buffer_size)

    def _on_step(self) -> bool:
        for i, info in enumerate(self.locals.get("infos", [])):
            if "episode_reward" in info:
                episode_reward = info["episode_reward"]
                lowest_boss_hp = info["lowest_boss_hp"]
                attack_count = info["attack_count"]
                hurt_count = info["hurt_count"]

                if episode_reward > self.highest_reward:
                    self.highest_reward = episode_reward

                self.attack_counts.append(attack_count)
                self.hurt_counts.append(hurt_count)
                self.lowest_boss_hps.append(lowest_boss_hp)

                self.logger.record("episode/attack_count_mean", np.mean(self.attack_counts))
                self.logger.record("episode/hurt_count_mean", np.mean(self.hurt_counts))
                self.logger.record("episode/lowest_boss_hp_mean", np.mean(self.lowest_boss_hps))
                self.logger.record("episode/highest_reward", self.highest_reward)

        return True
