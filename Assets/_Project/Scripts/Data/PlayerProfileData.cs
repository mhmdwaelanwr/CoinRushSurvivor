using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoinRushSurvivor.Data
{
    [Serializable]
    public sealed class ProfileUpgradeEntry
    {
        public string id = string.Empty;
        public int level;
    }

    [Serializable]
    public sealed class PlayerProfileData
    {
        public int saveVersion = 1;
        public int softCurrency;
        public int sessionsPlayed;
        public bool removeAdsPurchased;
        public string selectedCosmeticId = string.Empty;
        public List<string> unlockedCosmeticIds = new List<string>();
        public List<ProfileUpgradeEntry> upgradeLevels = new List<ProfileUpgradeEntry>();

        public static PlayerProfileData CreateDefault(int saveVersion)
        {
            var data = new PlayerProfileData();
            data.EnsureInitialized(saveVersion);
            return data;
        }

        public void EnsureInitialized(int currentSaveVersion)
        {
            saveVersion = Mathf.Max(1, currentSaveVersion);
            softCurrency = Mathf.Max(0, softCurrency);
            sessionsPlayed = Mathf.Max(0, sessionsPlayed);
            selectedCosmeticId = selectedCosmeticId ?? string.Empty;
            unlockedCosmeticIds ??= new List<string>();
            upgradeLevels ??= new List<ProfileUpgradeEntry>();

            RemoveInvalidUnlockedCosmetics();
            RemoveInvalidUpgradeEntries();
        }

        private void RemoveInvalidUnlockedCosmetics()
        {
            var seenIds = new HashSet<string>();

            for (var i = unlockedCosmeticIds.Count - 1; i >= 0; i--)
            {
                var cosmeticId = unlockedCosmeticIds[i];
                if (string.IsNullOrWhiteSpace(cosmeticId) || !seenIds.Add(cosmeticId))
                {
                    unlockedCosmeticIds.RemoveAt(i);
                }
            }
        }

        private void RemoveInvalidUpgradeEntries()
        {
            var seenIds = new HashSet<string>();

            for (var i = upgradeLevels.Count - 1; i >= 0; i--)
            {
                var entry = upgradeLevels[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || !seenIds.Add(entry.id))
                {
                    upgradeLevels.RemoveAt(i);
                    continue;
                }

                entry.level = Mathf.Max(0, entry.level);
            }
        }
    }
}
