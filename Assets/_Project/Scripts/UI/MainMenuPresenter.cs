using CoinRushSurvivor.Core;
using CoinRushSurvivor.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button cosmeticsButton;
        [SerializeField] private Button settingsButton;

        [Header("Display")]
        [SerializeField] private Text softCurrencyText;
        [SerializeField] private Text selectedCosmeticText;
        [SerializeField] private Text sessionsText;
        [SerializeField] private SettingsPanelPresenter settingsPanelPresenter;

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private string metaProgressionSceneName = "MetaProgression";

        private AudioService audioService;
        private PlayerProfileService profileService;
        private MenuNavigationState menuNavigationState;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            audioService?.PlayMenuMusic();
            settingsPanelPresenter?.Hide();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void ResolveReferences()
        {
            if (profileService == null)
            {
                profileService = ServiceLocator.TryGet<PlayerProfileService>(out var sharedProfileService)
                    ? sharedProfileService
                    : FindFirstObjectByType<PlayerProfileService>();
            }

            if (audioService == null)
            {
                audioService = ServiceLocator.TryGet<AudioService>(out var sharedAudioService)
                    ? sharedAudioService
                    : FindFirstObjectByType<AudioService>();
            }

            if (menuNavigationState == null)
            {
                menuNavigationState = ServiceLocator.TryGet<MenuNavigationState>(out var sharedNavigationState)
                    ? sharedNavigationState
                    : FindFirstObjectByType<MenuNavigationState>();
            }
        }

        private void BindButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(HandlePlayPressed);
            }

            if (upgradesButton != null)
            {
                upgradesButton.onClick.AddListener(HandleUpgradesPressed);
            }

            if (cosmeticsButton != null)
            {
                cosmeticsButton.onClick.AddListener(HandleCosmeticsPressed);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(HandleSettingsPressed);
            }
        }

        private void UnbindButtons()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(HandlePlayPressed);
            }

            if (upgradesButton != null)
            {
                upgradesButton.onClick.RemoveListener(HandleUpgradesPressed);
            }

            if (cosmeticsButton != null)
            {
                cosmeticsButton.onClick.RemoveListener(HandleCosmeticsPressed);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsPressed);
            }
        }

        private void Subscribe()
        {
            if (profileService == null)
            {
                return;
            }

            profileService.ProfileChanged -= HandleProfileChanged;
            profileService.ProfileChanged += HandleProfileChanged;
        }

        private void Unsubscribe()
        {
            if (profileService == null)
            {
                return;
            }

            profileService.ProfileChanged -= HandleProfileChanged;
        }

        private void HandleProfileChanged(PlayerProfileService service)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (profileService == null)
            {
                return;
            }

            if (softCurrencyText != null)
            {
                softCurrencyText.text = profileService.SoftCurrency.ToString();
            }

            if (sessionsText != null)
            {
                sessionsText.text = profileService.SessionsPlayed.ToString();
            }

            if (selectedCosmeticText != null)
            {
                var selectedCosmetic = profileService.GetSelectedCosmetic();
                selectedCosmeticText.text = selectedCosmetic != null
                    ? selectedCosmetic.DisplayName
                    : "None";
            }
        }

        private void HandlePlayPressed()
        {
            audioService?.PlayUiClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }

        private void HandleUpgradesPressed()
        {
            OpenMetaScene(MetaScreenTab.Upgrades);
        }

        private void HandleCosmeticsPressed()
        {
            OpenMetaScene(MetaScreenTab.Cosmetics);
        }

        private void HandleSettingsPressed()
        {
            settingsPanelPresenter?.Open();
        }

        private void OpenMetaScene(MetaScreenTab tab)
        {
            audioService?.PlayUiClick();
            Time.timeScale = 1f;
            menuNavigationState?.RequestMetaTab(tab);
            SceneManager.LoadScene(metaProgressionSceneName);
        }
    }
}
