#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CoinRushSurvivor.Core;
using CoinRushSurvivor.Data;
using CoinRushSurvivor.Enemies;
using CoinRushSurvivor.Gameplay;
using CoinRushSurvivor.Pickups;
using CoinRushSurvivor.Player;
using CoinRushSurvivor.Progression;
using CoinRushSurvivor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CoinRushSurvivor.Editor
{
    [InitializeOnLoad]
    public static class VerticalSliceBuilder
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MetaScenePath = "Assets/_Project/Scenes/MetaProgression.unity";

        static VerticalSliceBuilder()
        {
            EditorApplication.delayCall += AutoBuildIfNeeded;
        }

        [MenuItem("Coin Rush Survivor/Build Vertical Slice")]
        public static void BuildVerticalSlice()
        {
            BuildAll(forceRebuildScenes: false);
        }

        [MenuItem("Coin Rush Survivor/Rebuild Vertical Slice")]
        public static void RebuildVerticalSlice()
        {
            BuildAll(forceRebuildScenes: true);
        }

        private static void AutoBuildIfNeeded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += AutoBuildIfNeeded;
                return;
            }

            if (IsBuildPresent())
            {
                return;
            }

            BuildAll(forceRebuildScenes: false);
        }

        private static bool IsBuildPresent()
        {
            return File.Exists(BootstrapScenePath) &&
                   File.Exists(MainMenuScenePath) &&
                   File.Exists(GameScenePath) &&
                   File.Exists(MetaScenePath);
        }

        internal static void BuildAll(bool forceRebuildScenes)
        {
            EnsureDirectories();
            EnsureLayers();

            var context = new VerticalSliceBuildContext
            {
                TitleFont = VerticalSliceEditorUtil.LoadFont("Assets/_Project/Resources/UI/Fonts/CoinRushTitle.ttf"),
                BodyFont = VerticalSliceEditorUtil.LoadFont("Assets/_Project/Resources/UI/Fonts/CoinRushBody.ttf"),
                UiSprite = VerticalSliceEditorUtil.GetDefaultUiSprite()
            };

            EnsureScriptableObjects(context);
            EnsurePrefabs(context);
            VerticalSliceSceneBuilder.BuildScenes(context, forceRebuildScenes);
            ConfigureBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");
            Directory.CreateDirectory("Assets/_Project/Prefabs/Player");
            Directory.CreateDirectory("Assets/_Project/Prefabs/Enemies");
            Directory.CreateDirectory("Assets/_Project/Prefabs/Pickups");
            Directory.CreateDirectory("Assets/_Project/Prefabs/UI");
            Directory.CreateDirectory("Assets/_Project/ScriptableObjects/RunUpgrades");
            Directory.CreateDirectory("Assets/_Project/ScriptableObjects/PermanentUpgrades");
            Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Cosmetics");
        }

        private static void EnsureLayers()
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null || tagManager.Length == 0)
            {
                return;
            }

            var serializedObject = new SerializedObject(tagManager[0]);
            var layersProperty = serializedObject.FindProperty("layers");
            if (layersProperty == null || layersProperty.arraySize < 11)
            {
                return;
            }

            EnsureLayer(layersProperty, 8, "Player");
            EnsureLayer(layersProperty, 9, "Enemy");
            EnsureLayer(layersProperty, 10, "Pickup");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureLayer(SerializedProperty layersProperty, int index, string layerName)
        {
            var layerProperty = layersProperty.GetArrayElementAtIndex(index);
            if (string.IsNullOrWhiteSpace(layerProperty.stringValue) || layerProperty.stringValue == layerName)
            {
                layerProperty.stringValue = layerName;
            }
        }

        private static void EnsureScriptableObjects(VerticalSliceBuildContext context)
        {
            context.RunUpgrades = new[]
            {
                CreateOrUpdateRunUpgrade(
                    "Assets/_Project/ScriptableObjects/RunUpgrades/RunUpgrade_MoveSpeed.asset",
                    "run_move_speed",
                    "Move Speed",
                    "Move faster and weave through enemy swarms with more control.",
                    RunUpgradeEffectType.MoveSpeed,
                    0.6f,
                    5,
                    1f),
                CreateOrUpdateRunUpgrade(
                    "Assets/_Project/ScriptableObjects/RunUpgrades/RunUpgrade_MagnetRadius.asset",
                    "run_magnet_radius",
                    "Magnet Radius",
                    "Pull pickups from farther away so coins and XP feel easier to collect.",
                    RunUpgradeEffectType.MagnetRadius,
                    0.45f,
                    5,
                    1f),
                CreateOrUpdateRunUpgrade(
                    "Assets/_Project/ScriptableObjects/RunUpgrades/RunUpgrade_MaxHealth.asset",
                    "run_max_health",
                    "Max Health",
                    "Increase your health pool and heal to full instantly.",
                    RunUpgradeEffectType.MaxHealth,
                    1f,
                    5,
                    0.75f),
                CreateOrUpdateRunUpgrade(
                    "Assets/_Project/ScriptableObjects/RunUpgrades/RunUpgrade_AuraDamage.asset",
                    "run_aura_damage",
                    "Aura Damage",
                    "Make the passive damage aura melt enemies faster.",
                    RunUpgradeEffectType.AuraDamage,
                    0.75f,
                    6,
                    1f),
                CreateOrUpdateRunUpgrade(
                    "Assets/_Project/ScriptableObjects/RunUpgrades/RunUpgrade_ShieldChance.asset",
                    "run_shield_chance",
                    "Shield Chance",
                    "Gain a chance to ignore incoming hits during the run.",
                    RunUpgradeEffectType.ShieldChance,
                    0.06f,
                    5,
                    0.65f),
                CreateOrUpdateRunUpgrade(
                    "Assets/_Project/ScriptableObjects/RunUpgrades/RunUpgrade_CoinMultiplier.asset",
                    "run_coin_multiplier",
                    "Coin Multiplier",
                    "Increase how many coins each pickup is worth during this run.",
                    RunUpgradeEffectType.CoinMultiplier,
                    0.2f,
                    5,
                    0.7f)
            };

            context.PermanentUpgrades = new[]
            {
                CreateOrUpdatePermanentUpgrade(
                    "Assets/_Project/ScriptableObjects/PermanentUpgrades/Permanent_StartingHealth.asset",
                    "perm_start_health",
                    "Starting Health",
                    "Start each run with more maximum health.",
                    PermanentUpgradeType.StartingHealth,
                    40,
                    1.65f,
                    5,
                    1f),
                CreateOrUpdatePermanentUpgrade(
                    "Assets/_Project/ScriptableObjects/PermanentUpgrades/Permanent_CoinGain.asset",
                    "perm_coin_gain",
                    "Coin Gain",
                    "Boost coin payouts across every run.",
                    PermanentUpgradeType.CoinGain,
                    60,
                    1.7f,
                    5,
                    0.15f),
                CreateOrUpdatePermanentUpgrade(
                    "Assets/_Project/ScriptableObjects/PermanentUpgrades/Permanent_PickupRadius.asset",
                    "perm_pickup_radius",
                    "Pickup Radius",
                    "Start runs with a stronger pickup magnet.",
                    PermanentUpgradeType.PickupRadius,
                    50,
                    1.65f,
                    5,
                    0.25f),
                CreateOrUpdatePermanentUpgrade(
                    "Assets/_Project/ScriptableObjects/PermanentUpgrades/Permanent_ReviveHealth.asset",
                    "perm_revive_health",
                    "Revive Health",
                    "Restore more health when a revive is used.",
                    PermanentUpgradeType.ReviveHealthBonus,
                    80,
                    1.75f,
                    3,
                    0.1f)
            };

            context.Cosmetics = new[]
            {
                CreateOrUpdateCosmetic(
                    "Assets/_Project/ScriptableObjects/Cosmetics/Cosmetic_Classic.asset",
                    "cosmetic_classic",
                    "Classic Rush",
                    0,
                    new Color(0.96f, 0.73f, 0.24f),
                    true),
                CreateOrUpdateCosmetic(
                    "Assets/_Project/ScriptableObjects/Cosmetics/Cosmetic_Mint.asset",
                    "cosmetic_mint",
                    "Mint Dash",
                    150,
                    new Color(0.39f, 0.92f, 0.76f),
                    false),
                CreateOrUpdateCosmetic(
                    "Assets/_Project/ScriptableObjects/Cosmetics/Cosmetic_Coral.asset",
                    "cosmetic_coral",
                    "Coral Burst",
                    250,
                    new Color(0.96f, 0.48f, 0.40f),
                    false)
            };
        }

        private static RunUpgradeDefinition CreateOrUpdateRunUpgrade(
            string path,
            string id,
            string displayName,
            string description,
            RunUpgradeEffectType effectType,
            float effectValue,
            int maxStacks,
            float weight)
        {
            var asset = CreateOrLoadAsset<RunUpgradeDefinition>(path);
            VerticalSliceEditorUtil.SetPrivateField(asset, "id", id);
            VerticalSliceEditorUtil.SetPrivateField(asset, "displayName", displayName);
            VerticalSliceEditorUtil.SetPrivateField(asset, "description", description);
            VerticalSliceEditorUtil.SetPrivateField(asset, "effectType", effectType);
            VerticalSliceEditorUtil.SetPrivateField(asset, "effectValue", effectValue);
            VerticalSliceEditorUtil.SetPrivateField(asset, "maxStacks", maxStacks);
            VerticalSliceEditorUtil.SetPrivateField(asset, "weight", weight);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static PermanentUpgradeDefinition CreateOrUpdatePermanentUpgrade(
            string path,
            string id,
            string displayName,
            string description,
            PermanentUpgradeType upgradeType,
            int baseCost,
            float costGrowth,
            int maxLevel,
            float effectPerLevel)
        {
            var asset = CreateOrLoadAsset<PermanentUpgradeDefinition>(path);
            VerticalSliceEditorUtil.SetPrivateField(asset, "id", id);
            VerticalSliceEditorUtil.SetPrivateField(asset, "displayName", displayName);
            VerticalSliceEditorUtil.SetPrivateField(asset, "description", description);
            VerticalSliceEditorUtil.SetPrivateField(asset, "upgradeType", upgradeType);
            VerticalSliceEditorUtil.SetPrivateField(asset, "baseCost", baseCost);
            VerticalSliceEditorUtil.SetPrivateField(asset, "costGrowth", costGrowth);
            VerticalSliceEditorUtil.SetPrivateField(asset, "maxLevel", maxLevel);
            VerticalSliceEditorUtil.SetPrivateField(asset, "effectPerLevel", effectPerLevel);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static CosmeticDefinition CreateOrUpdateCosmetic(
            string path,
            string id,
            string displayName,
            int unlockCost,
            Color previewColor,
            bool defaultUnlocked)
        {
            var asset = CreateOrLoadAsset<CosmeticDefinition>(path);
            VerticalSliceEditorUtil.SetPrivateField(asset, "id", id);
            VerticalSliceEditorUtil.SetPrivateField(asset, "displayName", displayName);
            VerticalSliceEditorUtil.SetPrivateField(asset, "unlockCost", unlockCost);
            VerticalSliceEditorUtil.SetPrivateField(asset, "previewColor", previewColor);
            VerticalSliceEditorUtil.SetPrivateField(asset, "defaultUnlocked", defaultUnlocked);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsurePrefabs(VerticalSliceBuildContext context)
        {
            context.PlayerPrefab = CreatePlayerPrefab();
            context.BasicEnemyPrefab = CreateEnemyPrefab();
            context.CoinPickupPrefab = CreatePickupPrefab(
                "Assets/_Project/Prefabs/Pickups/CoinPickup.prefab",
                "CoinPickup",
                PickupType.Coin,
                "CoinPickup",
                new Color(0.96f, 0.80f, 0.16f),
                Quaternion.identity);
            context.ExperiencePickupPrefab = CreatePickupPrefab(
                "Assets/_Project/Prefabs/Pickups/ExperiencePickup.prefab",
                "ExperiencePickup",
                PickupType.Experience,
                "ExperiencePickup",
                new Color(0.30f, 0.88f, 0.95f),
                Quaternion.Euler(0f, 0f, 45f));
        }

        private static GameObject CreatePlayerPrefab()
        {
            const string prefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
            var prefabRoot = new GameObject("Player");
            prefabRoot.layer = LayerMask.NameToLayer("Player");
            prefabRoot.transform.localScale = Vector3.one * 0.8f;

            var spriteRenderer = prefabRoot.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = VerticalSliceEditorUtil.GetDefaultUiSprite();
            spriteRenderer.color = new Color(0.16f, 0.47f, 0.96f);
            spriteRenderer.sortingOrder = 2;

            var body = prefabRoot.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider2D = prefabRoot.AddComponent<CircleCollider2D>();
            collider2D.radius = 0.55f;

            var health = prefabRoot.AddComponent<Health>();
            VerticalSliceEditorUtil.SetPrivateField(health, "maxHealth", 5f);

            var controller = prefabRoot.AddComponent<PlayerController>();
            VerticalSliceEditorUtil.SetPrivateField(controller, "body", body);
            VerticalSliceEditorUtil.SetPrivateField(controller, "health", health);
            VerticalSliceEditorUtil.SetPrivateField(controller, "auraOrigin", prefabRoot.transform);
            VerticalSliceEditorUtil.SetPrivateField(controller, "moveSpeed", 5f);
            VerticalSliceEditorUtil.SetPrivateField(controller, "magnetRadius", 2.2f);
            VerticalSliceEditorUtil.SetPrivateField(controller, "auraRadius", 1.35f);
            VerticalSliceEditorUtil.SetPrivateField(controller, "auraDamage", 1f);
            VerticalSliceEditorUtil.SetPrivateField(controller, "enemyLayers", LayerMask.GetMask("Enemy"));

            return SavePrefab(prefabRoot, prefabPath);
        }

        private static GameObject CreateEnemyPrefab()
        {
            const string prefabPath = "Assets/_Project/Prefabs/Enemies/BasicEnemy.prefab";
            var prefabRoot = new GameObject("BasicEnemy");
            prefabRoot.layer = LayerMask.NameToLayer("Enemy");
            prefabRoot.transform.localScale = Vector3.one * 0.65f;

            var spriteRenderer = prefabRoot.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = VerticalSliceEditorUtil.GetDefaultUiSprite();
            spriteRenderer.color = new Color(0.93f, 0.36f, 0.24f);
            spriteRenderer.sortingOrder = 1;

            var body = prefabRoot.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider2D = prefabRoot.AddComponent<CircleCollider2D>();
            collider2D.radius = 0.55f;

            var health = prefabRoot.AddComponent<Health>();
            VerticalSliceEditorUtil.SetPrivateField(health, "maxHealth", 3f);

            var controller = prefabRoot.AddComponent<EnemyController>();
            VerticalSliceEditorUtil.SetPrivateField(controller, "body", body);
            VerticalSliceEditorUtil.SetPrivateField(controller, "health", health);
            VerticalSliceEditorUtil.SetPrivateField(controller, "poolKey", "BasicEnemy");
            VerticalSliceEditorUtil.SetPrivateField(controller, "baseMoveSpeed", 2.25f);
            VerticalSliceEditorUtil.SetPrivateField(controller, "baseMaxHealth", 3f);
            VerticalSliceEditorUtil.SetPrivateField(controller, "baseContactDamage", 1f);
            VerticalSliceEditorUtil.SetPrivateField(controller, "scoreValue", 10);
            VerticalSliceEditorUtil.SetPrivateField(controller, "coinDropCount", 1);
            VerticalSliceEditorUtil.SetPrivateField(controller, "experienceDropCount", 1);
            VerticalSliceEditorUtil.SetPrivateField(controller, "coinPickupPoolKey", "CoinPickup");
            VerticalSliceEditorUtil.SetPrivateField(controller, "experiencePickupPoolKey", "ExperiencePickup");

            return SavePrefab(prefabRoot, prefabPath);
        }

        private static GameObject CreatePickupPrefab(
            string prefabPath,
            string prefabName,
            PickupType pickupType,
            string poolKey,
            Color color,
            Quaternion visualRotation)
        {
            var prefabRoot = new GameObject(prefabName);
            prefabRoot.layer = LayerMask.NameToLayer("Pickup");

            var visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(prefabRoot.transform, false);
            visualRoot.transform.localRotation = visualRotation;
            visualRoot.transform.localScale = Vector3.one * 0.35f;

            var spriteRenderer = visualRoot.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = VerticalSliceEditorUtil.GetDefaultUiSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 3;

            var body = prefabRoot.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.bodyType = RigidbodyType2D.Kinematic;

            var collider2D = prefabRoot.AddComponent<CircleCollider2D>();
            collider2D.isTrigger = true;
            collider2D.radius = 0.45f;

            var pickup = prefabRoot.AddComponent<PickupItem>();
            VerticalSliceEditorUtil.SetPrivateField(pickup, "body", body);
            VerticalSliceEditorUtil.SetPrivateField(pickup, "triggerCollider", collider2D);
            VerticalSliceEditorUtil.SetPrivateField(pickup, "visualRoot", visualRoot.transform);
            VerticalSliceEditorUtil.SetPrivateField(pickup, "pickupType", pickupType);
            VerticalSliceEditorUtil.SetPrivateField(pickup, "amount", 1);
            VerticalSliceEditorUtil.SetPrivateField(pickup, "poolKey", poolKey);

            return SavePrefab(prefabRoot, prefabPath);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
                new EditorBuildSettingsScene(MetaScenePath, true)
            };
        }
    }

    internal sealed class VerticalSliceBuildContext
    {
        public RunUpgradeDefinition[] RunUpgrades = Array.Empty<RunUpgradeDefinition>();
        public PermanentUpgradeDefinition[] PermanentUpgrades = Array.Empty<PermanentUpgradeDefinition>();
        public CosmeticDefinition[] Cosmetics = Array.Empty<CosmeticDefinition>();
        public GameObject PlayerPrefab;
        public GameObject BasicEnemyPrefab;
        public GameObject CoinPickupPrefab;
        public GameObject ExperiencePickupPrefab;
        public Font TitleFont;
        public Font BodyFont;
        public Sprite UiSprite;
    }

    internal static class VerticalSliceEditorUtil
    {
        private static Font cachedFallbackFont;
        private static Sprite cachedUiSprite;

        public static Font LoadFont(string assetPath)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
            return font != null ? font : GetFallbackFont();
        }

        public static Font GetFallbackFont()
        {
            if (cachedFallbackFont == null)
            {
                cachedFallbackFont = AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
            }

            return cachedFallbackFont;
        }

        public static Sprite GetDefaultUiSprite()
        {
            if (cachedUiSprite == null)
            {
                cachedUiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }

            return cachedUiSprite;
        }

        public static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                throw new MissingFieldException(type.FullName, fieldName);
            }

            field.SetValue(target, value);
            if (target is UnityEngine.Object unityObject)
            {
                EditorUtility.SetDirty(unityObject);
            }
        }

        public static Array CreatePrivateNestedArray(Type ownerType, string nestedTypeName, int size)
        {
            var nestedType = ownerType.GetNestedType(nestedTypeName, BindingFlags.NonPublic);
            if (nestedType == null)
            {
                throw new MissingMemberException(ownerType.FullName, nestedTypeName);
            }

            return Array.CreateInstance(nestedType, size);
        }

        public static object CreatePrivateNestedInstance(Type ownerType, string nestedTypeName)
        {
            var nestedType = ownerType.GetNestedType(nestedTypeName, BindingFlags.NonPublic);
            if (nestedType == null)
            {
                throw new MissingMemberException(ownerType.FullName, nestedTypeName);
            }

            return Activator.CreateInstance(nestedType);
        }

        public static Color PanelColor => new Color(1f, 0.97f, 0.91f, 0.95f);
        public static Color AccentColor => new Color(0.96f, 0.73f, 0.17f, 1f);
        public static Color BackgroundColor => new Color(1f, 0.96f, 0.89f, 1f);
        public static Color BodyTextColor => new Color(0.12f, 0.17f, 0.29f, 1f);
        public static Color SecondaryTextColor => new Color(0.24f, 0.31f, 0.39f, 1f);
        public static Color GoodColor => new Color(0.30f, 0.74f, 0.48f, 1f);
        public static Color DangerColor => new Color(0.86f, 0.32f, 0.27f, 1f);
    }
}
#endif
