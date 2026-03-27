using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LamHD.GameOptimizer.Editor
{
    public class FindUnusedResourcesTab
    {
        private Vector2 _scrollPosition;
        private List<UnusedResourceResult> _results = new List<UnusedResourceResult>();
        private bool _hasScanned;
        private long _totalWastedBytes;
        private int _selectedCount;
        private bool _selectAll;

        // Scan options
        private bool _scanScenes = true;
        private bool _scanPrefabs = true;
        private bool _scanScriptableObjects = true;
        private bool _scanScripts = true;

        private struct UnusedResourceResult
        {
            public string assetPath;
            public string resourcePath; // path used in Resources.Load()
            public string assetType;
            public long fileSize;
            public bool selected;
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Find Unused Resources", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scan for assets in 'Resources/' folders that are NOT referenced by any scene, prefab, " +
                "ScriptableObject, or Resources.Load() call in scripts.\n\n" +
                "⚠️ Limitation: Dynamic paths like Resources.Load(\"prefix_\" + var) cannot be detected. " +
                "Always review results before deleting!",
                MessageType.Warning);
            EditorGUILayout.Space(5);

            // Scan options
            EditorGUILayout.LabelField("Scan Options", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _scanScenes = EditorGUILayout.ToggleLeft("Scenes", _scanScenes, GUILayout.Width(80));
            _scanPrefabs = EditorGUILayout.ToggleLeft("Prefabs", _scanPrefabs, GUILayout.Width(80));
            _scanScriptableObjects = EditorGUILayout.ToggleLeft("ScriptableObjects", _scanScriptableObjects, GUILayout.Width(130));
            _scanScripts = EditorGUILayout.ToggleLeft("Scripts (Resources.Load)", _scanScripts, GUILayout.Width(170));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("🔍 Scan Resources Folders", GUILayout.Height(40)))
            {
                ScanUnusedResources();
            }

            EditorGUILayout.Space(10);

            if (_hasScanned)
            {
                if (_results.Count > 0)
                {
                    DrawResults();
                }
                else
                {
                    EditorGUILayout.HelpBox("No unused resources found! All assets in Resources/ are being used. 🎉", MessageType.Info);
                }
            }
        }

        private void DrawResults()
        {
            // Summary header
            _selectedCount = _results.Count(r => r.selected);
            long selectedBytes = _results.Where(r => r.selected).Sum(r => r.fileSize);

            var summaryStyle = new GUIStyle(EditorStyles.helpBox)
            {
                richText = true,
                fontSize = 12,
                padding = new RectOffset(10, 10, 8, 8)
            };

            EditorGUILayout.LabelField(
                $"Found <b>{_results.Count}</b> potentially unused assets  |  " +
                $"Total wasted: <b>{FormatBytes(_totalWastedBytes)}</b>  |  " +
                $"Selected: <b>{_selectedCount}</b> ({FormatBytes(selectedBytes)})",
                summaryStyle);

            EditorGUILayout.Space(5);

            // Toolbar
            EditorGUILayout.BeginHorizontal();
            bool newSelectAll = EditorGUILayout.ToggleLeft("Select All", _selectAll, GUILayout.Width(80));
            if (newSelectAll != _selectAll)
            {
                _selectAll = newSelectAll;
                for (int i = 0; i < _results.Count; i++)
                {
                    var r = _results[i];
                    r.selected = _selectAll;
                    _results[i] = r;
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear Results", GUILayout.Width(100)))
            {
                _results.Clear();
                _hasScanned = false;
                return;
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            EditorGUI.BeginDisabledGroup(_selectedCount == 0);
            if (GUILayout.Button($"🗑 Delete Selected ({_selectedCount})", GUILayout.Width(180)))
            {
                DeleteSelectedAssets();
            }
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Results list
            var boxStyle = new GUIStyle(EditorStyles.helpBox);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, boxStyle, GUILayout.Height(300));

            for (int i = 0; i < _results.Count; i++)
            {
                var res = _results[i];
                EditorGUILayout.BeginHorizontal();

                // Checkbox
                bool sel = EditorGUILayout.Toggle(res.selected, GUILayout.Width(20));
                if (sel != res.selected)
                {
                    res.selected = sel;
                    _results[i] = res;
                }

                // Ping button
                if (GUILayout.Button("📌", GUILayout.Width(30)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(res.assetPath);
                    if (asset != null)
                    {
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                    }
                }

                // Asset info
                var richStyle = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = false };
                var typeColor = GetTypeColor(res.assetType);
                EditorGUILayout.LabelField(
                    $"<color={typeColor}>[{res.assetType}]</color> {res.assetPath}",
                    richStyle);

                GUILayout.FlexibleSpace();

                // File size
                var sizeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = new Color(1f, 0.7f, 0.3f) }
                };
                EditorGUILayout.LabelField(FormatBytes(res.fileSize), sizeStyle, GUILayout.Width(80));

                EditorGUILayout.EndHorizontal();

                // Separator
                EditorGUI.DrawRect(GUILayoutUtility.GetRect(100, 1), new Color(0.3f, 0.3f, 0.3f, 0.3f));
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanUnusedResources()
        {
            _results.Clear();
            _totalWastedBytes = 0;
            _selectAll = false;

            try
            {
                // Step 1: Find all assets in Resources folders
                EditorUtility.DisplayProgressBar("Scanning Resources", "Finding all Resources assets...", 0f);
                var resourceAssets = FindAllResourceAssets();

                if (resourceAssets.Count == 0)
                {
                    _hasScanned = true;
                    return;
                }

                // Step 2: Build a set of all referenced assets
                var referencedAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 2a: Scan dependencies from Scenes
                if (_scanScenes)
                {
                    CollectDependenciesFromScenes(referencedAssets, resourceAssets.Count);
                }

                // 2b: Scan dependencies from Prefabs
                if (_scanPrefabs)
                {
                    CollectDependenciesFromPrefabs(referencedAssets);
                }

                // 2c: Scan dependencies from ScriptableObjects
                if (_scanScriptableObjects)
                {
                    CollectDependenciesFromScriptableObjects(referencedAssets);
                }

                // 2d: Scan C# scripts for Resources.Load() calls
                HashSet<string> scriptReferencedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (_scanScripts)
                {
                    EditorUtility.DisplayProgressBar("Scanning Resources", "Scanning scripts for Resources.Load()...", 0.8f);
                    scriptReferencedPaths = ScanScriptsForResourcesLoad();
                }

                // Step 3: Compare and find unused
                EditorUtility.DisplayProgressBar("Scanning Resources", "Comparing references...", 0.9f);

                foreach (var kvp in resourceAssets)
                {
                    string assetPath = kvp.Key;
                    string resourcePath = kvp.Value;

                    // Check if referenced via AssetDatabase dependencies
                    bool isReferenced = referencedAssets.Contains(assetPath);

                    // Check if referenced via Resources.Load() in scripts
                    if (!isReferenced && _scanScripts)
                    {
                        // Check exact match and partial match (Resources.Load can omit extension)
                        isReferenced = scriptReferencedPaths.Any(srp =>
                            resourcePath.Equals(srp, StringComparison.OrdinalIgnoreCase) ||
                            resourcePath.StartsWith(srp + "/", StringComparison.OrdinalIgnoreCase) ||
                            srp.Equals(Path.GetFileNameWithoutExtension(resourcePath), StringComparison.OrdinalIgnoreCase));
                    }

                    if (!isReferenced)
                    {
                        var fileInfo = new FileInfo(assetPath);
                        long size = fileInfo.Exists ? fileInfo.Length : 0;
                        _totalWastedBytes += size;

                        _results.Add(new UnusedResourceResult
                        {
                            assetPath = assetPath,
                            resourcePath = resourcePath,
                            assetType = GetAssetTypeName(assetPath),
                            fileSize = size,
                            selected = false
                        });
                    }
                }

                // Sort by file size descending (biggest waste first)
                _results.Sort((a, b) => b.fileSize.CompareTo(a.fileSize));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _hasScanned = true;
            }
        }

        /// <summary>
        /// Find all assets inside any Resources/ folder in the project.
        /// Returns a dictionary: assetPath => resourcePath (path relative to Resources/ without extension)
        /// </summary>
        private Dictionary<string, string> FindAllResourceAssets()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Find all directories named "Resources"
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var path in allAssetPaths)
            {
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if this asset is inside a Resources folder
                int resourcesIndex = path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
                if (resourcesIndex < 0) continue;

                // Skip directories, .meta files
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;

                // Calculate the resource path (what you'd pass to Resources.Load)
                string afterResources = path.Substring(resourcesIndex + "/Resources/".Length);
                string resourcePath = Path.ChangeExtension(afterResources, null); // Remove extension

                result[path] = resourcePath;
            }

            return result;
        }

        private void CollectDependenciesFromScenes(HashSet<string> referencedAssets, int totalResourceCount)
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                EditorUtility.DisplayProgressBar("Scanning Resources",
                    $"Scanning scene: {Path.GetFileName(scenePath)} ({i + 1}/{sceneGuids.Length})",
                    0.1f + 0.2f * ((float)i / sceneGuids.Length));

                var deps = AssetDatabase.GetDependencies(scenePath, true);
                foreach (var dep in deps)
                {
                    referencedAssets.Add(dep);
                }
            }
        }

        private void CollectDependenciesFromPrefabs(HashSet<string> referencedAssets)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Scanning Resources",
                        $"Scanning prefabs ({i + 1}/{prefabGuids.Length})",
                        0.3f + 0.2f * ((float)i / prefabGuids.Length));
                }

                var deps = AssetDatabase.GetDependencies(prefabPath, true);
                foreach (var dep in deps)
                {
                    referencedAssets.Add(dep);
                }
            }
        }

        private void CollectDependenciesFromScriptableObjects(HashSet<string> referencedAssets)
        {
            var soGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
            for (int i = 0; i < soGuids.Length; i++)
            {
                string soPath = AssetDatabase.GUIDToAssetPath(soGuids[i]);
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Scanning Resources",
                        $"Scanning ScriptableObjects ({i + 1}/{soGuids.Length})",
                        0.5f + 0.2f * ((float)i / soGuids.Length));
                }

                var deps = AssetDatabase.GetDependencies(soPath, true);
                foreach (var dep in deps)
                {
                    referencedAssets.Add(dep);
                }
            }
        }

        /// <summary>
        /// Scan all C# scripts for Resources.Load("path") patterns.
        /// Returns a set of resource paths found in code.
        /// </summary>
        private HashSet<string> ScanScriptsForResourcesLoad()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Pattern: Resources.Load("...", ...) or Resources.Load<Type>("...")
            // Also matches: Resources.LoadAll, Resources.LoadAsync
            var pattern = new Regex(
                @"Resources\s*\.\s*(?:Load(?:All|Async)?(?:<[^>]+>)?)\s*\(\s*""([^""]+)""",
                RegexOptions.Compiled);

            var scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                if (!scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    string content = File.ReadAllText(scriptPath);
                    var matches = pattern.Matches(content);
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            paths.Add(match.Groups[1].Value);
                        }
                    }
                }
                catch
                {
                    // Skip files that can't be read
                }
            }

            return paths;
        }

        private void DeleteSelectedAssets()
        {
            var toDelete = _results.Where(r => r.selected).ToList();
            if (toDelete.Count == 0) return;

            long totalSize = toDelete.Sum(r => r.fileSize);
            if (!EditorUtility.DisplayDialog("Delete Unused Resources",
                    $"Are you sure you want to delete {toDelete.Count} assets ({FormatBytes(totalSize)})?\n\n" +
                    "This cannot be undone! Make sure you have reviewed all selected assets.",
                    "Yes, Delete", "Cancel"))
            {
                return;
            }

            int deleted = 0;
            foreach (var res in toDelete)
            {
                if (AssetDatabase.DeleteAsset(res.assetPath))
                {
                    deleted++;
                }
            }

            _results.RemoveAll(r => r.selected);
            _totalWastedBytes = _results.Sum(r => r.fileSize);

            AssetDatabase.Refresh();
            Debug.Log($"[Game Optimizer] Deleted {deleted}/{toDelete.Count} unused resource assets.");
        }

        private string GetAssetTypeName(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".png": case ".jpg": case ".jpeg": case ".tga": case ".psd": case ".bmp": case ".gif":
                    return "Texture";
                case ".mat":
                    return "Material";
                case ".prefab":
                    return "Prefab";
                case ".asset":
                    return "Asset";
                case ".wav": case ".mp3": case ".ogg": case ".aif":
                    return "Audio";
                case ".fbx": case ".obj": case ".dae": case ".3ds":
                    return "Model";
                case ".anim":
                    return "Animation";
                case ".controller": case ".overridecontroller":
                    return "Animator";
                case ".shader": case ".shadergraph":
                    return "Shader";
                case ".ttf": case ".otf":
                    return "Font";
                case ".txt": case ".json": case ".xml": case ".csv":
                    return "TextAsset";
                case ".unity":
                    return "Scene";
                default:
                    return "Other";
            }
        }

        private string GetTypeColor(string typeName)
        {
            switch (typeName)
            {
                case "Texture": return "#4FC3F7";
                case "Material": return "#81C784";
                case "Prefab": return "#7986CB";
                case "Audio": return "#FFB74D";
                case "Model": return "#E57373";
                case "Animation": return "#BA68C8";
                case "Animator": return "#9575CD";
                case "Shader": return "#4DB6AC";
                case "Font": return "#A1887F";
                case "TextAsset": return "#90A4AE";
                default: return "#BDBDBD";
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }
    }
}
