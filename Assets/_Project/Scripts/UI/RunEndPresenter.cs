using CoinRushSurvivor.Gameplay;
using CoinRushSurvivor.Progression;
using CoinRushSurvivor.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class RunEndPresenter : MonoBehaviour
    {
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private RunProgressionController progressionController;
        [SerializeField] private GameObject panelRoot;

        [Header("Summary Text")]
        [SerializeField] private Text timeValueText;
        [SerializeField] private Text scoreValueText;
        [SerializeField] private Text coinsValueText;
        [SerializeField] private Text levelValueText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private AudioService audioService;

        private void Awake()
        {
            ResolveReferences();

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartRun);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(GoToMainMenu);
            }

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
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartRun);
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.RemoveListener(GoToMainMenu);
            }
        }

        private void ResolveReferences()
        {
            if (runDirector == null)
            {
                runDirector = FindFirstObjectByType<RunDirector>();
            }

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
            if (runDirector == null)
            {
                return;
            }

            runDirector.RunEnded -= HandleRunEnded;
            runDirector.RunEnded += HandleRunEnded;
            runDirector.RunStarted -= HandleRunStarted;
            runDirector.RunStarted += HandleRunStarted;
        }

        private void Unsubscribe()
        {
            if (runDirector == null)
            {
                return;
            }

            runDirector.RunEnded -= HandleRunEnded;
            runDirector.RunStarted -= HandleRunStarted;
        }

        private void HandleRunStarted(RunDirector director)
        {
            HidePanel();
        }

        private void HandleRunEnded(RunDirector director)
        {
            if (timeValueText != null)
            {
                var totalSeconds = Mathf.FloorToInt(director.ElapsedTime);
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                timeValueText.text = $"{minutes:00}:{seconds:00}";
            }

            if (scoreValueText != null)
            {
                scoreValueText.text = director.Score.ToString();
            }

            if (coinsValueText != null)
            {
                coinsValueText.text = director.CoinsCollected.ToString();
            }

            if (levelValueText != null)
            {
                var level = progressionController != null ? progressionController.CurrentLevel : 1;
                levelValueText.text = level.ToString();
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        private void RestartRun()
        {
            if (runDirector == null)
            {
                return;
            }

            audioService?.PlayUiClick();
            runDirector.RestartCurrentScene();
        }

        private void GoToMainMenu()
        {
            audioService?.PlayUiClick();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void HidePanel()
        {
            if (panelRoot != null && panelRoot != gameObject)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
