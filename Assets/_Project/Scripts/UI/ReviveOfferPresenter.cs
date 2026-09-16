using CoinRushSurvivor.Core;
using CoinRushSurvivor.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class ReviveOfferPresenter : MonoBehaviour
    {
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button reviveButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        private AudioService audioService;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
            HidePanel();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            HidePanel();
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
            if (runDirector == null)
            {
                runDirector = FindFirstObjectByType<RunDirector>();
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
            if (reviveButton != null)
            {
                reviveButton.onClick.AddListener(HandleRevivePressed);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(HandleSkipPressed);
            }
        }

        private void UnbindButtons()
        {
            if (reviveButton != null)
            {
                reviveButton.onClick.RemoveListener(HandleRevivePressed);
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(HandleSkipPressed);
            }
        }

        private void Subscribe()
        {
            if (runDirector == null)
            {
                return;
            }

            runDirector.ReviveRequested -= HandleReviveRequested;
            runDirector.ReviveRequested += HandleReviveRequested;
            runDirector.RunStarted -= HandleRunStarted;
            runDirector.RunStarted += HandleRunStarted;
            runDirector.RunEnded -= HandleRunEnded;
            runDirector.RunEnded += HandleRunEnded;
        }

        private void Unsubscribe()
        {
            if (runDirector == null)
            {
                return;
            }

            runDirector.ReviveRequested -= HandleReviveRequested;
            runDirector.RunStarted -= HandleRunStarted;
            runDirector.RunEnded -= HandleRunEnded;
        }

        private void HandleRunStarted(RunDirector director)
        {
            HidePanel();
        }

        private void HandleRunEnded(RunDirector director)
        {
            HidePanel();
        }

        private void HandleReviveRequested(RunDirector director)
        {
            if (titleText != null)
            {
                titleText.text = "One More Chance";
            }

            if (bodyText != null)
            {
                var restoredPercent = Mathf.RoundToInt(director.ReviveHealthFraction * 100f);
                bodyText.text = $"Revive with {restoredPercent}% health and keep this run going.";
            }

            ShowPanel();
        }

        private void HandleRevivePressed()
        {
            if (runDirector == null)
            {
                return;
            }

            audioService?.PlayUiClick();
            if (runDirector.CompleteRevive())
            {
                HidePanel();
            }
        }

        private void HandleSkipPressed()
        {
            if (runDirector == null)
            {
                return;
            }

            audioService?.PlayUiClick();
            HidePanel();
            runDirector.CancelReviveOfferAndEndRun();
        }

        private void ShowPanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        private void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
