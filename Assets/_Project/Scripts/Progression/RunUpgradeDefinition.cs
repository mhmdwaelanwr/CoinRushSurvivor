using UnityEngine;

namespace CoinRushSurvivor.Progression
{
    public enum RunUpgradeEffectType
    {
        MoveSpeed,
        MagnetRadius,
        MaxHealth,
        AuraDamage,
        ShieldChance,
        CoinMultiplier
    }

    [CreateAssetMenu(fileName = "RunUpgrade_", menuName = "Coin Rush Survivor/Run Upgrade")]
    public sealed class RunUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string id = "upgrade-id";
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField, TextArea] private string description = "Upgrade description.";
        [SerializeField] private Sprite icon;
        [SerializeField] private RunUpgradeEffectType effectType;
        [SerializeField] private float effectValue = 1f;
        [SerializeField, Min(1)] private int maxStacks = 5;
        [SerializeField, Min(0.01f)] private float weight = 1f;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public RunUpgradeEffectType EffectType => effectType;
        public float EffectValue => effectValue;
        public int MaxStacks => maxStacks;
        public float Weight => weight;
    }
}
