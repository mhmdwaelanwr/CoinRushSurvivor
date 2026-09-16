using CoinRushSurvivor.Core;
using CoinRushSurvivor.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class MetaProgressionPresenter : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button upgradesTabButton;
        [SerializeField] private Button cosmeticsTabButton;
        [SerializeField] private Button shopTabButton;
        [SerializeField] private Button backButton;

        [Header("Panels")]
        [SerializeField] private GameObject upgradesPanel;
        [SerializeField] private GameObject cosmeticsPanel;
        [SerializeField] private GameObject shopPanel;

        [Header("Display")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text softCurrencyText;
        [SerializeField] private Text shopPlaceholderText;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private MetaScreenTab currentTab = MetaScreenTab.Upgrades;
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

            var requestedTab = menuNavigationState != null
                ? menuNavigationState.ConsumeRequestedMetaTab()
                : MetaScreenTab.Upgrades;

            SetActiveTab(requestedTab);
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
            if (upgradesTabButton != null)
            {
                upgradesTabButton.onClick.AddListener(HandleUpgradesTabPressed);
            }

            if (cosmeticsTabButton != null)
            {
                cosmeticsTabButton.onClick.AddListener(HandleCosmeticsTabPressed);
            }

            if (shopTabButton != null)
            {
                shopTabButton.onClick.AddListener(HandleShopTabPressed);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBackPressed);
            }
        }

        private void UnbindButtons()
        {
            if (upgradesTabButton != null)
            {
                upgradesTabButton.onClick.RemoveListener(HandleUpgradesTabPressed);
            }

            if (cosmeticsTabButton != null)
            {
                cosmeticsTabButton.onClick.RemoveListener(HandleCosmeticsTabPressed);
            }

            if (shopTabButton != null)
            {
                shopTabButton.onClick.RemoveListener(HandleShopTabPressed);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackPressed);
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
            if (profileService != null && softCurrencyText != null)
            {
                softCurrencyText.text = profileService.SoftCurrency.ToString();
            }

            if (titleText != null)
            {
                switch (currentTab)
                {
                    case MetaScreenTab.Upgrades:
                        titleText.text = "Permanent Upgrades";
                        break;

                    case MetaScreenTab.Cosmetics:
                        titleText.text = "Cosmetics";
                        break;

                    default:
                        titleText.text = "Shop";
                        break;
                }
            }

            if (shopPlaceholderText != null)
            {
                shopPlaceholderText.text = "Reward bundles, remove-ads, and cosmetic packs will be connected here later.";
            }
        }

        private void SetActiveTab(MetaScreenTab tab)
        {
            currentTab = tab;

            if (upgradesPanel != null)
            {
                upgradesPanel.SetActive(tab == MetaScreenTab.Upgrades);
            }

            if (cosmeticsPanel != null)
            {
                cosmeticsPanel.SetActive(tab == MetaScreenTab.Cosmetics);
            }

            if (shopPanel != null)
            {
                shopPanel.SetActive(tab == MetaScreenTab.Shop);
            }

            if (upgradesTabButton != null)
            {
                upgradesTabButton.interactable = tab != MetaScreenTab.Upgrades;
            }

            if (cosmeticsTabButton != null)
            {
                cosmeticsTabButton.interactable = tab != MetaScreenTab.Cosmetics;
            }

            if (shopTabButton != null)
            {
                shopTabButton.interactable = tab != MetaScreenTab.Shop;
            }

            Refresh();
        }

        private void HandleUpgradesTabPressed()
        {
            audioService?.PlayUiClick();
            SetActiveTab(MetaScreenTab.Upgrades);
        }

        private void HandleCosmeticsTabPressed()
        {
            audioService?.PlayUiClick();
            SetActiveTab(MetaScreenTab.Cosmetics);
        }

        private void HandleShopTabPressed()
        {
            audioService?.PlayUiClick();
            SetActiveTab(MetaScreenTab.Shop);
        }

        private void HandleBackPressed()
        {
            audioService?.PlayUiClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
