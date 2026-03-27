using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LamHD.GameOptimizer.Editor
{
    public class CleanMissingScriptsTab
    {
        private Vector2 _scrollPosition;
        private List<string> _logMessages = new List<string>();

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Sweep Missing Scripts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Remove broken or missing MonoBehaviour components from GameObjects.", MessageType.Info);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clean Active Scene / Opened Prefab", GUILayout.Height(40)))
            {
                CleanCurrentContext();
            }

            if (GUILayout.Button("Scan & Clean Entire Project", GUILayout.Height(40)))
            {
                CleanEntireProject();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            if (_logMessages.Count > 0)
            {
                EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
                
                var style = new GUIStyle(EditorStyles.helpBox);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, style, GUILayout.Height(150));
                
                foreach (var msg in _logMessages)
                {
                    EditorGUILayout.LabelField(msg, EditorStyles.wordWrappedLabel);
                }
                
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Clear Logs", GUILayout.Width(100)))
                {
                    _logMessages.Clear();
                }
            }
        }

        private void CleanCurrentContext()
        {
            _logMessages.Clear();
            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            int goCount = 0;
            int componentsCount = 0;

            foreach (var go in objects)
            {
                if (go.scene.isLoaded || PrefabUtility.IsPartOfPrefabInstance(go) || PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                        continue;

                    int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    if (count > 0)
                    {
                        goCount++;
                        componentsCount += count;
                        _logMessages.Add($"[{go.scene.name}] Removed {count} missing scripts from: {go.name}");
                        EditorUtility.SetDirty(go);
                    }
                }
            }

            _logMessages.Insert(0, $"=== SUMMARY ===");
            _logMessages.Insert(1, $"Total missing scripts removed: {componentsCount}");
            _logMessages.Insert(2, $"Affected GameObjects: {goCount}");
            _logMessages.Insert(3, $"=================");
        }

        private void CleanEntireProject()
        {
            _logMessages.Clear();
            
            if (!EditorUtility.DisplayDialog("Clean Entire Project", 
                "This will scan all prefabs in the project and remove missing scripts. This might take a while and cannot be easily undone. Are you sure?", 
                "Yes, Clean it", "Cancel"))
            {
                return;
            }

            var prefabPaths = AssetDatabase.FindAssets("t:Prefab");
            int goCount = 0;
            int componentsCount = 0;
            int prefabCount = 0;

            try
            {
                for (int i = 0; i < prefabPaths.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(prefabPaths[i]);
                    
                    if (EditorUtility.DisplayCancelableProgressBar("Cleaning Prefabs", $"Processing {path} ({i}/{prefabPaths.Length})", (float)i / prefabPaths.Length))
                    {
                        break;
                    }

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                        if (count > 0)
                        {
                            prefabCount++;
                            goCount++;
                            componentsCount += count;
                            
                            // Iterate children
                            var children = prefab.GetComponentsInChildren<Transform>(true);
                            foreach (var child in children)
                            {
                                int childCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                                if (childCount > 0)
                                {
                                    goCount++;
                                    componentsCount += childCount;
                                }
                            }

                            _logMessages.Add($"Removed missing scripts from Prefab: {path}");
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }

            _logMessages.Insert(0, $"=== SUMMARY ===");
            _logMessages.Insert(1, $"Total missing scripts removed: {componentsCount}");
            _logMessages.Insert(2, $"Affected GameObjects: {goCount} (across {prefabCount} prefabs)");
            _logMessages.Insert(3, $"=================");
        }
    }
}
