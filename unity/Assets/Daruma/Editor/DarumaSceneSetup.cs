using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ゲーム用シーンとステージを用意するエディタ拡張。
// ・プロジェクトを開いたとき、シーンがなければ作る
// ・ゲーム用シーンを開いていてステージがなければ、ステージを組み立てて保存する
// ・メニュー「だるま」から作り直しもできる
[InitializeOnLoad]
public static class DarumaSceneSetup
{
    const string ScenePath = "Assets/Scenes/DarumaGame.unity";

    static DarumaSceneSetup()
    {
        EditorApplication.delayCall += AutoSetup;
    }

    static void AutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(ScenePath)) { CreateScene(); return; }
        var active = EditorSceneManager.GetActiveScene();
        if (active.path == ScenePath && Object.FindFirstObjectByType<Stage>() == null) RebuildStage();
    }

    [MenuItem("だるま/ゲームシーンを作り直す")]
    public static void CreateScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("DarumaGame");
        game.AddComponent<DarumaGame>();
        game.AddComponent<GameHUD>();
        Stage.Build(game.transform, DarumaGame.Cols, DarumaGame.Rows, DarumaGame.TileSize);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[だるま] ゲームシーンを作成しました: " + ScenePath + "　▶ を押すと遊べます");
    }

    [MenuItem("だるま/ステージをシーンに作り直す")]
    public static void RebuildStage()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path != ScenePath) OpenScene();
        var game = Object.FindFirstObjectByType<DarumaGame>();
        if (game == null)
        {
            game = new GameObject("DarumaGame").AddComponent<DarumaGame>();
            game.gameObject.AddComponent<GameHUD>();
        }
        foreach (var old in Object.FindObjectsByType<Stage>(FindObjectsSortMode.None)) Object.DestroyImmediate(old.gameObject);
        Stage.Build(game.transform, DarumaGame.Cols, DarumaGame.Rows, DarumaGame.TileSize);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[だるま] ステージをシーンに配置しました（DarumaGame > Stage）");
    }

    [MenuItem("だるま/ゲームシーンを開く")]
    public static void OpenScene()
    {
        if (!File.Exists(ScenePath)) { CreateScene(); return; }
        EditorSceneManager.OpenScene(ScenePath);
    }
}
