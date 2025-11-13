using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class VolumeSetting : MonoBehaviour
{
     [Header("Mixer & Exposed params")] 
     [SerializeField] private AudioMixer mixer;

     [SerializeField] private string masterParam = "MasterVolume";
     [SerializeField] private string sfxParam = "SFXVolume";
     [SerializeField] private string musicParam = "MusicVolume";
     
     [Header("Panel Visibility")]
     [SerializeField] private CanvasGroup panel;
     [SerializeField] private bool startHidden = true;
     private InputAction _toggleVolumeAction;
     
     [Header("UI Sliders (0..1)")] 
     [SerializeField] private Slider masterSlider;
     [SerializeField] private Slider sfxSlider;
     [SerializeField] private Slider musicSlider;
     
     // Persistance Keys
     private const string KeyMaster = "vol.master";
     private const string KeySfx = "vol.sfx";
     private const string KeyMusic = "vol.music";
     private const string KeyPanel  = "ui.volume.visible";

     private const float MinDb = -40f; // Silent
     private const float MaxDb = 0f; // Full Volume

     private void Awake()
     {
          // make sliders normalized 0..1 and continuous
          ConfigureSlider(masterSlider);
          ConfigureSlider(sfxSlider);
          ConfigureSlider(musicSlider);
          
          // Create an inline input action bound to V
          _toggleVolumeAction = new InputAction("ToggleVolume", binding: "<Keyboard>/v");
     }
     
     private void OnEnable()
     {
          // Load persisted values (default to 1.0 if not found)
          float master = PlayerPrefs.GetFloat(KeyMaster, 1f);
          float sfx    = PlayerPrefs.GetFloat(KeySfx,    1f);
          float music  = PlayerPrefs.GetFloat(KeyMusic,  1f);

          // Push to UI (won’t spam mixer yet because we subscribe after set)
          if (masterSlider) masterSlider.SetValueWithoutNotify(master);
          if (sfxSlider)    sfxSlider.SetValueWithoutNotify(sfx);
          if (musicSlider)  musicSlider.SetValueWithoutNotify(music);

          // Apply to mixer
          ApplyVolume(masterParam, master);
          ApplyVolume(sfxParam,    sfx);
          ApplyVolume(musicParam,  music);

          // Subscribe UI events
          if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterChanged);
          if (sfxSlider)    sfxSlider.onValueChanged.AddListener(OnSfxChanged);
          if (musicSlider)  musicSlider.onValueChanged.AddListener(OnMusicChanged);
          
          // Panel visibility (persisted, else startHidden)
          SetPanelVisible(startHidden);

          // Hook the V key
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
     
     // --- UI handlers ---
     private void OnMasterChanged(float value)
     {
          ApplyVolume(masterParam, value);
          PlayerPrefs.SetFloat(KeyMaster, value);
     }

     private void OnSfxChanged(float value)
     {
          ApplyVolume(sfxParam, value);
          PlayerPrefs.SetFloat(KeySfx, value);
     }

     private void OnMusicChanged(float value)
     {
          ApplyVolume(musicParam, value);
          PlayerPrefs.SetFloat(KeyMusic, value);
     }
     
     // --- Toggle handler ---
     private void OnTogglePerformed(InputAction.CallbackContext _)
     {
          if (!panel) return;
          SetPanelVisible(panel.alpha < 0.5f);
     }

     public void CloseVolumePanel()
     {
          SetPanelVisible(false);
     }
     
     public void OpenVolumePanel()
     {
          SetPanelVisible(true);
     }

     private void SetPanelVisible(bool visible)
     {
          if (!panel) return;
          panel.alpha = visible ? 1f : 0f;
          panel.interactable = visible;
          panel.blocksRaycasts = visible;
          PlayerPrefs.SetInt(KeyPanel, visible ? 1 : 0);
     }
     
     // --- Core mapping ---
     
     /// <summary>
     /// Maps a normalized slider value (0..1) to dB and sets it on the mixer.
     /// Uses log mapping: 0 -> MinDb (mute), 1 -> 0 dB.
     /// </summary>
     private void ApplyVolume(string exposedParam, float normalized)
     {
          if (mixer == null || string.IsNullOrEmpty(exposedParam)) return;

          float db = NormalizedToDb(normalized);
          mixer.SetFloat(exposedParam, db);
     }
     
     /// <summary>
     /// Converts 0..1 to decibels with a perceptually sane curve.
     /// At 0 returns MinDb (mute). Otherwise 20*log10(value) clamped to [MinDb, 0].
     /// </summary>
     private static float NormalizedToDb(float value)
     {
          if (value <= 0.0001f) return MinDb; // avoid -Inf
          float db = Mathf.Log10(Mathf.Clamp01(value)) * 20f;
          return Mathf.Max(db, MinDb);
     }
     
     private static float DbToNormalized(float db)
     {
          if (db <= MinDb) return 0f;
          return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
     }
     
     private static void ConfigureSlider(Slider s)
     {
          if (!s) return;
          s.minValue = 0f;
          s.maxValue = 1f;
          s.wholeNumbers = false;
     }
     

}
