using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")] [SerializeField]
    private string gamePlaySceneName = "LD_TestScene";

    /// <summary>
    /// Called by the Start button (OnClick)
    /// </summary>
    public void OnStartClicked()
    {
        if (string.IsNullOrWhiteSpace(gamePlaySceneName))
        {
            Debug.LogError("MainMenu: gamePlaySceneName is empty. Set it in the Inspector.");
            return;
        }
        
        //
        if(!IsSceneInBuild(gamePlaySceneName)){
            Debug.LogError($"MainMenu: Scene '{gamePlaySceneName}' is not in Build Settings.");
            return;
        }
        
        SceneManager.LoadScene(gamePlaySceneName, LoadSceneMode.Single);
    }
    
    // --- helpers ---
    private bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        return false;
    }
    
    /// <summary>
    /// Called by the Exit button (OnClick)
    /// </summary>
    public void OnExitClicked()
    {
        #if UNITY_EDITOR
        // Stop Play Mode in Editor
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
