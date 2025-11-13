using UI;
using UnityEngine;
using UnityEngine.UI;
public class MainMenuMusicStarter : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip mainMenuMusic;   // assign in Inspector

    [Tooltip("Seconds to cross-fade in (0 = start immediately).")]
    [SerializeField] private float fadeSeconds = 0f;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("MainMenuMusicStarter: AudioManager not found in scene.");
            return;
        }

        if (mainMenuMusic != null)
        {
            // Use the AudioManager API we already have
            AudioManager.Instance.PlayMusic(mainMenuMusic, fadeSeconds);
        }
        else
        {
            Debug.LogWarning("MainMenuMusicStarter: mainMenuMusic is not assigned.");
        }
    }
}