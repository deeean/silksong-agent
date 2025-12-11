import atexit
import os
import signal
import struct
import subprocess
from enum import IntEnum
from multiprocessing import shared_memory
from dataclasses import dataclass

import numpy as np

from silksong.constants import (
    PLAYER_MAX_HEALTH,
    PLAYER_MAX_SILK,
    BOSS_MAX_HEALTH,
    NUM_BOSS_ATTACK_STATES,
    ARENA_MIN_X,
    ARENA_MAX_X,
    ARENA_MIN_Y,
    ARENA_MAX_Y,
    HERO_VEL_X_RANGE,
    HERO_VEL_Y_RANGE,
    BOSS_VEL_X_RANGE,
    BOSS_VEL_Y_RANGE,
)

_active_instances: list["SilkSongSharedMemory"] = []


def _cleanup_all():
    for instance in _active_instances[:]:
        try:
            instance.close()
        except Exception:
            pass


atexit.register(_cleanup_all)


def _signal_handler(signum, frame):
    _cleanup_all()
    exit(0)


signal.signal(signal.SIGTERM, _signal_handler)
signal.signal(signal.SIGINT, _signal_handler)


class CommandType(IntEnum):
    NONE = 0
    STEP = 1
    RESET = 2


class StateType(IntEnum):
    NONE = 0
    READY = 1
    STEP = 2
    RESET = 3


class BossAttackState(IntEnum):
    IDLE = 0
    HOP = 1
    POSE = 2
    COMBO_SLASH_ANTIC = 3
    COMBO_SLASH_ATTACK = 4
    COUNTER_ANTIC = 5
    COUNTER_STANCE = 6
    COUNTER_ATTACK = 7
    RAPID_SLASH_ANTIC = 8
    RAPID_SLASH_ATTACK = 9
    J_SLASH_ANTIC = 10
    J_SLASH_ATTACK = 11
    DOWNSTAB_ANTIC = 12
    DOWNSTAB_ATTACK = 13
    CHARGE_ANTIC = 14
    CHARGE_ATTACK = 15
    CROSS_SLASH_ANTIC = 16
    CROSS_SLASH_ATTACK = 17
    EVADE = 18
    STUN = 19
    TELEPORT = 20
    PHASE_TRANSITION = 21
    QUICK_SLASH_ATTACK = 22
    UNKNOWN = 23


