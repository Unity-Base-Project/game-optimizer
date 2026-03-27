using UnityEditor;
using UnityEngine;

namespace LamHD.GameOptimizer.Editor
{
    public class GameOptimizerWindow : EditorWindow
    {
        private int _selectedTab;
        private readonly string[] _tabNames = { "🧹 Clean Missing Scripts", "🔍 Find Missing References", "📦 Unused Resources" };

        private CleanMissingScriptsTab _cleanMissingScriptsTab;
        private FindMissingReferencesTab _findMissingReferencesTab;
        private FindUnusedResourcesTab _findUnusedResourcesTab;

        private Vector2 _scrollPosition;

        private static readonly Color HeaderColor = new Color(0.18f, 0.18f, 0.22f);
        private static readonly Color AccentColor = new Color(0.3f, 0.7f, 1f);

        [MenuItem("Tools/Game Optimizer %#g", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameOptimizerWindow>();
            window.titleContent = new GUIContent("Game Optimizer", EditorGUIUtility.IconContent("d_Settings@2x").image);
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _cleanMissingScriptsTab = new CleanMissingScriptsTab();
            _findMissingReferencesTab = new FindMissingReferencesTab();
            _findUnusedResourcesTab = new FindUnusedResourcesTab();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();
            DrawTabContent();
        }

        private void DrawHeader()
        {
            var headerRect = new Rect(0, 0, position.width, 50);
            EditorGUI.DrawRect(headerRect, HeaderColor);

            GUILayout.BeginArea(headerRect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = AccentColor },
                padding = new RectOffset(0, 0, 12, 0)
            };
            GUILayout.Label("⚡ Game Optimizer", titleStyle);

            GUILayout.FlexibleSpace();

            var versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                padding = new RectOffset(0, 15, 12, 0)
            };
            GUILayout.Label("v1.1.0", versionStyle);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUILayout.Space(55);
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            for (int i = 0; i < _tabNames.Length; i++)
            {
                var isSelected = _selectedTab == i;
                var style = new GUIStyle(isSelected ? EditorStyles.toolbarButton : EditorStyles.toolbarButton)
                {
                    fixedHeight = 30,
                    fontSize = 12,
                    fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                    normal =
                    {
                        textColor = isSelected ? AccentColor : Color.gray
                    }
                };

                if (GUILayout.Toggle(isSelected, _tabNames[i], style, GUILayout.MinWidth(180)))
                {
                    _selectedTab = i;
                }
            }

            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            // Separator
            var separatorRect = GUILayoutUtility.GetRect(position.width, 2);
            EditorGUI.DrawRect(separatorRect, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.3f));

            GUILayout.Space(5);
        }

        private void DrawTabContent()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();

            switch (_selectedTab)
            {
                case 0:
                    _cleanMissingScriptsTab?.OnGUI();
                    break;
                case 1:
                    _findMissingReferencesTab?.OnGUI();
                    break;
                case 2:
                    _findUnusedResourcesTab?.OnGUI();
                    break;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }
    }
}
