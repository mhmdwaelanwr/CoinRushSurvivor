using System;
using System.IO;
using CoinRushSurvivor.Core;
using UnityEngine;

namespace CoinRushSurvivor.Data
{
    [DisallowMultipleComponent]
    public sealed class SaveService : MonoBehaviour
    {
        [SerializeField] private string saveDirectoryName = "CoinRushSurvivor";
        [SerializeField] private string profileFileName = "player-profile.json";
        [SerializeField] private bool prettyPrintJson;

        private static SaveService instance;

        public string SaveFilePath
        {
            get
            {
                var directoryPath = Path.Combine(Application.persistentDataPath, saveDirectoryName);
                return Path.Combine(directoryPath, profileFileName);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            if (ServiceLocator.TryGet<SaveService>(out var registeredService) &&
                ReferenceEquals(registeredService, this))
            {
                ServiceLocator.Unregister(this);
            }

            instance = null;
        }

        public PlayerProfileData LoadOrCreateProfile(int saveVersion)
        {
            var profile = LoadProfile(saveVersion);
            if (profile != null)
            {
                return profile;
            }

            profile = PlayerProfileData.CreateDefault(saveVersion);
            SaveProfile(profile);
            return profile;
        }

        public PlayerProfileData LoadProfile(int saveVersion)
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    return null;
                }

                var json = File.ReadAllText(SaveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                var profile = JsonUtility.FromJson<PlayerProfileData>(json);
                if (profile == null)
                {
                    return null;
                }

                profile.EnsureInitialized(saveVersion);
                return profile;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveService failed to load profile data. A default profile will be used. {exception.Message}");
                return null;
            }
        }

        public bool SaveProfile(PlayerProfileData profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("SaveService was asked to save a null profile.");
                return false;
            }

            try
            {
                profile.EnsureInitialized(profile.saveVersion);

                var directoryPath = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrWhiteSpace(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var json = JsonUtility.ToJson(profile, prettyPrintJson);
                File.WriteAllText(SaveFilePath, json);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveService failed to save profile data. {exception.Message}");
                return false;
            }
        }
    }
}
