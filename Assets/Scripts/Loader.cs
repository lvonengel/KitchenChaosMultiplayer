using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles scene loading with dedicated loading scene
/// </summary>
public static class Loader {
    
    /// <summary>
    /// All Staes used in the game
    /// </summary>
    public enum Scene {
        MainMenuScene,
        GameScene,
        LoadingScene,
        LobbyScene,
        CharacterSelectScene,
    }
    
    /// <summary>
    /// Scene that will be laoded after the loading screen finishes
    /// </summary>
    private static Scene targetScene;

    /// <summary>
    /// First transitions to loading screen, then loading screen
    /// calls callback when done
    /// </summary>
    /// <param name="targetScene">The scene that loads after loading scene</param>
    public static void Load(Scene targetScene) {
        Loader.targetScene = targetScene;
        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static void LoadNetwork(Scene targetScene) {
        NetworkManager.Singleton.SceneManager.LoadScene(targetScene.ToString(), LoadSceneMode.Single);
    }

    /// <summary>
    /// Callback executed and loads the target scene when done
    /// </summary>
    public static void LoaderCallback() {
        SceneManager.LoadScene(targetScene.ToString());
    }

}
