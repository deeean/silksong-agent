from collections import deque

import numpy as np
import torch
import torch.nn as nn
from stable_baselines3.common.callbacks import BaseCallback
from stable_baselines3.common.torch_layers import BaseFeaturesExtractor

from silksong.constants import (
    STATE_DIM, RAYCAST_DIM, NUM_RAYS, NUM_HIT_TYPES, HIT_TYPE_EMBED_DIM,
    NUM_BOSS_ANIMATION_STATES, NUM_PLAYER_ANIMATION_STATES,
    BOSS_ANIM_EMBED_DIM, PLAYER_ANIM_EMBED_DIM,
)


class MultiHeadFeatureExtractor(BaseFeaturesExtractor):
    BASE_STATE_DIM = 21
    BOSS_ANIM_IDX = 21
    BOSS_ANIM_PROGRESS_IDX = 22
    PLAYER_ANIM_IDX = 23
    PLAYER_ANIM_PROGRESS_IDX = 24

    def __init__(self, observation_space, features_dim: int = 128):
        super().__init__(observation_space, features_dim)

        self.hit_type_embedding = nn.Embedding(NUM_HIT_TYPES, HIT_TYPE_EMBED_DIM)
        self.boss_anim_embedding = nn.Embedding(NUM_BOSS_ANIMATION_STATES, BOSS_ANIM_EMBED_DIM)
        self.player_anim_embedding = nn.Embedding(NUM_PLAYER_ANIMATION_STATES, PLAYER_ANIM_EMBED_DIM)

        raycast_input_dim = NUM_RAYS + NUM_RAYS * HIT_TYPE_EMBED_DIM

        state_input_dim = self.BASE_STATE_DIM + BOSS_ANIM_EMBED_DIM + 1 + PLAYER_ANIM_EMBED_DIM + 1

        self.state_branch = nn.Sequential(
            nn.Linear(state_input_dim, 128),
            nn.ReLU(),
            nn.LayerNorm(128),
            nn.Linear(128, 128),
            nn.ReLU(),
        )

        self.raycast_branch = nn.Sequential(
            nn.Linear(raycast_input_dim, 128),
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
        raycast_raw = observations[:, STATE_DIM:]

        base_state = state_obs[:, :self.BASE_STATE_DIM]

        boss_anim_idx = state_obs[:, self.BOSS_ANIM_IDX].long().clamp(0, NUM_BOSS_ANIMATION_STATES - 1)
        boss_anim_progress = state_obs[:, self.BOSS_ANIM_PROGRESS_IDX:self.BOSS_ANIM_PROGRESS_IDX + 1]
        boss_anim_embedded = self.boss_anim_embedding(boss_anim_idx)

        player_anim_idx = state_obs[:, self.PLAYER_ANIM_IDX].long().clamp(0, NUM_PLAYER_ANIMATION_STATES - 1)
        player_anim_progress = state_obs[:, self.PLAYER_ANIM_PROGRESS_IDX:self.PLAYER_ANIM_PROGRESS_IDX + 1]
        player_anim_embedded = self.player_anim_embedding(player_anim_idx)

        state_combined = torch.cat([
            base_state,
            boss_anim_embedded, boss_anim_progress,
            player_anim_embedded, player_anim_progress
        ], dim=-1)

        distances = raycast_raw[:, :NUM_RAYS]
        hit_type_indices = raycast_raw[:, NUM_RAYS:NUM_RAYS * 2].long().clamp(0, NUM_HIT_TYPES - 1)

        hit_type_embedded = self.hit_type_embedding(hit_type_indices)
        hit_type_flat = hit_type_embedded.view(hit_type_embedded.size(0), -1)

        raycast_obs = torch.cat([distances, hit_type_flat], dim=-1)

        state_features = self.state_branch(state_combined)
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
