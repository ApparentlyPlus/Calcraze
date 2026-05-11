# Calcraze

[![Deploy Windows](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/Windows.yml/badge.svg)](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/Windows.yml)
[![Deploy Linux](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/Linux.yml/badge.svg)](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/Linux.yml)
[![Deploy macOS (Apple Silicon)](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/MacOS-ARM.yml/badge.svg)](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/MacOS-ARM.yml)
[![Deploy macOS (Intel)](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/MacOS-Intel.yml/badge.svg)](https://github.com/ApparentlyPlus/Calcraze/actions/workflows/MacOS-Intel.yml)

Calcraze is a game where players race against the clock to solve mathematical expressions. Each expression is randomly generated but guaranteed to be solvable. The game adapts difficulty based on player performance.

## Features

- **Expression Generation**: Expressions are built as abstract syntax trees and decomposed to guarantee solvability
- **Difficulty Adaptation**: Gameplay difficulty adjusts based on player performance and streaks
- **Configurable Presets**: Three difficulty levels (Easy, Balanced, Hard)
- **Customizable Difficulty**: Control expression depth, operator types, leaf node ranges, and more through the settings screen
- **Real-time Feedback**: Visual feedback on answer correctness with streak tracking
- **Fully Cross-platform**: Runs on Windows, macOS (Intel & ARM), and Linux
- **Timed Gameplay**: Configurable round durations

## How It Works

### Expression Generation

The **ExpressionGenerator** class uses a recursive tree-building algorithm to create mathematically valid expressions:

1. **Target-Driven Generation**: Given a target number, the algorithm decomposes it into sub-expressions
2. **Tree Structure**: Expressions are represented as abstract syntax trees (AST) with operator nodes and value leaf nodes
3. **Operator Selection**: Operators are chosen based on target factorization to ensure viable decomposition
4. **Controlled Complexity**: Depth limits and leaf bias parameters control expression complexity

### Difficulty Scaling

The adaptive difficulty system monitors player performance:
- Tracks correct/incorrect answers and win streaks
- Adjusts expression depth, operator distribution, and number ranges
- Blends between Base and Ceiling configurations for a smooth difficulty curve

### Architecture

- **UI Framework**: [Avalonia](https://avaloniaui.net/)
- **.NET Runtime**: .NET 10.0
- **MVVM Pattern**: Separation of concerns via ViewModels
- **Game Loop**: 30fps timer based update cycle

## Getting Started

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/en-us/download) with AOT support

See the [.NET 10 download page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) for platform-specific installation instructions.

### Getting the game

Pre-built binaries for all platforms are available on the [Releases](../../releases) page. Download the zip for your platform and extract it.

>[!IMPORTANT]
>**For macOS users**: After extracting the binary, open a terminal in the folder containing the binary and run:
>```bash
>xattr -rd com.apple.quarantine .
>```
>
>This removes the macOS quarantine attribute to allow the binary to run.

### Building from Source

The project uses publish profiles for cross-platform builds, making it *exceedingly simple* to build from source.

#### Linux Build

```bash
git clone https://github.com/ApparentlyPlus/Calcraze.git
cd Calcraze
dotnet publish /p:PublishProfile=Linux
```

The output will be in `publish/Calcraze`.

#### Windows Build

```bash
git clone https://github.com/ApparentlyPlus/Calcraze.git
cd Calcraze
dotnet publish /p:PublishProfile=Windows
```

Generates a Windows executable. The output will be in `publish/Calcraze.exe`.

#### macOS (Intel) Build

```bash
git clone https://github.com/ApparentlyPlus/Calcraze.git
cd Calcraze
dotnet publish /p:PublishProfile=IntelMac

# For mac specifically:
xattr -rd com.apple.quarantine publish/Calcraze
```
Generates an x86 macOS executable. The output will be in `publish/Calcraze`.

#### macOS (Apple Silicon/ARM) Build

```bash
git clone https://github.com/ApparentlyPlus/Calcraze.git
cd Calcraze
dotnet publish /p:PublishProfile=ArmMac

# For mac specifically:
xattr -rd com.apple.quarantine publish/Calcraze
```

Generates a macOS executable for Apple Silicon (M1, M2, M3, M4, M5 processors). The output will be in `publish/Calcraze`.

## Controls

- **Number Keys**: Input your answer
- **Enter / Return**: Submit your answer
- **Settings**: Adjust difficulty and round duration from the start screen
- **New Game**: Start a fresh game from the game over screen

## Difficulty Settings

Modify gameplay through the settings panel:

- **Difficulty Preset**: Choose from Easy, Balanced, or Hard
- **Round Duration**: Set time limit from 30 to 180 seconds
- **Custom Difficulty** (Advanced):
  - Expression depth and complexity
  - Operator weights (Add, Subtract, Multiply, Divide)
  - Allowed number ranges
  - Negative number support

## Project Structure

```
src/
  ├─ Program.cs                    # Application entry point
  ├─ App.axaml/cs                  # Application root component
  ├─ ExpressionGenerator.cs        # Core expression generation algorithm
  ├─ ExpressionConfig.cs           # Configuration and presets
  ├─ Nodes.cs                      # Expression AST node definitions
  ├─ ViewLocator.cs                # MVVM view resolution
  ├─ ViewModels/
  │  ├─ MainWindowViewModel.cs     # Game state and logic
  │  ├─ SettingsViewModel.cs       # Settings management
  │  └─ ViewModelBase.cs           # Base VM class
  ├─ Views/
  │  ├─ MainWindow.axaml/cs        # Main game UI
  │  └─ SettingsPanel.axaml/cs     # Settings UI
  └─ Assets/                       # Images, icons, styling

publish/                           # Output directory for built binaries
```

## Pedagogy

Calcraze is designed to help players develop and strengthen core mathematical and cognitive skills:

### Arithmetic Fluency
- Players practice mental calculation under time pressure, reinforcing speed and accuracy in basic operations (addition, subtraction, multiplication, division)
- Repeated exposure to number combinations builds automatic recall and reduces reliance on external computation aids

### Expression Evaluation
- Players learn to parse and evaluate complex mathematical expressions following proper order of operations
- Exposure to nested expressions with multiple operators builds understanding of operator precedence and parenthetical grouping

### Problem-Solving Strategies
- The time constraint encourages players to develop efficient mental math techniques
- Players learn to recognize patterns and shortcuts (e.g., factorization, grouping) to solve expressions faster

### Working Memory Development
- Tracking intermediate results while solving multi-step expressions exercises working memory capacity
- The adaptive difficulty ensures players operate at their current cognitive limit, promoting growth

### Persistence and Resilience
- The streak system and difficulty adaptation motivate continued practice even after failures
- Players learn that sustained effort improves performance, building growth mindset

### Number Sense
- Repeated interaction with different number ranges and operations strengthens intuitive understanding of magnitude and relationships
- Players develop better estimation and approximation skills through practice

---

Built with: C#, .NET 10, Avalonia 12, and Knuth's subtractive RNG