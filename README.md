# silksong-agent

A reinforcement learning agent for Hollow Knight: Silksong boss fights.

> Currently specialized for the **Lace** boss fight.

## Requirements

| Requirement | Description |
|-------------|-------------|
| [Hollow Knight: Silksong](https://store.steampowered.com/app/1030300/Hollow_Knight_Silksong/) | Game (paid) |
| [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) | Mod framework |
| [.NET SDK](https://dotnet.microsoft.com/download) | For building the plugin |
| [uv](https://github.com/astral-sh/uv) | Python package manager |

## Installation

### 1. Game Setup

You can run up to 4 game instances simultaneously during training. Each instance requires a separate copy of the game.

1. Disable Steam Cloud synchronization
2. Install BepInEx in the Steam game installation folder
3. Create `steam_appid.txt` in the game folder and enter `1030300`
4. Copy this folder to 4 separate locations

### 2. Save File Setup

Copy [resources/user1.dat](resources/user1.dat) to:
```
%USERPROFILE%\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\default
```

### 3. Environment Configuration

1. Copy `plugin/Directory.Build.props.example` → `plugin/Directory.Build.props`
2. Copy `.env.example` → `.env`
3. Set `SILKSONG_PATH_1` to `SILKSONG_PATH_4` to the game executable paths

```
SILKSONG_PATH_1=D:/Games/Silksong 1/Hollow Knight Silksong.exe
SILKSONG_PATH_2=D:/Games/Silksong 2/Hollow Knight Silksong.exe
...
```

### 4. Build Plugin

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

## Game Plugin

### Launch Arguments

| Argument | Description |
|----------|-------------|
| `-id <n>` | Instance ID (1-4) |
| `-timescale <n>` | Game speed multiplier (training: 4, evaluation: 1) |
| `-manual` | Manual mode (enables keyboard input) |
| `-nofx` | Disable visual effects |

### Manual Mode

When the game runs with the plugin installed, keyboard input is blocked and the Lace boss fight starts automatically. Use the `-manual` argument when you need to modify equipment/items.

```bash
"D:/Games/Silksong 1/Hollow Knight Silksong.exe" -manual
```

### Debug Overlay

| Key | Action |
|-----|--------|
| `F1` | Toggle state UI overlay |
| `F2` | Toggle raycast visualization |

## Architecture

```
┌─────────────────┐     Shared Memory      ┌─────────────────┐
│                 │ ◄──────────────────────│                 │
│  Python (RL)    │     GameState          │  Game (Plugin)  │
│                 │ ──────────────────────►│                 │
└─────────────────┘     Command            └─────────────────┘
```

| Component | Description |
|-----------|-------------|
| **Shared Memory** | Python and the game plugin communicate through memory-mapped files |
| **Step Mode** | Game pauses after each step (`Time.timeScale = 0`) and waits for next action |
| **GameState** | Player position/velocity, boss state, raycast data sent every step |
| **Command** | Python sends actions (move, jump, attack, etc.) and reset commands |

## Extending to Other Bosses

To train on other bosses, modify the following files:

| File | Purpose |
|------|---------|
| `silksong/constants.py` | Boss health, arena coordinates |
| `plugin/Source/Core/Constants.cs` | Same as above (C# side) |
| `plugin/Source/Core/EpisodeResetter.cs` | Scene transition logic, playerData settings |
| `plugin/Source/Managers/BossStateManager.cs` | Boss state mapping |
| `silksong/shared_memory.py` | GameState structure (Python) |
| `plugin/Source/Managers/SharedMemoryManager.cs` | GameState structure (C#) |

> **Tip**: Analyze the boss's state through PlayMakerFSM.

## Notes

- Game audio is disabled by the plugin.
- Initially, the reward was designed as binary (hit/hurt). However, the agent learned to spam the Clawline skill because it deals multiple hits despite low damage per hit. The reward has been changed to damage-proportional.

## Acknowledgments

This project was inspired by [HKRL](https://github.com/AdityaJain1030/HKRL).

## License

MIT License