using System.Collections;
using UnityEngine;

namespace CoinRushSurvivor.Core
{
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour
    {
        private const string MusicEnabledKey = "coinrush.audio.music.enabled";
        private const string SfxEnabledKey = "coinrush.audio.sfx.enabled";
        private const string MusicVolumeKey = "coinrush.audio.music.volume";
        private const string SfxVolumeKey = "coinrush.audio.sfx.volume";

        [Header("Music")]
        [SerializeField] private AudioClip menuMusicClip;
        [SerializeField] private AudioClip gameplayMusicClip;
        [SerializeField] private string menuMusicResourcePath = "Audio/Music/CoinRushMenu";
        [SerializeField] private string gameplayMusicResourcePath = "Audio/Music/CoinRushGameplay";
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.55f;
        [SerializeField, Min(0f)] private float musicFadeDuration = 0.35f;

        [Header("SFX")]
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip levelUpClip;
        [SerializeField] private AudioClip gameOverClip;
        [SerializeField] private string uiClickResourcePath = "Audio/SFX/CoinRushUiClick";
        [SerializeField] private string levelUpResourcePath = "Audio/SFX/CoinRushLevelUp";
        [SerializeField] private string gameOverResourcePath = "Audio/SFX/CoinRushRunEnd";
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.8f;

        [Header("Settings")]
        [SerializeField] private bool defaultMusicEnabled = true;
        [SerializeField] private bool defaultSfxEnabled = true;

        private AudioSource primaryMusicSource;
        private AudioSource secondaryMusicSource;
        private AudioSource sfxSource;
        private Coroutine musicFadeRoutine;
        private AudioClip queuedMusicClip;
        private bool queuedMusicLoop = true;
        private bool musicEnabled;
        private bool sfxEnabled;
        private float musicVolume;
        private float sfxVolume;

        public bool MusicEnabled => musicEnabled;
        public bool SfxEnabled => sfxEnabled;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            EnsureAudioSources();
            LoadPreferences();
            LoadDefaultClips();
            ApplyVolumeState();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet<AudioService>(out var registeredService) &&
                ReferenceEquals(registeredService, this))
            {
                ServiceLocator.Unregister(this);
            }
        }

        public void PlayMenuMusic()
        {
            PlayMusic(ResolveClip(menuMusicClip, menuMusicResourcePath), true);
        }

        public void PlayGameplayMusic()
        {
            PlayMusic(ResolveClip(gameplayMusicClip, gameplayMusicResourcePath), true);
        }

        public void PlayUiClick()
        {
            PlaySfx(ResolveClip(uiClickClip, uiClickResourcePath), 1f);
        }

        public void PlayLevelUpSting()
        {
            PlaySfx(ResolveClip(levelUpClip, levelUpResourcePath), 1f);
        }

        public void PlayGameOverSting()
        {
            PlaySfx(ResolveClip(gameOverClip, gameOverResourcePath), 1f);
        }

        public void SetMusicEnabled(bool enabled)
        {
            musicEnabled = enabled;
            PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();

            if (!musicEnabled)
            {
                StopMusicSources();
                return;
            }

            ReplayQueuedMusic();
            ApplyVolumeState();
        }

        public void SetSfxEnabled(bool enabled)
        {
            sfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.Save();
            ApplyVolumeState();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
        }

        private void EnsureAudioSources()
        {
            primaryMusicSource = CreateAudioSource("Music_Primary");
            secondaryMusicSource = CreateAudioSource("Music_Secondary");
            sfxSource = CreateAudioSource("Sfx");

            primaryMusicSource.loop = true;
            secondaryMusicSource.loop = true;
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        private AudioSource CreateAudioSource(string sourceName)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            var audioSource = sourceObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            return audioSource;
        }

        private void LoadPreferences()
        {
            musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, defaultMusicEnabled ? 1 : 0) == 1;
            sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, defaultSfxEnabled ? 1 : 0) == 1;
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfxVolume);
        }

        private void LoadDefaultClips()
        {
            menuMusicClip = ResolveClip(menuMusicClip, menuMusicResourcePath);
            gameplayMusicClip = ResolveClip(gameplayMusicClip, gameplayMusicResourcePath);
            uiClickClip = ResolveClip(uiClickClip, uiClickResourcePath);
            levelUpClip = ResolveClip(levelUpClip, levelUpResourcePath);
            gameOverClip = ResolveClip(gameOverClip, gameOverResourcePath);
        }

        private AudioClip ResolveClip(AudioClip assignedClip, string resourcePath)
        {
            if (assignedClip != null)
            {
                return assignedClip;
            }

            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            return Resources.Load<AudioClip>(resourcePath);
        }

        private void PlayMusic(AudioClip clip, bool loop)
        {
            if (clip == null)
            {
                return;
            }

            queuedMusicClip = clip;
            queuedMusicLoop = loop;

            if (!musicEnabled)
            {
                StopMusicSources();
                return;
            }

            var activeSource = GetActiveMusicSource();
            if (activeSource.isPlaying && activeSource.clip == clip)
            {
                return;
            }

            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
            }

            musicFadeRoutine = StartCoroutine(CrossfadeMusic(clip, loop));
        }

        private void ReplayQueuedMusic()
        {
            if (queuedMusicClip == null)
            {
                return;
            }

            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
                musicFadeRoutine = null;
            }

            var activeSource = GetActiveMusicSource();
            activeSource.clip = queuedMusicClip;
            activeSource.loop = queuedMusicLoop;
            activeSource.volume = musicVolume;
            activeSource.Play();
        }

        private IEnumerator CrossfadeMusic(AudioClip nextClip, bool loop)
        {
            var fromSource = GetActiveMusicSource();
            var toSource = GetInactiveMusicSource();

            toSource.clip = nextClip;
            toSource.loop = loop;
            toSource.volume = 0f;
            toSource.Play();

            if (!fromSource.isPlaying || fromSource.clip == null || musicFadeDuration <= 0f)
            {
                fromSource.Stop();
                toSource.volume = musicVolume;
                SwapMusicSources();
                musicFadeRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(elapsed / musicFadeDuration);

                fromSource.volume = Mathf.Lerp(musicVolume, 0f, progress);
                toSource.volume = Mathf.Lerp(0f, musicVolume, progress);

                yield return null;
            }

            fromSource.Stop();
            fromSource.volume = 0f;
            toSource.volume = musicVolume;

            SwapMusicSources();
            musicFadeRoutine = null;
        }

        private void PlaySfx(AudioClip clip, float volumeScale)
        {
            if (!sfxEnabled || clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * sfxVolume);
        }

        private void ApplyVolumeState()
        {
            if (primaryMusicSource != null)
            {
                primaryMusicSource.volume = musicEnabled ? musicVolume : 0f;
            }

            if (secondaryMusicSource != null)
            {
                secondaryMusicSource.volume = musicEnabled ? musicVolume : 0f;
            }
        }

        private void StopMusicSources()
        {
            if (primaryMusicSource != null)
            {
                primaryMusicSource.Stop();
            }

            if (secondaryMusicSource != null)
            {
                secondaryMusicSource.Stop();
            }
        }

        private AudioSource GetActiveMusicSource()
        {
            if (secondaryMusicSource != null && secondaryMusicSource.isPlaying)
            {
                return secondaryMusicSource;
            }

            return primaryMusicSource;
        }

        private AudioSource GetInactiveMusicSource()
        {
            return GetActiveMusicSource() == primaryMusicSource
                ? secondaryMusicSource
                : primaryMusicSource;
        }

        private void SwapMusicSources()
        {
            var oldPrimary = primaryMusicSource;
            primaryMusicSource = secondaryMusicSource;
            secondaryMusicSource = oldPrimary;
        }
    }
}
