# Changelog

## [1.1.0] - 2026-03-27

### Added
- **Find Unused Resources** tab
  - Scan all `Resources/` folders for assets not referenced by any scene, prefab, or ScriptableObject
  - Detect `Resources.Load()` / `Resources.LoadAll()` / `Resources.LoadAsync()` calls in C# scripts
  - Display file sizes and total wasted space
  - Select and batch-delete unused assets
  - Configurable scan options (Scenes, Prefabs, ScriptableObjects, Scripts)
  - Color-coded asset type labels

## [1.0.0] - 2026-03-25

### Added
- **Clean Missing Scripts** tab
  - Clean missing scripts in current scene or opened prefab
  - Scan and clean missing scripts across all prefabs in the project
- **Find Missing References** tab
  - Find missing/null object references in current scene
  - Scan all prefabs in project for missing references
  - Clickable results to navigate to problematic GameObjects
