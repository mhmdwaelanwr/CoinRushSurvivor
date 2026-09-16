using UnityEngine;

namespace CoinRushSurvivor.Progression
{
    [CreateAssetMenu(fileName = "Cosmetic_", menuName = "Coin Rush Survivor/Cosmetic")]
    public sealed class CosmeticDefinition : ScriptableObject
    {
        [SerializeField] private string id = "cosmetic-id";
        [SerializeField] private string displayName = "Cosmetic";
        [SerializeField, Min(0)] private int unlockCost = 0;
        [SerializeField] private Sprite previewIcon;
        [SerializeField] private Color previewColor = Color.white;
        [SerializeField] private bool defaultUnlocked = true;

        public string Id => id;
        public string DisplayName => displayName;
        public int UnlockCost => unlockCost;
        public Sprite PreviewIcon => previewIcon;
        public Color PreviewColor => previewColor;
        public bool DefaultUnlocked => defaultUnlocked;
    }
}
