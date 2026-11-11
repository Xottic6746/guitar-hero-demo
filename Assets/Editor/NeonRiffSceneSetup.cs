using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NeonRiffSceneSetup
{
    [MenuItem("Neon Riff/Create Playable Demo Scene")]
    public static void CreatePlayableScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject game = new GameObject("Neon Riff Game");
        game.AddComponent<NeonRiffGame>();
        const string folder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Scenes");
        const string scenePath = "Assets/Scenes/NeonRiff.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(scenePath);
        Debug.Log("Neon Riff demo scene created. Press Play to start.");
    }
}
