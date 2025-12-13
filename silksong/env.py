import gymnasium as gym
import numpy as np
from gymnasium import spaces

from silksong.shared_memory import SilkSongSharedMemory, GameState
from silksong.constants import (
    PLAYER_MAX_HEALTH,
    BOSS_MAX_HEALTH,
    MAX_EPISODE_STEPS,
    SILK_COST_CLAWLINE,
    SILK_COST_SKILL,
    SILK_COST_HEAL,
    OBSERVATION_DIM,
)


class SilksongBossEnv(gym.Env):
    metadata = {"render_modes": []}

    def __init__(self, id: int = 1, time_scale: float = 1.0, nofx: bool = False):
        super().__init__()

        self.action_space = spaces.MultiDiscrete([3, 3, 2, 2, 2, 2, 2, 2])
        self.observation_space = spaces.Box(
            low=-np.inf, high=np.inf, shape=(OBSERVATION_DIM,), dtype=np.float32
        )

        self.shm = SilkSongSharedMemory(id, time_scale, nofx)

        self.prev_boss_health = 0
        self.prev_player_health = 0
        self.prev_player_silk = 0
        self.total_steps = 0

        self.attack_count = 0
        self.heal_count = 0
        self.hurt_count = 0
        self.episode_reward = 0.0
        self.lowest_boss_hp = float('inf')

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        game_state = self.shm.reset()

        self.prev_boss_health = game_state.boss_health
        self.prev_player_health = game_state.player_health
        self.prev_player_silk = game_state.player_silk
        self.total_steps = 0

        self.attack_count = 0
        self.heal_count = 0
        self.hurt_count = 0
        self.episode_reward = 0.0
        self.lowest_boss_hp = game_state.boss_health

        observation = game_state.to_observation()
        info = self._get_info(game_state)

        return observation, info

    def step(self, action):
        self.total_steps += 1

        binary_action = self._convert_to_binary(action)
        game_state = self.shm.step(binary_action)

        reward = self._calculate_reward(game_state)

        self.episode_reward += reward
        self.lowest_boss_hp = min(self.lowest_boss_hp, game_state.boss_health)

        observation = game_state.to_observation()

        terminated = self._is_terminated(game_state)
        truncated = self._is_truncated(game_state)

        self.prev_boss_health = game_state.boss_health
        self.prev_player_health = game_state.player_health
        self.prev_player_silk = game_state.player_silk

        info = self._get_info(game_state, terminated or truncated)

        return observation, reward, terminated, truncated, info

    def _calculate_reward(self, game_state: GameState) -> float:
        boss_dmg = self.prev_boss_health - game_state.boss_health
        player_dmg = self.prev_player_health - game_state.player_health

        reward = 0.0

        if boss_dmg == 0 and player_dmg == 0:
            reward -= 0.001

        if boss_dmg > 0:
            reward += (boss_dmg / BOSS_MAX_HEALTH)
            self.attack_count += 1
        if player_dmg > 0:
            reward -= (player_dmg / PLAYER_MAX_HEALTH) * 0.2
            self.hurt_count += 1

        distance = np.sqrt(
            (game_state.player_pos_x - game_state.boss_pos_x) ** 2 +
            (game_state.player_pos_y - game_state.boss_pos_y) ** 2
        )

        too_far = distance > 15.0
        too_close = distance < 1.0

        if too_far:
            reward -= 0.001
        elif too_close:
            reward -= 0.001

        return reward

    def _is_terminated(self, game_state: GameState) -> bool:
        return game_state.boss_health <= 0 or game_state.player_health <= 0

    def _is_truncated(self, game_state: GameState) -> bool:
        if self.total_steps >= MAX_EPISODE_STEPS:
            return True
        return game_state.truncated

    def _get_info(self, game_state: GameState, episode_end: bool = False) -> dict:
        info = {
            "player_health": game_state.player_health,
            "boss_health": game_state.boss_health,
            "player_silk": game_state.player_silk,
            "episode_time": game_state.episode_time,
            "total_steps": self.total_steps,
            "player_pos": (game_state.player_pos_x, game_state.player_pos_y),
            "boss_pos": (game_state.boss_pos_x, game_state.boss_pos_y),
        }

        if episode_end:
            info["episode_reward"] = self.episode_reward
            info["lowest_boss_hp"] = self.lowest_boss_hp
            info["attack_count"] = self.attack_count
            info["heal_count"] = self.heal_count
            info["hurt_count"] = self.hurt_count

        return info

    def _convert_to_binary(self, action: np.ndarray) -> np.ndarray:
        binary = np.zeros(10, dtype=np.int8)

        if action[0] == 1:
            binary[0] = 1
        elif action[0] == 2:
            binary[1] = 1

        if action[1] == 1:
            binary[2] = 1
        elif action[1] == 2:
            binary[3] = 1

        binary[4] = action[2]
        binary[5] = action[3]
        binary[6] = action[4]
        binary[7] = action[5]
        binary[8] = action[6]
        binary[9] = action[7]

        return binary

    def close(self):
        if hasattr(self, 'shm') and self.shm is not None:
            self.shm.close()
            self.shm = None
