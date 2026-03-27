# Game Optimizer

A comprehensive Unity Editor tool for game optimization and cleanup.

## Features

### 🧹 Clean Missing Scripts
- Remove all missing (broken) MonoBehaviour scripts from GameObjects
- **Scene/Prefab Mode**: Clean the currently opened scene or prefab
- **Project-wide Scan**: Scan and clean all prefabs in the project

### 🔍 Find Missing References
- Detect serialized fields that reference destroyed or missing objects
- **Scene Scan**: Check all GameObjects in the active scene
- **Project-wide Scan**: Check all prefabs across the project
- Clickable results for quick navigation

### 📦 Find Unused Resources
- Detect assets in `Resources/` folders that are never referenced
- Cross-references dependencies from Scenes, Prefabs, and ScriptableObjects
- Scans C# scripts for `Resources.Load()` calls
- Shows file sizes to identify biggest savings
- Select & delete unused assets directly from the tool
- Configurable scan options (toggle Scenes/Prefabs/ScriptableObjects/Scripts)

## Installation

### Via Git URL (Unity Package Manager)
1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL**
3. Enter: `https://github.com/Unity-Base-Project/game-optimizer.git`

### Via manifest.json
Add to your `Packages/manifest.json`:
```json
"com.lamhd.gameoptimizer": "https://github.com/Unity-Base-Project/game-optimizer.git"
```

## Usage

Open the tool via **Tools → Game Optimizer** in the Unity Editor menu bar.

## Requirements
- Unity 6000.0+
