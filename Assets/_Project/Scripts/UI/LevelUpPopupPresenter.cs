using CoinRushSurvivor.Progression;
using CoinRushSurvivor.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelUpPopupPresenter : MonoBehaviour
    {
        [SerializeField] private RunProgressionController progressionController;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private LevelUpOptionButton[] optionButtons;

        private AudioService audioService;
        private bool wasShowingPanel;

        private void Awake()
        {
            ResolveReferences();

            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(false);
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

        public void SelectOption(RunUpgradeDefinition upgrade)
        {
            if (progressionController == null)
            {
                return;
            }

            audioService?.PlayUiClick();
            progressionController.ApplyUpgrade(upgrade);
        }

        private void ResolveReferences()
        {
            if (progressionController == null)
            {
                progressionController = FindFirstObjectByType<RunProgressionController>();
            }

            if (audioService == null)
            {
                audioService = ServiceLocator.TryGet<AudioService>(out var sharedAudioService)
                    ? sharedAudioService
                    : FindFirstObjectByType<AudioService>();
            }
        }

        private void Subscribe()
        {
            if (progressionController == null)
            {
                return;
            }

            progressionController.ChoicesChanged -= HandleChoicesChanged;
            progressionController.ChoicesChanged += HandleChoicesChanged;
        }

        private void Unsubscribe()
        {
            if (progressionController == null)
            {
                return;
            }

            progressionController.ChoicesChanged -= HandleChoicesChanged;
        }

        private void HandleChoicesChanged(RunProgressionController controller)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (progressionController == null)
            {
                return;
            }

            var showPanel = progressionController.IsAwaitingSelection && progressionController.CurrentChoices.Count > 0;
            if (showPanel && !wasShowingPanel)
            {
                audioService?.PlayLevelUpSting();
            }

            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(showPanel);
            }

            wasShowingPanel = showPanel;

            if (!showPanel)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = $"Level {progressionController.CurrentLevel} Upgrade";
            }

            for (var i = 0; i < optionButtons.Length; i++)
            {
                var upgrade = i < progressionController.CurrentChoices.Count
                    ? progressionController.CurrentChoices[i]
                    : null;
                var stacks = upgrade != null ? progressionController.GetStackCount(upgrade) : 0;

                if (optionButtons[i] != null)
                {
                    optionButtons[i].Bind(this, upgrade, stacks);
                }
            }
        }
    }
}
