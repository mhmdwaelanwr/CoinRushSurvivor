using UnityEngine;

namespace CoinRushSurvivor.Progression
{
    public enum PermanentUpgradeType
    {
        StartingHealth,
        CoinGain,
        PickupRadius,
        ReviveHealthBonus
    }

    [CreateAssetMenu(fileName = "PermanentUpgrade_", menuName = "Coin Rush Survivor/Permanent Upgrade")]
    public sealed class PermanentUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string id = "permanent-upgrade-id";
        [SerializeField] private string displayName = "Permanent Upgrade";
        [SerializeField, TextArea] private string description = "Permanent upgrade description.";
        [SerializeField] private Sprite icon;
        [SerializeField] private PermanentUpgradeType upgradeType;
        [SerializeField, Min(0)] private int baseCost = 20;
        [SerializeField, Min(1f)] private float costGrowth = 1.5f;
        [SerializeField, Min(1)] private int maxLevel = 5;
        [SerializeField] private float effectPerLevel = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public PermanentUpgradeType UpgradeType => upgradeType;
        public int MaxLevel => maxLevel;
        public float EffectPerLevel => effectPerLevel;

        public int GetCostForLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel)
            {
                return 0;
            }

            var rawCost = baseCost * Mathf.Pow(costGrowth, Mathf.Max(0, currentLevel));
            return Mathf.Max(1, Mathf.RoundToInt(rawCost));
        }

        public float GetTotalBonus(int currentLevel)
        {
            return Mathf.Max(0, currentLevel) * effectPerLevel;
        }
    }
}
