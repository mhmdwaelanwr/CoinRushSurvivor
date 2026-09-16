using System;
using System.Collections.Generic;
using CoinRushSurvivor.Core;
using UnityEngine;

namespace CoinRushSurvivor.Gameplay
{
    public interface IPooledObject
    {
        void OnSpawned();
        void OnDespawned();
    }

    [DisallowMultipleComponent]
    public sealed class ObjectPool : MonoBehaviour
    {
        [Serializable]
        private sealed class PoolEntry
        {
            public string key = "PoolKey";
            public GameObject prefab;
            public int initialSize = 8;
            public bool canExpand = true;
            public Transform parentOverride;
        }

        [SerializeField] private PoolEntry[] pools = Array.Empty<PoolEntry>();
        [SerializeField] private bool registerAsGlobalService = true;

        private readonly Dictionary<string, Queue<GameObject>> availableByKey = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, PoolEntry> configByKey = new Dictionary<string, PoolEntry>();
        private readonly Dictionary<string, Transform> rootByKey = new Dictionary<string, Transform>();
        private readonly Dictionary<GameObject, string> keyByInstance = new Dictionary<GameObject, string>();
        private readonly HashSet<GameObject> activeInstances = new HashSet<GameObject>();

        private void Awake()
        {
            BuildPools();

            if (registerAsGlobalService)
            {
                ServiceLocator.Register(this);
            }
        }

        private void OnDestroy()
        {
            if (registerAsGlobalService &&
                ServiceLocator.TryGet<ObjectPool>(out var registeredPool) &&
                ReferenceEquals(registeredPool, this))
            {
                ServiceLocator.Unregister(this);
            }
        }

        public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!configByKey.TryGetValue(key, out var config))
            {
                Debug.LogError($"ObjectPool could not find a pool with key '{key}'.");
                return null;
            }

            var instance = GetOrCreateInstance(key, config);
            if (instance == null)
            {
                return null;
            }

            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            activeInstances.Add(instance);
            NotifySpawned(instance);
            return instance;
        }

        public T Spawn<T>(string key, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var instance = Spawn(key, position, rotation, parent);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!keyByInstance.TryGetValue(instance, out var key))
            {
                Debug.LogWarning($"ObjectPool received an unknown instance '{instance.name}' to despawn.");
                return;
            }

            Despawn(key, instance);
        }

        public void Despawn(string key, GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!availableByKey.ContainsKey(key))
            {
                Debug.LogWarning($"ObjectPool could not despawn '{instance.name}' because pool '{key}' is missing.");
                return;
            }

            if (!activeInstances.Contains(instance))
            {
                return;
            }

            NotifyDespawned(instance);

            activeInstances.Remove(instance);
            instance.SetActive(false);
            instance.transform.SetParent(rootByKey[key], false);
            availableByKey[key].Enqueue(instance);
        }

        public int ActiveCount(string key)
        {
            var count = 0;

            foreach (var instance in activeInstances)
            {
                if (instance != null &&
                    keyByInstance.TryGetValue(instance, out var instanceKey) &&
                    instanceKey == key)
                {
                    count++;
                }
            }

            return count;
        }

        public int TotalActiveCount()
        {
            return activeInstances.Count;
        }

        private void BuildPools()
        {
            foreach (var pool in pools)
            {
                if (pool == null || pool.prefab == null || string.IsNullOrWhiteSpace(pool.key))
                {
                    continue;
                }

                if (configByKey.ContainsKey(pool.key))
                {
                    Debug.LogWarning($"ObjectPool has duplicate key '{pool.key}'. The later entry is ignored.");
                    continue;
                }

                configByKey[pool.key] = pool;
                availableByKey[pool.key] = new Queue<GameObject>();

                var root = pool.parentOverride != null
                    ? pool.parentOverride
                    : CreatePoolRoot(pool.key).transform;

                rootByKey[pool.key] = root;

                var startingSize = Mathf.Max(0, pool.initialSize);
                for (var i = 0; i < startingSize; i++)
                {
                    var instance = CreateInstance(pool.key, pool.prefab, root);
                    availableByKey[pool.key].Enqueue(instance);
                }
            }
        }

        private GameObject GetOrCreateInstance(string key, PoolEntry config)
        {
            while (availableByKey[key].Count > 0)
            {
                var instance = availableByKey[key].Dequeue();
                if (instance != null)
                {
                    return instance;
                }
            }

            if (!config.canExpand)
            {
                return null;
            }

            return CreateInstance(key, config.prefab, rootByKey[key]);
        }

        private GameObject CreateInstance(string key, GameObject prefab, Transform parent)
        {
            var instance = Instantiate(prefab, parent);
            instance.name = $"{prefab.name}_{keyByInstance.Count}";
            instance.SetActive(false);
            keyByInstance[instance] = key;
            return instance;
        }

        private GameObject CreatePoolRoot(string key)
        {
            var root = new GameObject($"{key}_Pool");
            root.transform.SetParent(transform, false);
            return root;
        }

        private static void NotifySpawned(GameObject instance)
        {
            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPooledObject pooledObject)
                {
                    pooledObject.OnSpawned();
                }
            }
        }

        private static void NotifyDespawned(GameObject instance)
        {
            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPooledObject pooledObject)
                {
                    pooledObject.OnDespawned();
                }
            }
        }
    }
}
