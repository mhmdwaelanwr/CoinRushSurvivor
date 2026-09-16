using System;
using System.Collections.Generic;
using CoinRushSurvivor.Core;
using CoinRushSurvivor.Progression;
using UnityEngine;

namespace CoinRushSurvivor.Data
{
    [DisallowMultipleComponent]
    public sealed class PlayerProfileService : MonoBehaviour
    {
        [SerializeField, Min(1)] private int currentSaveVersion = 1;
        [SerializeField] private PermanentUpgradeDefinition[] permanentUpgrades = Array.Empty<PermanentUpgradeDefinition>();
        [SerializeField] private CosmeticDefinition[] cosmetics = Array.Empty<CosmeticDefinition>();

        private static PlayerProfileService instance;
        private SaveService saveService;

        public event Action<PlayerProfileService> ProfileChanged;

        public PlayerProfileData Profile { get; private set; }
        public IReadOnlyList<PermanentUpgradeDefinition> PermanentUpgrades => permanentUpgrades;
        public IReadOnlyList<CosmeticDefinition> Cosmetics => cosmetics;
        public int SoftCurrency => Profile != null ? Profile.softCurrency : 0;
        public int SessionsPlayed => Profile != null ? Profile.sessionsPlayed : 0;
        public bool RemoveAdsPurchased => Profile != null && Profile.removeAdsPurchased;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            ResolveSaveService();

            Profile = saveService != null
                ? saveService.LoadOrCreateProfile(currentSaveVersion)
                : PlayerProfileData.CreateDefault(currentSaveVersion);

            var changed = EnsureCatalogDefaults();
            ServiceLocator.Register(this);

            if (changed)
            {
                SaveProfile();
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            if (ServiceLocator.TryGet<PlayerProfileService>(out var registeredService) &&
                ReferenceEquals(registeredService, this))
            {
                ServiceLocator.Unregister(this);
            }

            instance = null;
        }

        public int GetUpgradeLevel(PermanentUpgradeDefinition definition)
        {
            if (definition == null || Profile == null)
            {
                return 0;
            }

            for (var i = 0; i < Profile.upgradeLevels.Count; i++)
            {
                var entry = Profile.upgradeLevels[i];
                if (entry != null && entry.id == definition.Id)
                {
                    return Mathf.Clamp(entry.level, 0, definition.MaxLevel);
                }
            }

            return 0;
        }

        public int GetNextUpgradeCost(PermanentUpgradeDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return definition.GetCostForLevel(GetUpgradeLevel(definition));
        }

        public float GetPermanentUpgradeBonus(PermanentUpgradeType upgradeType)
        {
            var totalBonus = 0f;

            for (var i = 0; i < permanentUpgrades.Length; i++)
            {
                var definition = permanentUpgrades[i];
                if (definition == null || definition.UpgradeType != upgradeType)
                {
                    continue;
                }

                totalBonus += definition.GetTotalBonus(GetUpgradeLevel(definition));
            }

            return totalBonus;
        }

        public float GetReviveHealthFraction(float baseHealthFraction)
        {
            return Mathf.Clamp01(baseHealthFraction + GetPermanentUpgradeBonus(PermanentUpgradeType.ReviveHealthBonus));
        }

        public bool CanPurchaseUpgrade(PermanentUpgradeDefinition definition)
        {
            if (definition == null || Profile == null)
            {
                return false;
            }

            var currentLevel = GetUpgradeLevel(definition);
            if (currentLevel >= definition.MaxLevel)
            {
                return false;
            }

            return Profile.softCurrency >= GetNextUpgradeCost(definition);
        }

        public bool TryPurchaseUpgrade(PermanentUpgradeDefinition definition)
        {
            if (definition == null || Profile == null)
            {
                return false;
            }

            var currentLevel = GetUpgradeLevel(definition);
            if (currentLevel >= definition.MaxLevel)
            {
                return false;
            }

            var nextCost = definition.GetCostForLevel(currentLevel);
            if (Profile.softCurrency < nextCost)
            {
                return false;
            }

            Profile.softCurrency -= nextCost;
            GetOrCreateUpgradeEntry(definition.Id).level = currentLevel + 1;

            SaveAndNotify();
            return true;
        }

