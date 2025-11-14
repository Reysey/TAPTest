using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// UI-only controller for the Volume Panel:
    /// - Shows/hides the panel (CanvasGroup), toggle on V
    /// - Binds sliders (0..1) to AudioManager volumes
    /// - Persists slider values and panel visibility
    /// 
    /// All audio math & mixer routing is delegated to AudioManager.
    /// </summary>
    public class VolumePanelController : MonoBehaviour
    {
        [Header("Panel Visibility")]
        [SerializeField] private CanvasGroup panel;
        [SerializeField] private bool startHidden = true;

        [Header("UI Sliders (0..1)")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider musicSlider;

        // Input (inline action bound to V)
        private InputAction _toggleVolumeAction;

        // Persistence keys (same as before)
        private const string KeyMaster = "vol.master";
        private const string KeySfx    = "vol.sfx";
        private const string KeyMusic  = "vol.music";
        private const string KeyPanel  = "ui.volume.visible";

        private void Awake()
        {
            // Defend against missing refs
            if (!panel) panel = GetComponent<CanvasGroup>();

            ConfigureSlider(masterSlider);
            ConfigureSlider(sfxSlider);
            ConfigureSlider(musicSlider);

            // Inline InputAction (press V to toggle)
            _toggleVolumeAction = new InputAction("ToggleVolume", binding: "<Keyboard>/v");
        }

        private void OnEnable()
        {
            // 1) Load persisted values (fallback 1.0)
            float master = PlayerPrefs.GetFloat(KeyMaster, 1f);
            float sfx    = PlayerPrefs.GetFloat(KeySfx,    1f);
            float music  = PlayerPrefs.GetFloat(KeyMusic,  1f);

            // 2) Push to UI without invoking callbacks
            if (masterSlider) masterSlider.SetValueWithoutNotify(master);
            if (sfxSlider)    sfxSlider.SetValueWithoutNotify(sfx);
            if (musicSlider)  musicSlider.SetValueWithoutNotify(music);

            // 3) Apply to AudioManager (authoritative audio layer)
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(master);
                AudioManager.Instance.SetSfxVolume(sfx);
                AudioManager.Instance.SetMusicVolume(music);
            }
            else
            {
                // Debug.LogWarning("VolumePanelController: AudioManager.Instance is null. Volumes won’t be applied.");
            }

            // 4) Subscribe UI events
            if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (sfxSlider)    sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (musicSlider)  musicSlider.onValueChanged.AddListener(OnMusicChanged);

            // 5) Panel visibility (persisted > startHidden)
            // bool visible = PlayerPrefs.GetInt(KeyPanel, startHidden ? 0 : 1) == 1;
            SetPanelVisible(!startHidden);

            // 6) Hook V key
            _toggleVolumeAction.performed += OnTogglePerformed;
            _toggleVolumeAction.Enable();
        }

        private void OnDisable()
        {
            if (masterSlider) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            if (sfxSlider)    sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            if (musicSlider)  musicSlider.onValueChanged.RemoveListener(OnMusicChanged);

            _toggleVolumeAction.performed -= OnTogglePerformed;
            _toggleVolumeAction.Disable();
        }

        private void OnDestroy()
        {
            _toggleVolumeAction?.Dispose();
        }

        // ---------- UI callbacks ----------

        private void OnMasterChanged(float v)
        {
            PlayerPrefs.SetFloat(KeyMaster, v);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(v);
        }

        private void OnSfxChanged(float v)
        {
            PlayerPrefs.SetFloat(KeySfx, v);
            if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v);
        }

        private void OnMusicChanged(float v)
        {
            PlayerPrefs.SetFloat(KeyMusic, v);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v);
        }

        // ---------- Panel visibility & toggle ----------

        private void OnTogglePerformed(InputAction.CallbackContext _)
        {
            if (!panel) return;
            SetPanelVisible(panel.alpha < 0.5f);
        }

        /// <summary>Public toggle for a UI Button.</summary>
        public void TogglePanel()
        {
            if (!panel) return;
            SetPanelVisible(panel.alpha < 0.5f);
        }

        public void ShowPanel() => SetPanelVisible(true);
        public void HidePanel() => SetPanelVisible(false);

        private void SetPanelVisible(bool visible)
        {
            if (!panel) return;
            panel.alpha = visible ? 1f : 0f;
            panel.interactable = visible;
            panel.blocksRaycasts = visible;
            PlayerPrefs.SetInt(KeyPanel, visible ? 1 : 0);
        }

        // ---------- Utilities ----------

        private static void ConfigureSlider(Slider s)
        {
            if (!s) return;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.wholeNumbers = false;
        }
    }   
}