@dataclass
class GameState:
    player_pos_x: float
    player_pos_y: float
    player_vel_x: float
    player_vel_y: float
    player_health: int
    player_max_health: int
    player_silk: int
    player_grounded: bool
    player_can_dash: bool
    player_facing_right: bool
    player_invincible: bool

    boss_pos_x: float
    boss_pos_y: float
    boss_vel_x: float
    boss_vel_y: float
    boss_health: int
    boss_max_health: int
    boss_phase: int
    boss_attack_state: int
    boss_facing_right: bool

    episode_time: float
    terminated: bool
    truncated: bool
    player_can_attack: bool

    raycast_distances: np.ndarray
    raycast_hit_types: np.ndarray

    MAX_DISTANCE = np.sqrt((ARENA_MAX_X - ARENA_MIN_X) ** 2 + (ARENA_MAX_Y - ARENA_MIN_Y) ** 2)

    def to_observation(self) -> np.ndarray:
        norm_player_x = (self.player_pos_x - ARENA_MIN_X) / (ARENA_MAX_X - ARENA_MIN_X)
        norm_player_y = (self.player_pos_y - ARENA_MIN_Y) / (ARENA_MAX_Y - ARENA_MIN_Y)
        norm_boss_x = (self.boss_pos_x - ARENA_MIN_X) / (ARENA_MAX_X - ARENA_MIN_X)
        norm_boss_y = (self.boss_pos_y - ARENA_MIN_Y) / (ARENA_MAX_Y - ARENA_MIN_Y)

        norm_player_vel_x = (self.player_vel_x - HERO_VEL_X_RANGE[0]) / (HERO_VEL_X_RANGE[1] - HERO_VEL_X_RANGE[0])
        norm_player_vel_y = (self.player_vel_y - HERO_VEL_Y_RANGE[0]) / (HERO_VEL_Y_RANGE[1] - HERO_VEL_Y_RANGE[0])
        norm_boss_vel_x = (self.boss_vel_x - BOSS_VEL_X_RANGE[0]) / (BOSS_VEL_X_RANGE[1] - BOSS_VEL_X_RANGE[0])
        norm_boss_vel_y = (self.boss_vel_y - BOSS_VEL_Y_RANGE[0]) / (BOSS_VEL_Y_RANGE[1] - BOSS_VEL_Y_RANGE[0])

        boss_attack_one_hot = np.zeros(NUM_BOSS_ATTACK_STATES, dtype=np.float32)
        if 0 <= self.boss_attack_state < NUM_BOSS_ATTACK_STATES:
            boss_attack_one_hot[self.boss_attack_state] = 1.0

        rel_x = (self.boss_pos_x - self.player_pos_x) / (ARENA_MAX_X - ARENA_MIN_X)
        rel_y = (self.boss_pos_y - self.player_pos_y) / (ARENA_MAX_Y - ARENA_MIN_Y)
        distance = np.sqrt((self.boss_pos_x - self.player_pos_x)**2 + (self.boss_pos_y - self.player_pos_y)**2) / self.MAX_DISTANCE

        state_obs = [
            np.clip(norm_player_x, 0.0, 1.0),
            np.clip(norm_player_y, 0.0, 1.0),
            np.clip(norm_player_vel_x, 0.0, 1.0),
            np.clip(norm_player_vel_y, 0.0, 1.0),
            float(self.player_health) / PLAYER_MAX_HEALTH,
            float(self.player_silk) / PLAYER_MAX_SILK,
            float(self.player_grounded),
            float(self.player_can_dash),
            float(self.player_facing_right),
            float(self.player_invincible),
            float(self.player_can_attack),
            np.clip(norm_boss_x, 0.0, 1.0),
            np.clip(norm_boss_y, 0.0, 1.0),
            np.clip(norm_boss_vel_x, 0.0, 1.0),
            np.clip(norm_boss_vel_y, 0.0, 1.0),
            float(self.boss_health) / BOSS_MAX_HEALTH,
            float(self.boss_phase) / 2.0,
            float(self.boss_facing_right),
            np.clip(rel_x + 0.5, 0.0, 1.0),
            np.clip(rel_y + 0.5, 0.0, 1.0),
            np.clip(distance, 0.0, 1.0),
        ]

        state_obs.extend(boss_attack_one_hot)

        num_hit_types = 5
        normalized_hit_types = self.raycast_hit_types.astype(np.float32) / (num_hit_types - 1)

        raycast_obs = np.concatenate([
            self.raycast_distances,
            normalized_hit_types
        ])

        observe = np.concatenate([
            np.array(state_obs, dtype=np.float32),
            raycast_obs.astype(np.float32)
        ])

        return observe


