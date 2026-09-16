using System;
using System.Collections.Generic;
using CoinRushSurvivor.Gameplay;
using CoinRushSurvivor.Player;
using UnityEngine;

namespace CoinRushSurvivor.Progression
{
    [DisallowMultipleComponent]
    public sealed class RunProgressionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private PlayerController player;

        [Header("Experience")]
        [SerializeField, Min(1)] private int startingLevel = 1;
        [SerializeField, Min(1)] private int baseExperienceToLevel = 5;
        [SerializeField, Min(1f)] private float experienceGrowthMultiplier = 1.35f;

        [Header("Upgrade Catalog")]
        [SerializeField] private RunUpgradeDefinition[] upgradeCatalog = Array.Empty<RunUpgradeDefinition>();
        [SerializeField, Min(1)] private int choiceCount = 3;
        [SerializeField, Min(1)] private int selectionRetryCount = 12;

        private readonly Dictionary<string, int> stackCounts = new Dictionary<string, int>();
        private RunUpgradeDefinition[] currentChoices = Array.Empty<RunUpgradeDefinition>();
        private int observedTotalExperience;
        private int queuedLevelUps;

        public event Action<RunProgressionController> ProgressChanged;
        public event Action<RunProgressionController> ChoicesChanged;
        public event Action<RunProgressionController, RunUpgradeDefinition> UpgradeApplied;

        public int CurrentLevel { get; private set; }
        public int CurrentExperience { get; private set; }
        public int RequiredExperienceToNextLevel { get; private set; }
        public bool IsAwaitingSelection { get; private set; }
        public IReadOnlyList<RunUpgradeDefinition> CurrentChoices => currentChoices;
        public float ProgressToNextLevel => RequiredExperienceToNextLevel > 0
            ? (float)CurrentExperience / RequiredExperienceToNextLevel
            : 0f;

        private void Awake()
        {
            ResolveReferences();
            ResetProgressionState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToRunDirector();
            RefreshFromRunDirector();
        }

        private void OnDisable()
        {
            UnsubscribeFromRunDirector();
        }

