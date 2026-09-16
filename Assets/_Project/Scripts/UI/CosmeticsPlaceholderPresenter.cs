using System;
using CoinRushSurvivor.Core;
using CoinRushSurvivor.Data;
using CoinRushSurvivor.Progression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class CosmeticsPlaceholderPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class CosmeticEntryView
        {
            public CosmeticDefinition definition;
            public GameObject cardRoot;
            public Button actionButton;
            public Text titleText;
            public Text stateText;
            public Text costText;
            public Text actionText;
            public Image iconImage;
            public Image colorSwatch;
        }

        [SerializeField] private CosmeticEntryView[] entries = Array.Empty<CosmeticEntryView>();
        [SerializeField] private Text selectedCosmeticText;

        private AudioService audioService;
        private PlayerProfileService profileService;
        private UnityAction[] buttonActions = Array.Empty<UnityAction>();

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
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
        }

        private void BindButtons()
        {
            buttonActions = new UnityAction[entries.Length];

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null || entries[i].actionButton == null)
                {
                    continue;
                }

                var capturedIndex = i;
                buttonActions[i] = () => HandleEntryPressed(capturedIndex);
                entries[i].actionButton.onClick.AddListener(buttonActions[i]);
            }
        }

        private void UnbindButtons()
        {
            for (var i = 0; i < entries.Length && i < buttonActions.Length; i++)
            {
                if (entries[i] == null || entries[i].actionButton == null || buttonActions[i] == null)
                {
                    continue;
                }

                entries[i].actionButton.onClick.RemoveListener(buttonActions[i]);
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

            var selectedCosmetic = profileService.GetSelectedCosmetic();
            if (selectedCosmeticText != null)
            {
                selectedCosmeticText.text = selectedCosmetic != null
                    ? $"Selected: {selectedCosmetic.DisplayName}"
                    : "Selected: None";
            }

            for (var i = 0; i < entries.Length; i++)
            {
                RefreshEntry(entries[i], selectedCosmetic);
            }
        }

        private void RefreshEntry(CosmeticEntryView entry, CosmeticDefinition selectedCosmetic)
        {
            if (entry == null)
            {
                return;
            }

            var hasDefinition = entry.definition != null;

            if (entry.cardRoot != null && entry.cardRoot != gameObject)
            {
                entry.cardRoot.SetActive(hasDefinition);
            }

            if (!hasDefinition || profileService == null)
            {
                return;
            }

            var isUnlocked = profileService.IsCosmeticUnlocked(entry.definition);
            var isSelected = selectedCosmetic == entry.definition;

            if (entry.titleText != null)
            {
                entry.titleText.text = entry.definition.DisplayName;
            }

            if (entry.stateText != null)
            {
                entry.stateText.text = isSelected
                    ? "Selected"
                    : isUnlocked ? "Unlocked" : "Locked";
            }

            if (entry.costText != null)
            {
                entry.costText.text = isUnlocked
                    ? "Owned"
                    : $"{entry.definition.UnlockCost} Coins";
            }

            if (entry.actionText != null)
            {
                entry.actionText.text = isSelected
                    ? "Selected"
                    : isUnlocked ? "Select" : "Unlock";
            }

            if (entry.iconImage != null)
            {
                entry.iconImage.enabled = entry.definition.PreviewIcon != null;
                entry.iconImage.sprite = entry.definition.PreviewIcon;
            }

            if (entry.colorSwatch != null)
            {
                entry.colorSwatch.color = entry.definition.PreviewColor;
            }

            if (entry.actionButton != null)
            {
                entry.actionButton.interactable = !isSelected && (isUnlocked || profileService.SoftCurrency >= entry.definition.UnlockCost);
            }
        }

        private void HandleEntryPressed(int index)
        {
            if (profileService == null || index < 0 || index >= entries.Length || entries[index] == null || entries[index].definition == null)
            {
                return;
            }

            var cosmetic = entries[index].definition;
            var isUnlocked = profileService.IsCosmeticUnlocked(cosmetic);
            var changed = isUnlocked
                ? profileService.TrySelectCosmetic(cosmetic)
                : profileService.TryUnlockCosmetic(cosmetic);

            if (changed)
            {
                audioService?.PlayUiClick();
            }
        }
    }
}