class SilkSongSharedMemory:
    MEMORY_NAME = "silksong_shared_memory"
    MEMORY_SIZE = 4096

    STATE_OFFSET = 0
    GAME_STATE_OFFSET = 4
    COMMAND_OFFSET = 1024

    GAME_STATE_FORMAT = 'f' * 9 + 'i' * 7 + 'B' * 8 + 'f' * 32 + 'i' * 32
    GAME_STATE_SIZE = struct.calcsize(GAME_STATE_FORMAT)

    MAX_ENVS = 4

    @staticmethod
    def get_game_path(env_id: int) -> str:
        return os.getenv(f"SILKSONG_PATH_{env_id}")

    def __init__(self, id: int, time_scale: float = 1.0):
        self.id = id
        self.time_scale = time_scale
        self.process = None

        if id < 1 or id > self.MAX_ENVS:
            raise ValueError(f"Invalid environment ID: {id}. Must be 1-{self.MAX_ENVS}.")

        game_path = self.get_game_path(id)
        shm_name = self.MEMORY_NAME + f"_{id}"

        try:
            self.shm = shared_memory.SharedMemory(
                name=shm_name,
                create=True,
                size=self.MEMORY_SIZE,
            )
            self.shm.buf[:] = bytes(self.MEMORY_SIZE)
            self._owns_shm = True
            print(f"[Env {id}] Created shared memory: {shm_name}")
        except FileExistsError:
            self.shm = shared_memory.SharedMemory(
                name=shm_name,
                create=False,
            )
            self._owns_shm = False
            print(f"[Env {id}] Connected to existing shared memory: {shm_name}")

        print(f"[Env {id}] Launching game from: {game_path}")
        print(f"[Env {id}] Time scale: {time_scale}")
        self.process = subprocess.Popen([game_path, "-id", str(id), "-timescale", str(time_scale)])

        print(f"[Env {id}] Waiting for game to connect...")
        self.wait_for_state(StateType.READY)
        print(f"[Env {id}] Game connected!")

        _active_instances.append(self)

    def read_state(self) -> StateType:
        return struct.unpack_from('i', self.shm.buf, offset=self.STATE_OFFSET)[0]

    def read_game_state(self) -> GameState:
        data = struct.unpack_from(self.GAME_STATE_FORMAT, self.shm.buf, offset=self.GAME_STATE_OFFSET)

        raycast_distances = np.array(data[24:56], dtype=np.float32)
        raycast_hit_types = np.array(data[56:88], dtype=np.float32)

        return GameState(
            player_pos_x=data[0],
            player_pos_y=data[1],
            player_vel_x=data[2],
            player_vel_y=data[3],
            boss_pos_x=data[4],
            boss_pos_y=data[5],
            boss_vel_x=data[6],
            boss_vel_y=data[7],
            episode_time=data[8],
            player_health=data[9],
            player_max_health=data[10],
            player_silk=data[11],
            boss_health=data[12],
            boss_max_health=data[13],
            boss_phase=data[14],
            boss_attack_state=data[15],
            player_grounded=bool(data[16]),
            player_can_dash=bool(data[17]),
            player_facing_right=bool(data[18]),
            player_invincible=bool(data[19]),
            boss_facing_right=bool(data[20]),
            terminated=bool(data[21]),
            truncated=bool(data[22]),
            player_can_attack=bool(data[23]),
            raycast_distances=raycast_distances,
            raycast_hit_types=raycast_hit_types,
        )

    def send_command(self, command_type: CommandType,
                    left: bool = False, right: bool = False,
                    up: bool = False, down: bool = False,
                    jump: bool = False, attack: bool = False,
                    dash: bool = False, clawline: bool = False,
                    skill: bool = False, heal: bool = False):
        offset = self.COMMAND_OFFSET

        struct.pack_into('i', self.shm.buf, offset + 0, int(command_type))
        struct.pack_into('B', self.shm.buf, offset + 4, 1 if left else 0)
        struct.pack_into('B', self.shm.buf, offset + 5, 1 if right else 0)
        struct.pack_into('B', self.shm.buf, offset + 6, 1 if up else 0)
        struct.pack_into('B', self.shm.buf, offset + 7, 1 if down else 0)
        struct.pack_into('B', self.shm.buf, offset + 8, 1 if jump else 0)
        struct.pack_into('B', self.shm.buf, offset + 9, 1 if attack else 0)
        struct.pack_into('B', self.shm.buf, offset + 10, 1 if dash else 0)
        struct.pack_into('B', self.shm.buf, offset + 11, 1 if clawline else 0)
        struct.pack_into('B', self.shm.buf, offset + 12, 1 if skill else 0)
        struct.pack_into('B', self.shm.buf, offset + 13, 1 if heal else 0)
        struct.pack_into('i', self.shm.buf, offset + 14, 1)

    def wait_for_state(self, state_type: StateType):
        while True:
            current_state = self.read_state()
            if current_state == state_type:
                struct.pack_into('i', self.shm.buf, self.STATE_OFFSET, int(StateType.READY))
                break

    def reset(self) -> GameState:
        self.send_command(CommandType.RESET)
        self.wait_for_state(StateType.RESET)
        return self.read_game_state()

    def step(self, action: np.ndarray) -> GameState:
        if len(action) != 10:
            raise ValueError(f"Action must have 10 elements, got {len(action)}")

        self.send_command(
            CommandType.STEP,
            left=bool(action[0]),
            right=bool(action[1]),
            up=bool(action[2]),
            down=bool(action[3]),
            jump=bool(action[4]),
            attack=bool(action[5]),
            dash=bool(action[6]),
            clawline=bool(action[7]),
            skill=bool(action[8]),
            heal=bool(action[9])
        )
        self.wait_for_state(StateType.STEP)
        return self.read_game_state()

    def close(self):
        if self in _active_instances:
            _active_instances.remove(self)

        if self.process is not None:
            try:
                self.process.terminate()
                self.process.wait(timeout=5)
            except Exception:
                try:
                    self.process.kill()
                except Exception:
                    pass
            self.process = None

        if hasattr(self, 'shm') and self.shm is not None:
            try:
                self.shm.close()
                if self._owns_shm:
                    self.shm.unlink()
            except Exception:
                pass
            self.shm = None
