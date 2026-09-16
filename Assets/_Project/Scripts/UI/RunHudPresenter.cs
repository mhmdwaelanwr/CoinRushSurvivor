using CoinRushSurvivor.Gameplay;
using CoinRushSurvivor.Player;
using CoinRushSurvivor.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class RunHudPresenter : MonoBehaviour
    {
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private RunProgressionController progressionController;
        [SerializeField] private Health playerHealth;

        [Header("Texts")]
        [SerializeField] private Text timerText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text experienceText;
        [SerializeField] private Text healthText;

        [Header("Bars")]
        [SerializeField] private Image experienceFillImage;
        [SerializeField] private Image healthFillImage;

        private void Awake()
        {
            ResolveReferences();
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

            if (playerHealth == null)
            {
                var player = runDirector != null ? runDirector.Player : null;
                if (player == null)
                {
                    player = FindFirstObjectByType<PlayerController>();
                }

                playerHealth = player != null ? player.Health : null;
            }
        }

        private void Subscribe()
        {
            if (runDirector != null)
            {
                runDirector.RunStatsChanged -= HandleRunStatsChanged;
                runDirector.RunStatsChanged += HandleRunStatsChanged;
                runDirector.RunStarted -= HandleRunStarted;
                runDirector.RunStarted += HandleRunStarted;
                runDirector.RunEnded -= HandleRunEnded;
                runDirector.RunEnded += HandleRunEnded;
            }

            if (progressionController != null)
            {
                progressionController.ProgressChanged -= HandleProgressChanged;
                progressionController.ProgressChanged += HandleProgressChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
                playerHealth.HealthChanged += HandleHealthChanged;
            }
        }

        private void Unsubscribe()
        {
            if (runDirector != null)
            {
                runDirector.RunStatsChanged -= HandleRunStatsChanged;
                runDirector.RunStarted -= HandleRunStarted;
                runDirector.RunEnded -= HandleRunEnded;
            }

            if (progressionController != null)
            {
                progressionController.ProgressChanged -= HandleProgressChanged;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= HandleHealthChanged;
            }
        }

        private void HandleRunStarted(RunDirector director)
        {
            ResolveReferences();
            Refresh();
        }

        private void HandleRunEnded(RunDirector director)
        {
            Refresh();
        }

        private void HandleRunStatsChanged(RunDirector director)
        {
            Refresh();
        }

        private void HandleProgressChanged(RunProgressionController controller)
        {
            Refresh();
        }

        private void HandleHealthChanged(Health target, float current, float max)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (runDirector != null)
            {
                if (timerText != null)
                {
                    var totalSeconds = Mathf.FloorToInt(runDirector.ElapsedTime);
                    var minutes = totalSeconds / 60;
                    var seconds = totalSeconds % 60;
                    timerText.text = $"{minutes:00}:{seconds:00}";
                }

                if (scoreText != null)
                {
                    scoreText.text = runDirector.Score.ToString();
                }

                if (coinsText != null)
                {
                    coinsText.text = runDirector.CoinsCollected.ToString();
                }
            }

            if (playerHealth != null)
            {
                if (healthText != null)
                {
                    healthText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";
                }

                if (healthFillImage != null)
                {
                    var fillAmount = playerHealth.MaxHealth > 0f
                        ? playerHealth.CurrentHealth / playerHealth.MaxHealth
                        : 0f;
                    healthFillImage.fillAmount = Mathf.Clamp01(fillAmount);
                }
            }

            if (progressionController != null)
            {
                if (levelText != null)
                {
                    levelText.text = $"Lv {progressionController.CurrentLevel}";
                }

                if (experienceText != null)
                {
                    experienceText.text = $"{progressionController.CurrentExperience}/{progressionController.RequiredExperienceToNextLevel}";
                }

                if (experienceFillImage != null)
                {
                    experienceFillImage.fillAmount = progressionController.ProgressToNextLevel;
                }
            }
        }
    }
}
