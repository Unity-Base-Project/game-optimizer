using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;
using System.IO;

namespace LamHD.GameOptimizer.Editor
{
    [InitializeOnLoad]
    public static class SceneSwitcherToolbar
    {
        static SceneSwitcherToolbar()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();

            // Hiển thị tên Scene đang mở ở nhãn nút
            string activeSceneName = "Scenes";
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.name))
            {
                activeSceneName = activeScene.name;
            }

            var content = new GUIContent(activeSceneName, "Chuyển nhanh các Scene có trong Build Settings");
            var buttonRect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, GUILayout.Width(130));

            if (GUI.Button(buttonRect, content, EditorStyles.toolbarDropDown))
            {
                GenericMenu menu = new GenericMenu();
                EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

                if (buildScenes.Length == 0)
                {
                    menu.AddDisabledItem(new GUIContent("Chưa có Scene nào trong Build Settings"));
                }
                else
                {
                    foreach (var sceneInfo in buildScenes)
                    {
                        string path = sceneInfo.path;
                        if (string.IsNullOrEmpty(path)) continue;

                        string sceneName = Path.GetFileNameWithoutExtension(path);
                        bool isActive = (activeScene.path == path);

                        // Chỉ hiện dấu check cho Scene đang mở
                        menu.AddItem(new GUIContent(sceneName), isActive, () => 
                        {
                            // Nếu đang Play Game thì phải thoát PlayMode
                            if (EditorApplication.isPlaying)
                            {
                                EditorApplication.isPlaying = false;
                            }

                            // Hỏi lưu nếu Scene hiện tại chưa lưu
                            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                EditorSceneManager.OpenScene(path);
                            }
                        });
                    }
                }

                menu.DropDown(buttonRect);
            }
        }
    }
}
