using CoinRushSurvivor.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelUpOptionButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text stackText;
        [SerializeField] private Image iconImage;

        private LevelUpPopupPresenter owner;
        private RunUpgradeDefinition currentUpgrade;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Bind(LevelUpPopupPresenter popupPresenter, RunUpgradeDefinition upgrade, int currentStacks)
        {
            owner = popupPresenter;
            currentUpgrade = upgrade;

            var hasUpgrade = upgrade != null;

            if (button != null)
            {
                button.interactable = hasUpgrade;
            }

            gameObject.SetActive(hasUpgrade);
            if (!hasUpgrade)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = upgrade.DisplayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = upgrade.Description;
            }

            if (stackText != null)
            {
                stackText.text = $"Lv {currentStacks + 1}/{upgrade.MaxStacks}";
            }

            if (iconImage != null)
            {
                iconImage.enabled = upgrade.Icon != null;
                iconImage.sprite = upgrade.Icon;
            }
        }

        private void HandleClicked()
        {
            if (owner == null || currentUpgrade == null)
            {
                return;
            }

            owner.SelectOption(currentUpgrade);
        }
    }
}
