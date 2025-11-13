using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UI
{
    [AddComponentMenu("Audio/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        
        // ----------------------------- Serialized Setup --------------------------

        [Header("Mixer")]
        [Tooltip("AudioMixer that contains Master/SFX/Music groups with exposed parameters.")]
        [SerializeField] private AudioMixer mixer;

        [Tooltip("Exposed parameter name for MASTER volume (dB).")]
        [SerializeField] private string masterParam = "MasterVolume";
        [Tooltip("Exposed parameter name for SFX volume (dB).")]
        [SerializeField] private string sfxParam    = "SFXVolume";
        [Tooltip("Exposed parameter name for MUSIC volume (dB).")]
        [SerializeField] private string musicParam  = "MusicVolume";

        [Header("Routing")]
        [Tooltip("Route SFX one-shots to this mixer group.")]
        [SerializeField] private AudioMixerGroup sfxGroup;
        [Tooltip("Route Music to this mixer group.")]
        [SerializeField] private AudioMixerGroup musicGroup;

        [Header("Music Player")]
        [Tooltip("Default starting track.")]
        [SerializeField] private AudioClip defaultMusic;
        [Tooltip("Cross-fade duration seconds when switching music.")]
        [SerializeField, Min(0f)] private float musicFadeSeconds = 0.8f;

        [Header("SFX Pool")]
        [Tooltip("How many SFX AudioSources to keep for concurrent one-shots.")]
        [SerializeField, Range(2, 32)] private int sfxPoolSize = 8;
        [Tooltip("Create 3D SFX sources? If false, SFX are 2D (UI, HUD).")]
        [SerializeField] private bool sfx3D = false;
        [Tooltip("Default Spatial Blend for SFX when sfx3D = true (0 = 2D, 1 = 3D).")]
        [SerializeField, Range(0f,1f)] private float sfxSpatialBlend = 1f;
        
        // ----------------------------- Runtime State -----------------------------

        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private int _nextSfxIndex;

        private AudioSource _musicA;   // cross-fade A
        private AudioSource _musicB;   // cross-fade B
        private AudioSource _activeMusic; // which one is currently active
        private float _targetMaster = 1f, _targetSfx = 1f, _targetMusic = 1f; // 0..1

        // Volume persistence keys
        private const string KEY_MASTER = "vol.master";
        private const string KEY_SFX    = "vol.sfx";
        private const string KEY_MUSIC  = "vol.music";

        // dB clamp (practical mute)
        private const float MIN_DB = -80f;
        private const float MAX_DB = 0f;
        
        
        // --- Singleton
        /// <summary> Global access  </summary>
        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Duplicate AudioManager found. Destroying the new one.");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // BuildPools();
            // LoadSavedVolumes();
            // ApplyAllVolumes();
        }
        
        // ----------------------------- Initialization ----------------------------

        /// <summary> Prepares music sources and SFX pool. </summary>
        private void BuildPools()
        {
            // Music dual sources for cross-fades
            _musicA = gameObject.AddComponent<AudioSource>();
            _musicB = gameObject.AddComponent<AudioSource>();
            foreach (var src in new[] { _musicA, _musicB })
            {
                src.playOnAwake = false;
                src.loop = true;
                src.spatialBlend = 0f; // music is 2D
                if (musicGroup) src.outputAudioMixerGroup = musicGroup;
            }
            _activeMusic = _musicA;

            // SFX pool
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = false;
                s.spatialBlend = sfx3D ? sfxSpatialBlend : 0f;
                if (sfxGroup) s.outputAudioMixerGroup = sfxGroup;
                _sfxPool.Add(s);
            }

            #if UNITY_EDITOR
            if (!mixer)
                Debug.LogWarning("AudioManager: Mixer not assigned. Volume setters will be no-ops.");
            if (string.IsNullOrEmpty(masterParam) || string.IsNullOrEmpty(sfxParam) || string.IsNullOrEmpty(musicParam))
                Debug.LogWarning("AudioManager: Exposed parameter names are empty (Master/SFX/Music).");
            #endif
        }
        
        private void Start()
        {
            if (defaultMusic) PlayMusic(defaultMusic, fadeSeconds: 0f);
        }
        
        // ----------------------------- Public API (Music) ------------------------

        /// <summary>
        /// Play a music clip with optional cross-fade.
        /// If the same clip is already playing, nothing happens.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeSeconds = -1f)
        {
            if (clip == null) return;
            var current = _activeMusic;
            if (current.clip == clip && current.isPlaying) return;

            float fade = (fadeSeconds >= 0f) ? fadeSeconds : musicFadeSeconds;

            // Choose the inactive source for the new clip
            var next = (_activeMusic == _musicA) ? _musicB : _musicA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();

            // Cross-fade coroutine
            StopAllCoroutines();
            StartCoroutine(CrossFade(current, next, fade));
            _activeMusic = next;
        }
        
        /// <summary> Stops the current music with an optional fade-out. </summary>
        public void StopMusic(float fadeOutSeconds = -1f)
        {
            float fade = (fadeOutSeconds >= 0f) ? fadeOutSeconds : musicFadeSeconds;
            var current = _activeMusic;
            if (!current || !current.isPlaying) return;

            StopAllCoroutines();
            StartCoroutine(FadeOutStop(current, fade));
        }

        // ----------------------------- Public API (SFX) --------------------------

        /// <summary> Play a 2D one-shot SFX (UI/HUD) on the pool. </summary>
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (!clip) return;
            var s = NextSfxSource();
            s.transform.position = transform.position;
            s.spatialBlend = sfx3D ? sfxSpatialBlend : 0f;
            s.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            s.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary> Play a 3D one-shot SFX at a world position (if sfx3D enabled). </summary>
        public void PlaySfxAt(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f)
        {
            if (!clip) return;
            var s = NextSfxSource();
            s.transform.position = worldPos;
            s.spatialBlend = sfx3D ? sfxSpatialBlend : 0f;
            s.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            s.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private AudioSource NextSfxSource()
        {
            var s = _sfxPool[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxPool.Count;
            return s;
        }

        // ----------------------------- Public API (Volume) -----------------------

        /// <summary> 0..1 normalized volumes (log mapped to dB on the mixer). </summary>
        public void SetMasterVolume(float v) { _targetMaster = Clamp01(v); ApplyVolume(masterParam, _targetMaster); Save(KEY_MASTER, _targetMaster); }
        public void SetSfxVolume   (float v) { _targetSfx    = Clamp01(v); ApplyVolume(sfxParam,    _targetSfx);    Save(KEY_SFX,    _targetSfx);    }
        public void SetMusicVolume (float v) { _targetMusic  = Clamp01(v); ApplyVolume(musicParam,  _targetMusic);  Save(KEY_MUSIC,  _targetMusic);  }

        public float GetMasterVolume() => _targetMaster;
        public float GetSfxVolume()    => _targetSfx;
        public float GetMusicVolume()  => _targetMusic;

        // ----------------------------- Helpers (Volumes) -------------------------

        private void LoadSavedVolumes()
        {
            _targetMaster = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
            _targetSfx    = PlayerPrefs.GetFloat(KEY_SFX,    1f);
            _targetMusic  = PlayerPrefs.GetFloat(KEY_MUSIC,  1f);
        }

        private void ApplyAllVolumes()
        {
            ApplyVolume(masterParam, _targetMaster);
            ApplyVolume(sfxParam,    _targetSfx);
            ApplyVolume(musicParam,  _targetMusic);
        }

        private void ApplyVolume(string exposedParam, float normalized)
        {
            if (!mixer || string.IsNullOrEmpty(exposedParam)) return;
            mixer.SetFloat(exposedParam, NormalizedToDb(normalized));
        }

        private static float NormalizedToDb(float value)
        {
            if (value <= 0.0001f) return MIN_DB; // mute
            float db = Mathf.Log10(Mathf.Clamp01(value)) * 20f;
            return Mathf.Clamp(db, MIN_DB, MAX_DB);
        }

        private static float Clamp01(float v) => (v < 0f) ? 0f : (v > 1f ? 1f : v);
        private static void Save(string key, float v) => PlayerPrefs.SetFloat(key, v);

        // ----------------------------- Coroutines (Music) ------------------------

        private System.Collections.IEnumerator CrossFade(AudioSource from, AudioSource to, float seconds)
        {
            if (seconds <= 0f)
            {
                if (from) { from.Stop(); from.volume = 0f; }
                to.volume = 1f;
                yield break;
            }

            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime; // UI/menus unaffected by timeScale
                float a = Mathf.Clamp01(t / seconds);
                if (from) from.volume = 1f - a;
                to.volume = a;
                yield return null;
            }
            if (from) { from.Stop(); from.volume = 0f; }
            to.volume = 1f;
        }

        private System.Collections.IEnumerator FadeOutStop(AudioSource src, float seconds)
        {
            if (seconds <= 0f)
            {
                src.Stop();
                src.volume = 0f;
                yield break;
            }

            float start = src.volume;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / seconds);
                src.volume = start * a;
                yield return null;
            }
            src.Stop();
            src.volume = 0f;
        }

        // ----------------------------- Editor Helpers ----------------------------
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep pool size sane in inspector changes.
            sfxPoolSize = Mathf.Clamp(sfxPoolSize, 2, 32);
            sfxSpatialBlend = Mathf.Clamp01(sfxSpatialBlend);
            musicFadeSeconds = Mathf.Max(0f, musicFadeSeconds);
        }
        #endif
    }
}