        public int GetStackCount(RunUpgradeDefinition upgrade)
        {
            if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.Id))
            {
                return 0;
            }

            return stackCounts.TryGetValue(upgrade.Id, out var count) ? count : 0;
        }

        public void ApplyUpgrade(RunUpgradeDefinition selectedUpgrade)
        {
            if (!IsAwaitingSelection || selectedUpgrade == null || !ContainsChoice(selectedUpgrade))
            {
                return;
            }

            ApplyUpgradeEffect(selectedUpgrade);
            stackCounts[selectedUpgrade.Id] = GetStackCount(selectedUpgrade) + 1;

            queuedLevelUps = Mathf.Max(0, queuedLevelUps - 1);
            IsAwaitingSelection = false;
            currentChoices = Array.Empty<RunUpgradeDefinition>();

            if (runDirector != null)
            {
                runDirector.ResumeRun();
            }

            UpgradeApplied?.Invoke(this, selectedUpgrade);
            ChoicesChanged?.Invoke(this);
            ProgressChanged?.Invoke(this);

            TryOfferNextUpgradeChoice();
        }

        private void ResolveReferences()
        {
            if (runDirector == null)
            {
                runDirector = FindFirstObjectByType<RunDirector>();
            }

            if (player == null && runDirector != null)
            {
                player = runDirector.Player;
            }

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
            }
        }

        private void SubscribeToRunDirector()
        {
            if (runDirector == null)
            {
                return;
            }

            runDirector.RunStarted -= HandleRunStarted;
            runDirector.RunStarted += HandleRunStarted;
            runDirector.RunStatsChanged -= HandleRunStatsChanged;
            runDirector.RunStatsChanged += HandleRunStatsChanged;
        }

        private void UnsubscribeFromRunDirector()
        {
            if (runDirector == null)
            {
                return;
            }

            runDirector.RunStarted -= HandleRunStarted;
            runDirector.RunStatsChanged -= HandleRunStatsChanged;
        }

        private void HandleRunStarted(RunDirector director)
        {
            ResetProgressionState();
        }

        private void HandleRunStatsChanged(RunDirector director)
        {
            RefreshFromRunDirector();
        }

        private void RefreshFromRunDirector()
        {
            if (runDirector == null)
            {
                return;
            }

            ResolveReferences();

            var totalExperience = runDirector.ExperienceCollected;
            if (totalExperience <= observedTotalExperience)
            {
                return;
            }

            var delta = totalExperience - observedTotalExperience;
            observedTotalExperience = totalExperience;

            AddRuntimeExperience(delta);
        }

        private void ResetProgressionState()
        {
            CurrentLevel = Mathf.Max(1, startingLevel);
            CurrentExperience = 0;
            RequiredExperienceToNextLevel = ComputeRequiredExperience(CurrentLevel);
            observedTotalExperience = 0;
            queuedLevelUps = 0;
            IsAwaitingSelection = false;
            currentChoices = Array.Empty<RunUpgradeDefinition>();
            stackCounts.Clear();

            ChoicesChanged?.Invoke(this);
            ProgressChanged?.Invoke(this);
        }

        private void AddRuntimeExperience(int amount)
        {
            var remaining = Mathf.Max(0, amount);

            while (remaining > 0)
            {
                var missingForLevel = RequiredExperienceToNextLevel - CurrentExperience;
                var applied = Mathf.Min(missingForLevel, remaining);

                CurrentExperience += applied;
                remaining -= applied;

                if (CurrentExperience < RequiredExperienceToNextLevel)
                {
                    continue;
                }

                CurrentExperience -= RequiredExperienceToNextLevel;
                CurrentLevel++;
                RequiredExperienceToNextLevel = ComputeRequiredExperience(CurrentLevel);
                queuedLevelUps++;
            }

            ProgressChanged?.Invoke(this);
            TryOfferNextUpgradeChoice();
        }

        private void TryOfferNextUpgradeChoice()
        {
            if (queuedLevelUps <= 0 || IsAwaitingSelection)
            {
                return;
            }

            currentChoices = GenerateUpgradeChoices();
            if (currentChoices.Length == 0)
            {
                queuedLevelUps = 0;
                ChoicesChanged?.Invoke(this);
                ProgressChanged?.Invoke(this);
                return;
            }

            IsAwaitingSelection = true;

            if (runDirector != null)
            {
                runDirector.PauseRun();
            }

            ChoicesChanged?.Invoke(this);
            ProgressChanged?.Invoke(this);
        }

        private RunUpgradeDefinition[] GenerateUpgradeChoices()
        {
            var candidates = new List<RunUpgradeDefinition>();

            for (var i = 0; i < upgradeCatalog.Length; i++)
            {
                var upgrade = upgradeCatalog[i];
                if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.Id))
                {
                    continue;
                }

                if (GetStackCount(upgrade) >= upgrade.MaxStacks)
                {
                    continue;
                }

                candidates.Add(upgrade);
            }

            if (candidates.Count == 0)
            {
                return Array.Empty<RunUpgradeDefinition>();
            }

            var results = new List<RunUpgradeDefinition>();
            var selectedIds = new HashSet<string>();
            var attempts = 0;
            var maxAttempts = Mathf.Max(choiceCount, 1) * Mathf.Max(selectionRetryCount, 1);

            while (results.Count < choiceCount && attempts < maxAttempts)
            {
                attempts++;

                var pickedUpgrade = PickWeightedUpgrade(candidates, selectedIds);
                if (pickedUpgrade == null)
                {
                    break;
                }

                results.Add(pickedUpgrade);
                selectedIds.Add(pickedUpgrade.Id);
            }

            for (var i = 0; i < candidates.Count && results.Count < choiceCount; i++)
            {
                if (selectedIds.Add(candidates[i].Id))
                {
                    results.Add(candidates[i]);
                }
            }

            return results.ToArray();
        }

        private RunUpgradeDefinition PickWeightedUpgrade(List<RunUpgradeDefinition> candidates, HashSet<string> excludedIds)
        {
            var totalWeight = 0f;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || excludedIds.Contains(candidate.Id))
                {
                    continue;
                }

                totalWeight += Mathf.Max(0.01f, candidate.Weight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            var roll = UnityEngine.Random.value * totalWeight;

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || excludedIds.Contains(candidate.Id))
                {
                    continue;
                }

                roll -= Mathf.Max(0.01f, candidate.Weight);
                if (roll <= 0f)
                {
                    return candidate;
                }
            }

            return candidates[0];
        }

        private void ApplyUpgradeEffect(RunUpgradeDefinition upgrade)
        {
            ResolveReferences();

            switch (upgrade.EffectType)
            {
                case RunUpgradeEffectType.MoveSpeed:
                    player?.AddMoveSpeed(upgrade.EffectValue);
                    break;

                case RunUpgradeEffectType.MagnetRadius:
                    player?.AddMagnetRadius(upgrade.EffectValue);
                    break;

                case RunUpgradeEffectType.MaxHealth:
                    player?.AddMaxHealth(upgrade.EffectValue, true);
                    break;

                case RunUpgradeEffectType.AuraDamage:
                    player?.AddAuraDamage(upgrade.EffectValue);
                    break;

                case RunUpgradeEffectType.ShieldChance:
                    player?.AddShieldChance(upgrade.EffectValue);
                    break;

                case RunUpgradeEffectType.CoinMultiplier:
                    runDirector?.AddCoinMultiplier(upgrade.EffectValue);
                    break;
            }
        }

        private int ComputeRequiredExperience(int level)
        {
            var levelOffset = Mathf.Max(0, level - startingLevel);
            var required = baseExperienceToLevel * Mathf.Pow(experienceGrowthMultiplier, levelOffset);
            return Mathf.Max(1, Mathf.RoundToInt(required));
        }

        private bool ContainsChoice(RunUpgradeDefinition selectedUpgrade)
        {
            for (var i = 0; i < currentChoices.Length; i++)
            {
                if (currentChoices[i] == selectedUpgrade)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
