using System;
using System.Collections.Generic;
using CoinRushSurvivor.Core;
using CoinRushSurvivor.Gameplay;
using UnityEngine;

namespace CoinRushSurvivor.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Serializable]
        private struct SpawnEntry
        {
            public string poolKey;
            public float weight;
            public float unlockAtSeconds;
        }

        [Header("References")]
        [SerializeField] private ObjectPool objectPool;
        [SerializeField] private RunDirector runDirector;
        [SerializeField] private Camera gameplayCamera;

        [Header("Spawn Catalog")]
        [SerializeField] private SpawnEntry[] spawnEntries;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float initialSpawnDelay = 0.75f;
        [SerializeField, Min(0.1f)] private float baseSpawnInterval = 1.25f;
        [SerializeField, Min(0.1f)] private float minimumSpawnInterval = 0.35f;
        [SerializeField, Min(0f)] private float spawnIntervalDifficultyFactor = 0.18f;

        [Header("Positioning")]
        [SerializeField, Min(0f)] private float spawnDistanceFromView = 0.75f;
        [SerializeField, Min(0f)] private float edgePadding = 0.6f;
        [SerializeField] private LayerMask spawnBlockers;
        [SerializeField, Min(0f)] private float spawnCheckRadius = 0.2f;

        [Header("Limits")]
        [SerializeField, Min(1)] private int maxAliveEnemies = 40;

        private float spawnTimer;

        private void Awake()
        {
            if (objectPool == null)
            {
                ServiceLocator.TryGet(out objectPool);
            }

            if (runDirector == null)
            {
                ServiceLocator.TryGet(out runDirector);
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            spawnTimer = initialSpawnDelay;
        }

        private void Update()
        {
            if (!CanSpawn())
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            SpawnEnemy();
            spawnTimer = GetSpawnInterval();
        }

        private bool CanSpawn()
        {
            if (objectPool == null || runDirector == null || gameplayCamera == null)
            {
                return false;
            }

            if (!runDirector.IsRunActive)
            {
                return false;
            }

            return GetActiveEnemyCount() < maxAliveEnemies;
        }

        private void SpawnEnemy()
        {
            if (!TryChooseSpawnEntry(out var spawnEntry))
            {
                return;
            }

            if (!TryGetSpawnPosition(out var spawnPosition))
            {
                return;
            }

            var instance = objectPool.Spawn(spawnEntry.poolKey, spawnPosition, Quaternion.identity);
            if (instance == null)
            {
                return;
            }

            var enemy = instance.GetComponent<EnemyController>();
            if (enemy == null)
            {
                Debug.LogWarning($"Spawned pool '{spawnEntry.poolKey}' but no EnemyController was found.");
                return;
            }

            enemy.Configure(
                runDirector.PlayerTransform,
                objectPool,
                spawnEntry.poolKey,
                runDirector.DifficultyScalar);
        }

        private float GetSpawnInterval()
        {
            var difficulty = runDirector != null ? runDirector.DifficultyScalar : 1f;
            var adjustedInterval = baseSpawnInterval - (difficulty - 1f) * spawnIntervalDifficultyFactor;
            return Mathf.Max(minimumSpawnInterval, adjustedInterval);
        }

        private bool TryChooseSpawnEntry(out SpawnEntry chosenEntry)
        {
            chosenEntry = default;

            if (spawnEntries == null || spawnEntries.Length == 0)
            {
                return false;
            }

            var elapsedTime = runDirector != null ? runDirector.ElapsedTime : 0f;
            var totalWeight = 0f;

            for (var i = 0; i < spawnEntries.Length; i++)
            {
                var entry = spawnEntries[i];
                if (string.IsNullOrWhiteSpace(entry.poolKey) || entry.weight <= 0f || elapsedTime < entry.unlockAtSeconds)
                {
                    continue;
                }

                totalWeight += entry.weight;
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            var roll = UnityEngine.Random.value * totalWeight;

            for (var i = 0; i < spawnEntries.Length; i++)
            {
                var entry = spawnEntries[i];
                if (string.IsNullOrWhiteSpace(entry.poolKey) || entry.weight <= 0f || elapsedTime < entry.unlockAtSeconds)
                {
                    continue;
                }

                roll -= entry.weight;
                if (roll <= 0f)
                {
                    chosenEntry = entry;
                    return true;
                }
            }

            chosenEntry = spawnEntries[0];
            return true;
        }

        private bool TryGetSpawnPosition(out Vector2 spawnPosition)
        {
            spawnPosition = Vector2.zero;

            if (!gameplayCamera.orthographic)
            {
                return false;
            }

            var cameraPosition = gameplayCamera.transform.position;
            var halfHeight = gameplayCamera.orthographicSize;
            var halfWidth = halfHeight * gameplayCamera.aspect;

            for (var attempt = 0; attempt < 8; attempt++)
            {
                var side = UnityEngine.Random.Range(0, 4);
                var x = 0f;
                var y = 0f;

                switch (side)
                {
                    case 0:
                        x = UnityEngine.Random.Range(cameraPosition.x - halfWidth + edgePadding, cameraPosition.x + halfWidth - edgePadding);
                        y = cameraPosition.y + halfHeight + spawnDistanceFromView;
                        break;

                    case 1:
                        x = UnityEngine.Random.Range(cameraPosition.x - halfWidth + edgePadding, cameraPosition.x + halfWidth - edgePadding);
                        y = cameraPosition.y - halfHeight - spawnDistanceFromView;
                        break;

                    case 2:
                        x = cameraPosition.x - halfWidth - spawnDistanceFromView;
                        y = UnityEngine.Random.Range(cameraPosition.y - halfHeight + edgePadding, cameraPosition.y + halfHeight - edgePadding);
                        break;

                    default:
                        x = cameraPosition.x + halfWidth + spawnDistanceFromView;
                        y = UnityEngine.Random.Range(cameraPosition.y - halfHeight + edgePadding, cameraPosition.y + halfHeight - edgePadding);
                        break;
                }

                spawnPosition = new Vector2(x, y);

                if (spawnCheckRadius <= 0f)
                {
                    return true;
                }

                if (!Physics2D.OverlapCircle(spawnPosition, spawnCheckRadius, spawnBlockers))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (gameplayCamera == null || !gameplayCamera.orthographic)
            {
                return;
            }

            var cameraPosition = gameplayCamera.transform.position;
            var halfHeight = gameplayCamera.orthographicSize;
            var halfWidth = halfHeight * gameplayCamera.aspect;

            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
            Gizmos.DrawWireCube(
                new Vector3(cameraPosition.x, cameraPosition.y, 0f),
                new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));
        }

        private int GetActiveEnemyCount()
        {
            if (spawnEntries == null || spawnEntries.Length == 0 || objectPool == null)
            {
                return 0;
            }

            var count = 0;
            var countedKeys = new HashSet<string>();

            for (var i = 0; i < spawnEntries.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(spawnEntries[i].poolKey) || !countedKeys.Add(spawnEntries[i].poolKey))
                {
                    continue;
                }

                count += objectPool.ActiveCount(spawnEntries[i].poolKey);
            }

            return count;
        }
    }
}
