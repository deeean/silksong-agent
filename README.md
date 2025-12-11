# silksong-agent

A reinforcement learning agent for Hollow Knight: Silksong boss fights. Currently specialized for the Lace boss fight.

## Requirements

- [Hollow Knight: Silksong](https://store.steampowered.com/app/1030300/Hollow_Knight_Silksong/) (paid)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases)
- [.NET SDK](https://dotnet.microsoft.com/download) (for building the plugin)
- [uv](https://github.com/astral-sh/uv) (Python package manager)

## Installation

### Game Setup

You can run up to 4 game instances simultaneously during training.

Each instance requires a separate copy of the game.

First, disable Steam Cloud synchronization.

Install BepInEx in the Steam game installation folder, then create a `steam_appid.txt` file and enter `1030300`.

Copy this folder to 4 separate locations.

### Save File Setup

Copy [resources/user1.dat](resources/user1.dat) to `%USERPROFILE%\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\default`.

### Environment Configuration

Copy `plugin/Directory.Build.props.example` to `plugin/Directory.Build.props` and set `SILKSONG_PATH_1` to `SILKSONG_PATH_4` to the game executable paths (e.g., `D:/Games/Silksong 1/Hollow Knight Silksong.exe`).
Also copy `.env.example` to `.env` and configure it the same way.

### Build Plugin

```bash
dotnet build plugin
```

The built plugin will be automatically copied to each game's `BepInEx/plugins/` folder.

## Usage

### Training

```bash
uv run train.py
uv run train.py --n_envs 4
uv run train.py --n_envs 4 --checkpoint ./models/rl_model_1000_steps.zip
```

> **Tip**: Using a smaller game window size speeds up step processing.

### Evaluation

```bash
uv run train.py --eval --checkpoint ./models/rl_model_1000_steps.zip
```

### Arguments

| Argument | Description |
|----------|-------------|
| `--n_envs <n>` | Number of parallel environments (1-4, default: 1) |
| `--checkpoint <path>` | Resume training from checkpoint |
| `--eval` | Evaluation mode (requires --checkpoint) |

### Tensorboard

```bash
tensorboard --logdir ./logs
```

### Manual Mode

When the game runs with the plugin installed, keyboard input is blocked and the Lace boss fight starts automatically.
Use the `-manual` argument when you need to modify equipment/items.

```bash
"D:/Games/Silksong 1/Hollow Knight Silksong.exe" -manual
```

| Argument | Description |
|----------|-------------|
| `-id <n>` | Instance ID (1-4) |
| `-timescale <n>` | Game speed multiplier (training: 4, evaluation: 1) |
| `-manual` | Manual mode |

### Debug Overlay

Press `F1` to toggle the state UI overlay, and `F2` to toggle raycast visualization.

### Notes

- Game audio is disabled by the plugin.
- Initially, the reward was designed as binary (hit/hurt). However, the agent learned to spam the Clawline skill because it deals multiple hits despite low damage per hit. The reward has been changed to damage-proportional, but this has not been fully tested yet.

## Architecture

```
┌─────────────────┐     Shared Memory      ┌─────────────────┐
│                 │ ◄──────────────────────│                 │
│  Python (RL)    │     GameState          │  Game (Plugin)  │
│                 │ ──────────────────────►│                 │
└─────────────────┘     Command            └─────────────────┘
```

- **Shared Memory**: Python and the game plugin communicate through memory-mapped files for low-latency data exchange
- **Step Mode**: The game pauses after each step (`Time.timeScale = 0`) and waits for the next action from Python
- **GameState**: Player position/velocity, boss state, raycast data, etc. are sent to Python every step
- **Command**: Python sends actions (move, jump, attack, etc.) and reset commands to the game

## Extending to Other Bosses

To train on other bosses, you need to modify the following files:
- Analyze the boss's State through PlayMakerFSM
- [constants.py](silksong/constants.py), [Constants.cs](plugin/Source/Core/Constants.cs) - Boss health, arena coordinates, etc.
- [EpisodeResetter.cs](plugin/Source/Core/EpisodeResetter.cs) - Scene transition logic, playerData settings (Lace has an elevator animation, so it waits for `acceptingInput` to be true)
- [BossStateManager.cs](plugin/Source/Managers/BossStateManager.cs) - Boss State mapping
- [SharedMemoryManager.cs](plugin/Source/Managers/SharedMemoryManager.cs), [shared_memory.py](silksong/shared_memory.py) - When changing State structure

## TODO

- [ ] Optimize FindObjectsByType usage in ProjectileTrackerManager if a faster method exists
- [ ] Test if Raycast hit type needs one-hot encoding

## Acknowledgments

This project was inspired by the [HKRL](https://github.com/AdityaJain1030/HKRL) repository.

## License

MIT License
