import gymnasium as gym
import numpy as np
from gymnasium import spaces

from silksong.shared_memory import SilkSongSharedMemory, GameState, BossAttackState
from silksong.constants import (
    PLAYER_BASE_ATTACK_DAMAGE,
    BOSS_MAX_HEALTH,
    MAX_EPISODE_STEPS,
    SILK_COST_CLAWLINE,
    SILK_COST_SKILL,
    SILK_COST_HEAL,
)


class SilksongBossEnv(gym.Env):
    metadata = {"render_modes": []}

    def __init__(self, id: int = 1, time_scale: float = 1.0):
        super().__init__()

        self.action_space = spaces.MultiDiscrete([3, 3, 2, 2, 2, 2, 2, 2])
        self.observation_space = spaces.Box(
            low=-np.inf, high=np.inf, shape=(109,), dtype=np.float32
        )

        self.shm = SilkSongSharedMemory(id, time_scale)

        self.prev_boss_health = 0
        self.prev_player_health = 0
        self.prev_player_silk = 0
        self.total_steps = 0

        self.attack_cooldown = 0
        self.attack_cooldown_frames = 1
        self.skill_cooldown = 0
        self.skill_cooldown_frames = 1
        self.heal_cooldown = 0
        self.heal_cooldown_frames = 1
        self.clawline_cooldown = 0
        self.clawline_cooldown_frames = 1

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
        self.attack_cooldown = 0
        self.skill_cooldown = 0
        self.heal_cooldown = 0
        self.clawline_cooldown = 0

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

        hurt = player_dmg > 0
        hit = boss_dmg > 0
        boss_stunned = game_state.boss_attack_state == BossAttackState.STUN

        if hit:
            self.attack_count += 1
        if hurt:
            self.hurt_count += 1

        reward = 0.0

        if hit:
            damage_reward = boss_dmg / PLAYER_BASE_ATTACK_DAMAGE
            if boss_stunned:
                reward += damage_reward * 2.0
            else:
                reward += damage_reward

        if hurt:
            if boss_stunned:
                reward -= 2.0
            else:
                reward -= 1.0

        health_gained = game_state.player_health - self.prev_player_health
        if health_gained > 0:
            reward += health_gained / 3.0
            self.heal_count += 1

        if not (hurt or hit or health_gained > 0):
            reward -= 0.001

        rel_x = game_state.boss_pos_x - game_state.player_pos_x
        rel_y = game_state.boss_pos_y - game_state.player_pos_y
        distance_to_boss = np.sqrt(rel_x ** 2 + rel_y ** 2)

        too_far = distance_to_boss > 15.0
        too_close = distance_to_boss < 1.0

        if too_far:
            reward -= 0.001
        if too_close:
            reward -= 0.001

        win = game_state.boss_health <= 0
        lose = game_state.player_health <= 0

        if win:
            health_bonus = game_state.player_health / game_state.player_max_health
            time_bonus = min(1.0, 100.0 / max(game_state.episode_time, 1.0))
            reward += health_bonus * 0.5 + time_bonus
        elif lose:
            boss_remaining = game_state.boss_health / BOSS_MAX_HEALTH
            reward -= boss_remaining * 0.5

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

        if self.attack_cooldown > 0:
            binary[5] = 0
            self.attack_cooldown -= 1
        else:
            binary[5] = action[3]
            if action[3] == 1:
                self.attack_cooldown = self.attack_cooldown_frames

        binary[6] = action[4]

        if self.clawline_cooldown > 0:
            binary[7] = 0
            self.clawline_cooldown -= 1
        elif self.prev_player_silk < SILK_COST_CLAWLINE:
            binary[7] = 0
        else:
            binary[7] = action[5]
            if action[5] == 1:
                self.clawline_cooldown = self.clawline_cooldown_frames

        if self.skill_cooldown > 0:
            binary[8] = 0
            self.skill_cooldown -= 1
        elif self.prev_player_silk < SILK_COST_SKILL:
            binary[8] = 0
        else:
            binary[8] = action[6]
            if action[6] == 1:
                self.skill_cooldown = self.skill_cooldown_frames

        if self.heal_cooldown > 0:
            binary[9] = 0
            self.heal_cooldown -= 1
        elif self.prev_player_silk < SILK_COST_HEAL:
            binary[9] = 0
        else:
            binary[9] = action[7]
            if action[7] == 1:
                self.heal_cooldown = self.heal_cooldown_frames

        return binary

    def close(self):
        if hasattr(self, 'shm') and self.shm is not None:
            self.shm.close()
            self.shm = None
