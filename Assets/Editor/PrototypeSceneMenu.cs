#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeSceneMenu
{
    [MenuItem("The Bottom Drawer/Create Or Open Prototype Scene")]
    public static void CreateScene()
    {
        const string scenePath = "Assets/Scenes/Prototype.unity";
        if (File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.GetComponent<Camera>().orthographic = true;
        camera.GetComponent<Camera>().orthographicSize = 5.6f;
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new [] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.Refresh();
        Debug.Log("Prototype scene created. Press Play to launch The Bottom Drawer.");
    }
}
#endif
