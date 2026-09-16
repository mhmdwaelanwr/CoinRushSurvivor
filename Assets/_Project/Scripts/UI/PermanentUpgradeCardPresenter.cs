using CoinRushSurvivor.Data;
using CoinRushSurvivor.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class PermanentUpgradeCardPresenter : MonoBehaviour
    {
        [SerializeField] private PermanentUpgradeDefinition upgradeDefinition;
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text costText;
        [SerializeField] private Text bonusText;
        [SerializeField] private Text buttonText;
        [SerializeField] private Image iconImage;

        private CoinRushSurvivor.Core.AudioService audioService;
        private PlayerProfileService profileService;

        private void Awake()
        {
            ResolveReferences();

            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(HandlePurchasePressed);
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(HandlePurchasePressed);
            }
        }

        private void ResolveReferences()
        {
            if (profileService == null)
            {
                profileService = CoinRushSurvivor.Core.ServiceLocator.TryGet<PlayerProfileService>(out var sharedProfileService)
                    ? sharedProfileService
                    : FindFirstObjectByType<PlayerProfileService>();
            }

            if (audioService == null)
            {
                audioService = CoinRushSurvivor.Core.ServiceLocator.TryGet<CoinRushSurvivor.Core.AudioService>(out var sharedAudioService)
                    ? sharedAudioService
                    : FindFirstObjectByType<CoinRushSurvivor.Core.AudioService>();
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
            var shouldShow = upgradeDefinition != null;

            if (cardRoot != null && cardRoot != gameObject)
            {
                cardRoot.SetActive(shouldShow);
            }

            if (!shouldShow || profileService == null)
            {
                return;
            }

            var currentLevel = profileService.GetUpgradeLevel(upgradeDefinition);
            var isMaxed = currentLevel >= upgradeDefinition.MaxLevel;
            var nextCost = profileService.GetNextUpgradeCost(upgradeDefinition);
            var canPurchase = profileService.CanPurchaseUpgrade(upgradeDefinition);

            if (titleText != null)
            {
                titleText.text = upgradeDefinition.DisplayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = upgradeDefinition.Description;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv {currentLevel}/{upgradeDefinition.MaxLevel}";
            }

            if (costText != null)
            {
                costText.text = isMaxed ? "MAX" : nextCost.ToString();
            }

            if (bonusText != null)
            {
                bonusText.text = $"+{upgradeDefinition.EffectPerLevel:0.##} / level";
            }

            if (buttonText != null)
            {
                buttonText.text = isMaxed
                    ? "MAX"
                    : canPurchase ? "Buy" : "Need Coins";
            }

            if (iconImage != null)
            {
                iconImage.enabled = upgradeDefinition.Icon != null;
                iconImage.sprite = upgradeDefinition.Icon;
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable = !isMaxed && canPurchase;
            }
        }

        private void HandlePurchasePressed()
        {
            if (profileService == null || upgradeDefinition == null)
            {
                return;
            }

            if (profileService.TryPurchaseUpgrade(upgradeDefinition))
            {
                audioService?.PlayUiClick();
            }
        }
    }
}