        public bool IsCosmeticUnlocked(CosmeticDefinition definition)
        {
            if (definition == null || Profile == null)
            {
                return false;
            }

            for (var i = 0; i < Profile.unlockedCosmeticIds.Count; i++)
            {
                if (Profile.unlockedCosmeticIds[i] == definition.Id)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryUnlockCosmetic(CosmeticDefinition definition)
        {
            if (definition == null || Profile == null)
            {
                return false;
            }

            if (IsCosmeticUnlocked(definition))
            {
                return true;
            }

            if (Profile.softCurrency < definition.UnlockCost)
            {
                return false;
            }

            Profile.softCurrency -= definition.UnlockCost;
            Profile.unlockedCosmeticIds.Add(definition.Id);

            if (string.IsNullOrWhiteSpace(Profile.selectedCosmeticId))
            {
                Profile.selectedCosmeticId = definition.Id;
            }

            SaveAndNotify();
            return true;
        }

        public bool TrySelectCosmetic(CosmeticDefinition definition)
        {
            if (definition == null || !IsCosmeticUnlocked(definition) || Profile == null)
            {
                return false;
            }

            if (Profile.selectedCosmeticId == definition.Id)
            {
                return true;
            }

            Profile.selectedCosmeticId = definition.Id;
            SaveAndNotify();
            return true;
        }

        public CosmeticDefinition GetSelectedCosmetic()
        {
            if (Profile == null || string.IsNullOrWhiteSpace(Profile.selectedCosmeticId))
            {
                return null;
            }

            return GetCosmetic(Profile.selectedCosmeticId);
        }

        public CosmeticDefinition GetCosmetic(string cosmeticId)
        {
            if (string.IsNullOrWhiteSpace(cosmeticId))
            {
                return null;
            }

            for (var i = 0; i < cosmetics.Length; i++)
            {
                if (cosmetics[i] != null && cosmetics[i].Id == cosmeticId)
                {
                    return cosmetics[i];
                }
            }

            return null;
        }

        public void CommitRunResult(int collectedCoins)
        {
            if (Profile == null)
            {
                return;
            }

            Profile.softCurrency += Mathf.Max(0, collectedCoins);
            Profile.sessionsPlayed += 1;
            SaveAndNotify();
        }

        public void SaveProfile()
        {
            if (saveService == null || Profile == null)
            {
                return;
            }

            saveService.SaveProfile(Profile);
        }

        private void ResolveSaveService()
        {
            if (saveService != null)
            {
                return;
            }

            saveService = GetComponent<SaveService>();

            if (saveService == null && ServiceLocator.TryGet<SaveService>(out var sharedSaveService))
            {
                saveService = sharedSaveService;
            }
        }

        private ProfileUpgradeEntry GetOrCreateUpgradeEntry(string upgradeId)
        {
            for (var i = 0; i < Profile.upgradeLevels.Count; i++)
            {
                var entry = Profile.upgradeLevels[i];
                if (entry != null && entry.id == upgradeId)
                {
                    return entry;
                }
            }

            var newEntry = new ProfileUpgradeEntry
            {
                id = upgradeId,
                level = 0
            };

            Profile.upgradeLevels.Add(newEntry);
            return newEntry;
        }

        private bool EnsureCatalogDefaults()
        {
            if (Profile == null)
            {
                return false;
            }

            Profile.EnsureInitialized(currentSaveVersion);

            var changed = false;

            for (var i = 0; i < cosmetics.Length; i++)
            {
                var cosmetic = cosmetics[i];
                if (cosmetic == null || !cosmetic.DefaultUnlocked || IsCosmeticUnlocked(cosmetic))
                {
                    continue;
                }

                Profile.unlockedCosmeticIds.Add(cosmetic.Id);
                changed = true;
            }

            if (Profile.unlockedCosmeticIds.Count == 0)
            {
                for (var i = 0; i < cosmetics.Length; i++)
                {
                    if (cosmetics[i] == null)
                    {
                        continue;
                    }

                    Profile.unlockedCosmeticIds.Add(cosmetics[i].Id);
                    changed = true;
                    break;
                }
            }

            if (GetSelectedCosmetic() == null)
            {
                Profile.selectedCosmeticId = Profile.unlockedCosmeticIds.Count > 0
                    ? Profile.unlockedCosmeticIds[0]
                    : string.Empty;
                changed = true;
            }

            for (var i = Profile.upgradeLevels.Count - 1; i >= 0; i--)
            {
                var entry = Profile.upgradeLevels[i];
                var definition = GetPermanentUpgradeDefinition(entry != null ? entry.id : string.Empty);

                if (entry == null || definition == null)
                {
                    Profile.upgradeLevels.RemoveAt(i);
                    changed = true;
                    continue;
                }

                var clampedLevel = Mathf.Clamp(entry.level, 0, definition.MaxLevel);
                if (clampedLevel != entry.level)
                {
                    entry.level = clampedLevel;
                    changed = true;
                }
            }

            return changed;
        }

        private PermanentUpgradeDefinition GetPermanentUpgradeDefinition(string upgradeId)
        {
            if (string.IsNullOrWhiteSpace(upgradeId))
            {
                return null;
            }

            for (var i = 0; i < permanentUpgrades.Length; i++)
            {
                if (permanentUpgrades[i] != null && permanentUpgrades[i].Id == upgradeId)
                {
                    return permanentUpgrades[i];
                }
            }

            return null;
        }

        private void SaveAndNotify()
        {
            SaveProfile();
            ProfileChanged?.Invoke(this);
        }
    }
}
