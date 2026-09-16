using CoinRushSurvivor.Core;
using CoinRushSurvivor.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuPresenter : MonoBehaviour
    {
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

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
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(HandlePausePressed);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(HandleResumePressed);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(HandleBackToMenuPressed);
            }
        }

        private void UnbindButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(HandlePausePressed);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(HandleResumePressed);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(HandleBackToMenuPressed);
            }
        }

        private void Subscribe()
        {
            if (runDirector == null)
            {
                return;
            }

            runDirector.RunStarted -= HandleRunStarted;
            runDirector.RunStarted += HandleRunStarted;
            runDirector.RunEnded -= HandleRunEnded;
            runDirector.RunEnded += HandleRunEnded;
            runDirector.RunResumed -= HandleRunResumed;
            runDirector.RunResumed += HandleRunResumed;
        }

        private void Unsubscribe()
        {
            if (runDirector == null)
            {
                return;
            }

            runDirector.RunStarted -= HandleRunStarted;
            runDirector.RunEnded -= HandleRunEnded;
            runDirector.RunResumed -= HandleRunResumed;
        }

        private void HandleRunStarted(RunDirector director)
        {
            HidePanel();
        }

        private void HandleRunEnded(RunDirector director)
        {
            HidePanel();
        }

        private void HandleRunResumed(RunDirector director)
        {
            HidePanel();
        }

        private void HandlePausePressed()
        {
            if (runDirector == null || runDirector.State != RunState.Running)
            {
                return;
            }

            audioService?.PlayUiClick();
            runDirector.PauseRun();
            ShowPanel();
        }

        private void HandleResumePressed()
        {
            if (runDirector == null)
            {
                return;
            }

            audioService?.PlayUiClick();
            runDirector.ResumeRun();
            HidePanel();
        }

        private void HandleBackToMenuPressed()
        {
            audioService?.PlayUiClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
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
