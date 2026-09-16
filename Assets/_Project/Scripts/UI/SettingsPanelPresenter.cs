using CoinRushSurvivor.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text musicValueText;
        [SerializeField] private Text sfxValueText;

        private AudioService audioService;

        private void Awake()
        {
            ResolveReferences();
            BindControls();
            Hide();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Refresh();
        }

        private void OnDestroy()
        {
            UnbindControls();
        }

        public void Open()
        {
            var target = panelRoot != null ? panelRoot : gameObject;
            target.SetActive(true);

            audioService?.PlayUiClick();
            Refresh();
        }

        public void Hide()
        {
            var target = panelRoot != null ? panelRoot : gameObject;
            target.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (audioService == null)
            {
                audioService = ServiceLocator.TryGet<AudioService>(out var sharedAudioService)
                    ? sharedAudioService
                    : FindFirstObjectByType<AudioService>();
            }
        }

        private void BindControls()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HandleClosePressed);
            }

            if (musicToggle != null)
            {
                musicToggle.onValueChanged.AddListener(HandleMusicToggleChanged);
            }

            if (sfxToggle != null)
            {
                sfxToggle.onValueChanged.AddListener(HandleSfxToggleChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged);
            }
        }

        private void UnbindControls()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleClosePressed);
            }

            if (musicToggle != null)
            {
                musicToggle.onValueChanged.RemoveListener(HandleMusicToggleChanged);
            }

            if (sfxToggle != null)
            {
                sfxToggle.onValueChanged.RemoveListener(HandleSfxToggleChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(HandleSfxVolumeChanged);
            }
        }

        private void Refresh()
        {
            if (audioService == null)
            {
                return;
            }

            if (musicToggle != null)
            {
                musicToggle.SetIsOnWithoutNotify(audioService.MusicEnabled);
            }

            if (sfxToggle != null)
            {
                sfxToggle.SetIsOnWithoutNotify(audioService.SfxEnabled);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(audioService.MusicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(audioService.SfxVolume);
            }

            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (musicValueText != null)
            {
                var volume = musicVolumeSlider != null ? musicVolumeSlider.value : 0f;
                musicValueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
            }

            if (sfxValueText != null)
            {
                var volume = sfxVolumeSlider != null ? sfxVolumeSlider.value : 0f;
                sfxValueText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
            }
        }

        private void HandleClosePressed()
        {
            audioService?.PlayUiClick();
            Hide();
        }

        private void HandleMusicToggleChanged(bool enabled)
        {
            audioService?.SetMusicEnabled(enabled);
            RefreshLabels();
        }

        private void HandleSfxToggleChanged(bool enabled)
        {
            audioService?.SetSfxEnabled(enabled);
            audioService?.PlayUiClick();
            RefreshLabels();
        }

        private void HandleMusicVolumeChanged(float value)
        {
            audioService?.SetMusicVolume(value);
            RefreshLabels();
        }

        private void HandleSfxVolumeChanged(float value)
        {
            audioService?.SetSfxVolume(value);
            audioService?.PlayUiClick();
            RefreshLabels();
        }
    }
}
