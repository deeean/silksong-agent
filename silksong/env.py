import gymnasium as gym
import numpy as np
from gymnasium import spaces

from silksong.shared_memory import SilkSongSharedMemory, GameState, GameTimeoutError
from silksong.constants import (
    PLAYER_MAX_HEALTH,
    BOSS_MAX_HEALTH,
    MAX_EPISODE_STEPS,
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
        self.current_silk = 0
        self.total_steps = 0

        self.attack_count = 0
        self.heal_count = 0
        self.hurt_count = 0
        self.episode_reward = 0.0
        self.lowest_boss_hp = float('inf')

        self.prev_attack = 0

    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        try:
            game_state = self.shm.reset()
        except GameTimeoutError as e:
            print(f"[Env] Reset timeout: {e}")
            self.shm.restart()
            game_state = self.shm.reset()

        self.prev_boss_health = game_state.boss_health
        self.prev_player_health = game_state.player_health
        self.prev_player_silk = game_state.player_silk
        self.current_silk = game_state.player_silk
        self.total_steps = 0

        self.attack_count = 0
        self.heal_count = 0
        self.hurt_count = 0
        self.episode_reward = 0.0
        self.lowest_boss_hp = game_state.boss_health
        self.prev_attack = 0

        observation = game_state.to_observation()
        info = self._get_info(game_state)

        return observation, info

    def step(self, action):
        self.total_steps += 1

        binary_action = self._convert_to_binary(action)

        try:
            game_state = self.shm.step(binary_action)
        except GameTimeoutError as e:
            print(f"[Env] {e}")
            return self._handle_timeout()

        reward = self._calculate_reward(game_state)

        self.episode_reward += reward
        self.lowest_boss_hp = min(self.lowest_boss_hp, game_state.boss_health)

        observation = game_state.to_observation()

        terminated = self._is_terminated(game_state)
        truncated = self._is_truncated(game_state)

        self.prev_boss_health = game_state.boss_health
        self.prev_player_health = game_state.player_health
        self.prev_player_silk = game_state.player_silk
        self.current_silk = game_state.player_silk

        info = self._get_info(game_state, terminated or truncated)

        return observation, reward, terminated, truncated, info

    def _handle_timeout(self):
        self.shm.restart()

        game_state = self.shm.reset()

        self.prev_boss_health = game_state.boss_health
        self.prev_player_health = game_state.player_health
        self.prev_player_silk = game_state.player_silk

        observation = game_state.to_observation()
        info = self._get_info(game_state, episode_end=True)
        info["timeout_restart"] = True

        return observation, 0.0, False, True, info

    def _calculate_reward(self, game_state: GameState) -> float:
        boss_dmg = self.prev_boss_health - game_state.boss_health
        player_dmg = self.prev_player_health - game_state.player_health
        health_gained = game_state.player_health - self.prev_player_health

        hurt = player_dmg > 0
        hit = boss_dmg > 0
        boss_stunned = game_state.boss_animation_state == 16
        silk_used = self.prev_player_silk - game_state.player_silk

        if hit:
            self.attack_count += 1
        if hurt:
            self.hurt_count += 1

        reward = 0.0

        if hit:
            if boss_stunned:
                reward += 2.0
            else:
                reward += 1.0
        if hurt:
            if boss_stunned:
                reward -= 2.0
            else:
                reward -= 1.0

        if silk_used > 0:
            reward -= 0.05 * silk_used

        if health_gained > 0:
            reward += health_gained
            self.heal_count += 1

        if not (hurt or hit or health_gained > 0):
            reward -= 0.001

        rel_x = game_state.boss_pos_x - game_state.player_pos_x
        rel_y = game_state.boss_pos_y - game_state.player_pos_y
        distance_to_boss = np.sqrt(rel_x ** 2 + rel_y ** 2)

        if distance_to_boss > 15.0:
            reward -= 0.001
        if distance_to_boss < 1.0:
            reward -= 0.001

        win = game_state.boss_health <= 0
        lose = game_state.player_health <= 0

        if win:
            health_bonus = game_state.player_health / PLAYER_MAX_HEALTH
            time_bonus = min(1.0, 100.0 / max(game_state.episode_time, 1.0))
            reward += health_bonus + time_bonus
        elif lose:
            boss_remaining = game_state.boss_health / BOSS_MAX_HEALTH
            reward -= boss_remaining

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

        if self.prev_attack == 1:
            binary[5] = 0
        else:
            binary[5] = action[3]
        self.prev_attack = binary[5]

        binary[6] = action[4]
        binary[7] = action[5]
        binary[8] = action[6]
        binary[9] = action[7]

        return binary

    def close(self):
        if hasattr(self, 'shm') and self.shm is not None:
            self.shm.close()
            self.shm = None
