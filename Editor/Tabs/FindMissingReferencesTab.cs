using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LamHD.GameOptimizer.Editor
{
    public class FindMissingReferencesTab
    {
        private Vector2 _scrollPosition;
        private List<MissingReferenceResult> _results = new List<MissingReferenceResult>();

        private struct MissingReferenceResult
        {
            public GameObject targetObject;
            public string componentName;
            public string propertyName;
            public string path;
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField("Find Missing References", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scan for serialized fields referencing destroyed or missing objects (None/Missing).", MessageType.Warning);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Active Scene", GUILayout.Height(40)))
            {
                ScanActiveScene();
            }

            if (GUILayout.Button("Scan Project Prefabs", GUILayout.Height(40)))
            {
                ScanProjectPrefabs();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            if (_results.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Found {_results.Count} missing references:", EditorStyles.boldLabel);
                if (GUILayout.Button("Clear Results", GUILayout.Width(100)))
                {
                    _results.Clear();
                }
                EditorGUILayout.EndHorizontal();
                
                var style = new GUIStyle(EditorStyles.helpBox);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, style, GUILayout.Height(250));

                for (int i = 0; i < _results.Count; i++)
                {
                    var res = _results[i];
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = res.targetObject;
                        EditorGUIUtility.PingObject(res.targetObject);
                    }
                    
                    var richStyle = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
                    EditorGUILayout.LabelField(
                        $"<b>{res.targetObject.name}</b> in <color=#FF6B6B>{res.componentName}</color> (Field: <i>{res.propertyName}</i>) - {res.path}", 
                        richStyle
                    );
                    
                    EditorGUILayout.EndHorizontal();
                    
                    // Separator line for readability
                    EditorGUI.DrawRect(GUILayoutUtility.GetRect(100, 1), new Color(0.3f, 0.3f, 0.3f, 0.5f));
                }

                EditorGUILayout.EndScrollView();
            }
            else if (_results.Capacity > 0)
            {
                EditorGUILayout.HelpBox("No missing references found! 🎉", MessageType.Info);
            }
        }

        private void ScanActiveScene()
        {
            _results.Clear();
            _results.Capacity = 1; // Mark as searched

            var objects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in objects)
            {
                if (go.scene.isLoaded)
                {
                    if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                        continue;

                    CheckGameObjectForMissingRefs(go);
                }
            }
        }

        private void ScanProjectPrefabs()
        {
            _results.Clear();
            _results.Capacity = 1;

            string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
            
            try
            {
                for (int i = 0; i < prefabPaths.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(prefabPaths[i]);
                    if (EditorUtility.DisplayCancelableProgressBar("Scanning Prefabs", $"Processing {path} ({i}/{prefabPaths.Length})", (float)i / prefabPaths.Length))
                    {
                        break;
                    }

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        var transforms = prefab.GetComponentsInChildren<Transform>(true);
                        foreach (var tr in transforms)
                        {
                            CheckGameObjectForMissingRefs(tr.gameObject);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void CheckGameObjectForMissingRefs(GameObject go)
        {
            var components = go.GetComponents<Component>();
            foreach (var component in components)
            {
                // Component missing completely is handled by Clean missing scripts
                if (component == null) continue;

                var so = new SerializedObject(component);
                var sp = so.GetIterator();

                while (sp.NextVisible(true))
                {
                    if (sp.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        // Object reference is null, but the instance id is not 0 => Missing Reference!
                        if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                        {
                            _results.Add(new MissingReferenceResult
                            {
                                targetObject = go,
                                componentName = component.GetType().Name,
                                propertyName = ObjectNames.NicifyVariableName(sp.name),
                                path = GetFullPath(go)
                            });
                        }
                    }
                }
            }
        }

        private string GetFullPath(GameObject go)
        {
            string path = go.name;
            Transform tr = go.transform.parent;
            while (tr != null)
            {
                path = tr.name + "/" + path;
                tr = tr.parent;
            }
            return go.scene.isLoaded ? path : $"[Prefab] {path}";
        }
    }
}
