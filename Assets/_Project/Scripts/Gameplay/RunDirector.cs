using System;
using CoinRushSurvivor.Core;
using CoinRushSurvivor.Data;
using CoinRushSurvivor.Player;
using CoinRushSurvivor.Progression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinRushSurvivor.Gameplay
{
    public enum RunState
    {
        Idle,
        Running,
        Paused,
        AwaitingRevive,
        Ended
    }

    [DisallowMultipleComponent]
    public sealed class RunDirector : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private bool autoStartOnEnable = true;
        [SerializeField] private bool pauseTimeScaleOnEnd = true;

        [Header("Scoring")]
        [SerializeField, Min(0)] private int scorePerSecondSurvived = 5;

        [Header("Economy")]
        [SerializeField, Min(1f)] private float baseCoinMultiplier = 1f;

        [Header("Difficulty")]
        [SerializeField, Min(1f)] private float baseDifficulty = 1f;
        [SerializeField, Min(0f)] private float difficultyIncreasePerMinute = 0.35f;
        [SerializeField] private AnimationCurve difficultyOverTime = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(60f, 1.25f),
            new Keyframe(120f, 1.6f),
            new Keyframe(180f, 2.1f));

        [Header("Revive Hook")]
        [SerializeField] private bool requestReviveOnPlayerDeath;
        [SerializeField, Range(0.05f, 1f)] private float baseReviveHealthFraction = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool allowEditorRestartKey = true;
        [SerializeField] private KeyCode restartKey = KeyCode.R;

        private int actionScore;
        private int lastWholeSecond = -1;
        private bool reviveConsumed;
        private bool runResultCommitted;
        private float coinMultiplier;
        private float reviveHealthFraction;
        private PlayerProfileService profileService;
        private AudioService audioService;
        private RunState state = RunState.Idle;

        public event Action<RunDirector> RunStarted;
        public event Action<RunDirector> RunPaused;
        public event Action<RunDirector> RunResumed;
        public event Action<RunDirector> RunEnded;
        public event Action<RunDirector> RunStatsChanged;
        public event Action<RunDirector> ReviveRequested;

        public RunState State => state;
        public float ElapsedTime { get; private set; }
        public int CoinsCollected { get; private set; }
        public int ExperienceCollected { get; private set; }
        public int Score => actionScore + Mathf.FloorToInt(ElapsedTime * scorePerSecondSurvived);
        public bool IsRunActive => state == RunState.Running;
        public bool IsPaused => state == RunState.Paused || state == RunState.AwaitingRevive;
        public bool IsRunEnded => state == RunState.Ended;
        public bool ReviveConsumed => reviveConsumed;
        public Transform PlayerTransform => player != null ? player.transform : null;
        public PlayerController Player => player;
        public float DifficultyScalar => GetDifficultyScalar(ElapsedTime);
        public float CoinMultiplier => coinMultiplier;
        public float ReviveHealthFraction => reviveHealthFraction;

        private void Awake()
        {
            coinMultiplier = Mathf.Max(1f, baseCoinMultiplier);
            reviveHealthFraction = Mathf.Clamp01(baseReviveHealthFraction);
            ServiceLocator.Register(this);
            ResolvePlayerReferences();
            ResolveProfileService();
            ResolveAudioService();
            SubscribeToPlayerHealth();
        }

        private void OnEnable()
        {
            Time.timeScale = 1f;
            ResolveAudioService();
            audioService?.PlayGameplayMusic();

            if (autoStartOnEnable)
            {
                StartRun();
            }
        }

        private void Update()
        {
            HandleEditorRestart();

            if (state != RunState.Running)
            {
                return;
            }

            ElapsedTime += Time.deltaTime;

            var wholeSecond = Mathf.FloorToInt(ElapsedTime);
            if (wholeSecond != lastWholeSecond)
            {
                lastWholeSecond = wholeSecond;
                NotifyStatsChanged();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromPlayerHealth();

            if (ServiceLocator.TryGet<RunDirector>(out var registeredDirector) &&
                ReferenceEquals(registeredDirector, this))
            {
                ServiceLocator.Unregister(this);
            }

            Time.timeScale = 1f;
        }

        public void RegisterPlayer(PlayerController playerController)
        {
            if (ReferenceEquals(player, playerController))
            {
                return;
            }

            UnsubscribeFromPlayerHealth();

            player = playerController;
            playerHealth = player != null ? player.GetComponent<Health>() : null;

            SubscribeToPlayerHealth();
        }

        public void StartRun()
        {
            ResolvePlayerReferences();
            SubscribeToPlayerHealth();

            if (player != null)
            {
                player.ResetRuntimeStats();
            }

            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }

            ElapsedTime = 0f;
            CoinsCollected = 0;
            ExperienceCollected = 0;
            actionScore = 0;
            lastWholeSecond = -1;
            reviveConsumed = false;
            runResultCommitted = false;
            coinMultiplier = Mathf.Max(1f, baseCoinMultiplier);
            reviveHealthFraction = Mathf.Clamp01(baseReviveHealthFraction);
            state = RunState.Running;

            ApplyPermanentProfileBonuses();
            Time.timeScale = 1f;

            RunStarted?.Invoke(this);
            NotifyStatsChanged();
        }

        public void PauseRun()
        {
            if (state != RunState.Running)
            {
                return;
            }

            state = RunState.Paused;
            Time.timeScale = 0f;
            RunPaused?.Invoke(this);
        }

        public void ResumeRun()
        {
            if (state != RunState.Paused)
            {
                return;
            }

            state = RunState.Running;
            Time.timeScale = 1f;
            RunResumed?.Invoke(this);
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var rewardedCoins = Mathf.Max(0, Mathf.RoundToInt(amount * coinMultiplier));
            if (rewardedCoins <= 0)
            {
                return;
            }

            CoinsCollected += rewardedCoins;
            NotifyStatsChanged();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ExperienceCollected += amount;
            NotifyStatsChanged();
        }

        public void AddScore(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            actionScore += amount;
            NotifyStatsChanged();
        }

        public void AddCoinMultiplier(float amount)
        {
            coinMultiplier = Mathf.Max(1f, coinMultiplier + amount);
            NotifyStatsChanged();
        }

        public bool EnterReviveOfferState()
        {
            if (reviveConsumed || playerHealth == null || !playerHealth.IsDead)
            {
                return false;
            }

            state = RunState.AwaitingRevive;
            Time.timeScale = 0f;
            ReviveRequested?.Invoke(this);
            return true;
        }

        public bool CompleteRevive(float healthFraction = -1f)
        {
            if (reviveConsumed || playerHealth == null || !playerHealth.IsDead)
            {
                return false;
            }

            reviveConsumed = true;

            var resolvedHealthFraction = healthFraction > 0f
                ? healthFraction
                : reviveHealthFraction;

            playerHealth.Revive(resolvedHealthFraction);

            state = RunState.Running;
            Time.timeScale = 1f;
            NotifyStatsChanged();
            return true;
        }

        public void CancelReviveOfferAndEndRun()
        {
            if (state != RunState.AwaitingRevive)
            {
                return;
            }

            EndRun();
        }

        public void EndRun()
        {
            if (state == RunState.Ended)
            {
                return;
            }

            CommitRunResultIfNeeded();
            audioService?.PlayGameOverSting();
            state = RunState.Ended;
            Time.timeScale = pauseTimeScaleOnEnd ? 0f : 1f;

            RunEnded?.Invoke(this);
            NotifyStatsChanged();
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public float GetDifficultyScalar(float elapsedTime)
        {
            var time = Mathf.Max(0f, elapsedTime);
            var curveValue = difficultyOverTime != null && difficultyOverTime.length > 0
                ? difficultyOverTime.Evaluate(time)
                : 1f;
            var linearValue = baseDifficulty + (time / 60f) * difficultyIncreasePerMinute;

            return Mathf.Max(1f, Mathf.Max(curveValue * baseDifficulty, linearValue));
        }

        private void ResolvePlayerReferences()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }

            if (playerHealth == null && player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        private void ResolveProfileService()
        {
            if (profileService != null)
            {
                return;
            }

            if (!ServiceLocator.TryGet(out profileService))
            {
                profileService = FindFirstObjectByType<PlayerProfileService>();
            }
        }

        private void ResolveAudioService()
        {
            if (audioService != null)
            {
                return;
            }

            if (!ServiceLocator.TryGet(out audioService))
            {
                audioService = FindFirstObjectByType<AudioService>();
            }
        }

        private void SubscribeToPlayerHealth()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.Died -= HandlePlayerDied;
            playerHealth.Died += HandlePlayerDied;
        }

        private void UnsubscribeFromPlayerHealth()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.Died -= HandlePlayerDied;
        }

        private void HandlePlayerDied(Health targetHealth, GameObject source)
        {
            if (state != RunState.Running)
            {
                return;
            }

            if (requestReviveOnPlayerDeath && EnterReviveOfferState())
            {
                return;
            }

            EndRun();
        }

        private void ApplyPermanentProfileBonuses()
        {
            ResolveProfileService();
            if (profileService == null)
            {
                return;
            }

            var startingHealthBonus = profileService.GetPermanentUpgradeBonus(PermanentUpgradeType.StartingHealth);
            if (startingHealthBonus > 0f && player != null)
            {
                player.AddMaxHealth(startingHealthBonus, true);
            }

            var pickupRadiusBonus = profileService.GetPermanentUpgradeBonus(PermanentUpgradeType.PickupRadius);
            if (pickupRadiusBonus > 0f && player != null)
            {
                player.AddMagnetRadius(pickupRadiusBonus);
            }

            var coinGainBonus = profileService.GetPermanentUpgradeBonus(PermanentUpgradeType.CoinGain);
            if (coinGainBonus > 0f)
            {
                AddCoinMultiplier(coinGainBonus);
            }

            reviveHealthFraction = profileService.GetReviveHealthFraction(baseReviveHealthFraction);
        }

        private void CommitRunResultIfNeeded()
        {
            if (runResultCommitted)
            {
                return;
            }

            runResultCommitted = true;
            ResolveProfileService();
            profileService?.CommitRunResult(CoinsCollected);
        }

        private void NotifyStatsChanged()
        {
            RunStatsChanged?.Invoke(this);
        }

        private void HandleEditorRestart()
        {
#if UNITY_EDITOR
            if (!allowEditorRestartKey || !Input.GetKeyDown(restartKey))
            {
                return;
            }

            RestartCurrentScene();
#endif
        }
    }
}
