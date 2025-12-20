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

1. Disable Steam Cloud synchronization
2. Install BepInEx in the Steam game installation folder
3. Create `steam_appid.txt` in the game folder and enter `1030300`

> **Note**: Multiple game instances are automatically created at runtime using junctions and hard links. No manual copying required.

### 2. Save File Setup

Copy [resources/user1.dat](resources/user1.dat) to:

| Platform | Path |
|----------|------|
| Windows | `%USERPROFILE%\AppData\LocalLow\Team Cherry\Hollow Knight Silksong\default` |
| Linux | `~/.config/unity3d/Team Cherry/Hollow Knight Silksong/default` |

### 3. Environment Configuration

1. Copy `plugin/Directory.Build.props.example` → `plugin/Directory.Build.props`
2. Copy `.env.example` → `.env`
3. Set `SILKSONG_PATH` to the game executable path

```
SILKSONG_PATH=D:/Games/Silksong/Hollow Knight Silksong.exe
```

### 4. Build Plugin

```bash
dotnet build plugin
```

The built plugin will be automatically copied to the game's `BepInEx/plugins/` folder.

## Usage

### Training

```bash
uv run train.py
uv run train.py --n_envs 4
uv run train.py --n_envs 4 --checkpoint ./models/rl_model_1000_steps.zip
```

> **Tip**: Use `-nofx` mode (enabled by default in training) for faster step processing.

#### Linux

On Linux, using Xvfb (virtual framebuffer) significantly improves training speed:

```bash
sudo apt install xvfb
xvfb-run -a uv run train.py --n_envs 4
```

### Evaluation

```bash
uv run train.py --eval --checkpoint ./models/rl_model_1000_steps.zip
```

### Hyperparameter Tuning

```bash
uv run tune.py --n_trials 30 --n_envs 2
```

| Argument | Description |
|----------|-------------|
| `--n_trials` | Number of Optuna trials (default: 30) |
| `--timesteps` | Timesteps per trial (default: 200,000) |
| `--storage` | Optuna storage URL (e.g., `sqlite:///study.db`) |

### Arguments

| Argument | Description |
|----------|-------------|
| `--n_envs <n>` | Number of parallel environments (default: 1) |
| `--checkpoint <path>` | Resume training from checkpoint |
| `--eval` | Evaluation mode (requires --checkpoint) |

### Tensorboard

```bash
tensorboard --logdir ./logs
```

## Multi-Instance Architecture

When running with `--n_envs > 1`, the system automatically creates instance folders:

```
Hollow Knight Silksong/
├── Hollow Knight Silksong.exe           # Original (not used)
├── Hollow Knight Silksong_Data/
├── BepInEx/
├── MonoBleedingEdge/
└── instances/
    ├── 1/
    │   ├── Hollow Knight Silksong.exe   # Copy
    │   ├── Hollow Knight Silksong_Data/ # Junction → Original
    │   ├── MonoBleedingEdge/            # Junction → Original
    │   ├── UnityPlayer.dll              # Hard link → Original
    │   └── BepInEx/
    │       ├── plugins/                 # Junction → Original
    │       └── config/                  # Copy (separate per instance)
    ├── 2/
    │   └── ...
```

- **Junctions**: Folders (no admin required)
- **Hard links**: Large files like `UnityPlayer.dll` (saves disk space)
- **Copies**: Config files (avoid sharing conflicts)

## Game Plugin

### Launch Arguments

| Argument | Description |
|----------|-------------|
| `-id <n>` | Instance ID |
| `-timescale <n>` | Game speed multiplier (training: 4, evaluation: 1) |
| `-manual` | Manual mode (enables keyboard input) |
| `-nofx` | Disable visual/audio effects for performance |

### NoFx Mode

The `-nofx` flag significantly reduces CPU/GPU usage by disabling:
- Visual effects (particles, trails, blur, post-processing)
- Audio components
- Camera effects
- Vibration/haptics

Press `F9` to toggle minimal rendering (16x16 resolution) during NoFx mode.

### Manual Mode

When the game runs with the plugin installed, keyboard input is blocked and the Lace boss fight starts automatically. Use the `-manual` argument when you need to modify equipment/items.

```bash
"D:/Games/Silksong/Hollow Knight Silksong.exe" -manual
```

### Debug Overlay

| Key | Action |
|-----|--------|
| `F1` | Toggle state UI overlay |
| `F2` | Toggle raycast visualization |
| `F9` | Toggle minimal rendering (NoFx mode only) |

## Communication Architecture

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

## Acknowledgments

This project was inspired by [HKRL](https://github.com/AdityaJain1030/HKRL).

## License

MIT License
