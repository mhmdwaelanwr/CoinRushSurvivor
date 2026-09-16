#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CoinRushSurvivor.Core;
using CoinRushSurvivor.Data;
using CoinRushSurvivor.Enemies;
using CoinRushSurvivor.Gameplay;
using CoinRushSurvivor.Player;
using CoinRushSurvivor.Progression;
using CoinRushSurvivor.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CoinRushSurvivor.Editor
{
    internal static class VerticalSliceSceneBuilder
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MetaScenePath = "Assets/_Project/Scenes/MetaProgression.unity";

        internal static void BuildScenes(VerticalSliceBuildContext context, bool forceRebuildScenes)
        {
            BuildBootstrapScene(context, forceRebuildScenes);
            BuildMainMenuScene(context, forceRebuildScenes);
            BuildGameScene(context, forceRebuildScenes);
            BuildMetaProgressionScene(context, forceRebuildScenes);
        }

        private static void BuildBootstrapScene(VerticalSliceBuildContext context, bool force)
        {
            if (!force && File.Exists(BootstrapScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera("Main Camera", 5f, VerticalSliceEditorUtil.BodyTextColor);

            var bootstrapRoot = new GameObject("BootstrapRoot");
            bootstrapRoot.AddComponent<GameBootstrap>();
            bootstrapRoot.AddComponent<SaveService>();
            var profileService = bootstrapRoot.AddComponent<PlayerProfileService>();
            bootstrapRoot.AddComponent<MenuNavigationState>();
            bootstrapRoot.AddComponent<AudioService>();

            VerticalSliceEditorUtil.SetPrivateField(profileService, "permanentUpgrades", context.PermanentUpgrades);
            VerticalSliceEditorUtil.SetPrivateField(profileService, "cosmetics", context.Cosmetics);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void BuildMainMenuScene(VerticalSliceBuildContext context, bool force)
        {
            if (!force && File.Exists(MainMenuScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera("Main Camera", 5f, VerticalSliceEditorUtil.BackgroundColor);
            CreateEventSystem();

            var theme = new ThemeCollector();
            var canvasRoot = CreateCanvasRoot("MainMenuCanvas");
            var background = CreateStretchImage("Background", canvasRoot.transform, VerticalSliceEditorUtil.BackgroundColor);
            theme.PanelGraphics.Add(background);

            var titleText = CreateText(
                canvasRoot.transform,
                "TitleText",
                "COIN RUSH\nSURVIVOR",
                context.TitleFont,
                92,
                new Vector2(0f, 700f),
                new Vector2(900f, 240f),
                TextAnchor.MiddleCenter,
                VerticalSliceEditorUtil.BodyTextColor);
            theme.TitleTexts.Add(titleText);

            var subtitleText = CreateText(
                canvasRoot.transform,
                "SubtitleText",
                "Short portrait runs. Quick upgrades. Clean progression.",
                context.BodyFont,
                38,
                new Vector2(0f, 520f),
                new Vector2(980f, 80f),
                TextAnchor.MiddleCenter,
                VerticalSliceEditorUtil.SecondaryTextColor);
            theme.BodyTexts.Add(subtitleText);

            var statsPanel = CreatePanel("StatsPanel", canvasRoot.transform, new Vector2(820f, 260f), new Vector2(0f, 310f));
            theme.PanelGraphics.Add(statsPanel.Image);

            var softCurrencyLabel = CreateText(statsPanel.Content, "SoftCurrencyLabel", "Coins", context.BodyFont, 34, new Vector2(-260f, 65f), new Vector2(180f, 52f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var softCurrencyText = CreateText(statsPanel.Content, "SoftCurrencyText", "0", context.BodyFont, 44, new Vector2(170f, 65f), new Vector2(260f, 60f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.BodyTextColor);
            var selectedCosmeticLabel = CreateText(statsPanel.Content, "SelectedCosmeticLabel", "Selected Cosmetic", context.BodyFont, 34, new Vector2(-260f, 0f), new Vector2(300f, 52f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var selectedCosmeticText = CreateText(statsPanel.Content, "SelectedCosmeticText", "Classic Rush", context.BodyFont, 40, new Vector2(150f, 0f), new Vector2(320f, 60f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.BodyTextColor);
            var sessionsLabel = CreateText(statsPanel.Content, "SessionsLabel", "Sessions Played", context.BodyFont, 34, new Vector2(-260f, -65f), new Vector2(300f, 52f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var sessionsText = CreateText(statsPanel.Content, "SessionsText", "0", context.BodyFont, 40, new Vector2(170f, -65f), new Vector2(260f, 60f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.AddRange(new[] { softCurrencyLabel, softCurrencyText, selectedCosmeticLabel, selectedCosmeticText, sessionsLabel, sessionsText });

            var playButton = CreateButton(canvasRoot.transform, "PlayButton", "Play", context, new Vector2(0f, 80f), new Vector2(680f, 130f));
            var upgradesButton = CreateButton(canvasRoot.transform, "UpgradesButton", "Upgrades", context, new Vector2(0f, -90f), new Vector2(680f, 130f));
            var cosmeticsButton = CreateButton(canvasRoot.transform, "CosmeticsButton", "Cosmetics", context, new Vector2(0f, -260f), new Vector2(680f, 130f));
            var settingsButton = CreateButton(canvasRoot.transform, "SettingsButton", "Settings", context, new Vector2(0f, -430f), new Vector2(680f, 130f));
            RegisterButton(theme, playButton);
            RegisterButton(theme, upgradesButton);
            RegisterButton(theme, cosmeticsButton);
            RegisterButton(theme, settingsButton);

            var settingsOverlay = CreateOverlayPanel(canvasRoot.transform, "SettingsOverlay", new Vector2(860f, 980f), "Settings", context, theme);
            settingsOverlay.Root.SetActive(false);

            var musicToggle = CreateToggleRow(settingsOverlay.Window, "MusicToggle", "Music", context, new Vector2(0f, 240f), new Vector2(660f, 80f), true, theme);
            var sfxToggle = CreateToggleRow(settingsOverlay.Window, "SfxToggle", "SFX", context, new Vector2(0f, 130f), new Vector2(660f, 80f), true, theme);
            var musicSlider = CreateSliderRow(settingsOverlay.Window, "MusicVolumeRow", "Music Volume", context, new Vector2(0f, -30f), new Vector2(700f, 120f), theme);
            var sfxSlider = CreateSliderRow(settingsOverlay.Window, "SfxVolumeRow", "SFX Volume", context, new Vector2(0f, -220f), new Vector2(700f, 120f), theme);
            var closeButton = CreateButton(settingsOverlay.Window, "CloseSettingsButton", "Close", context, new Vector2(0f, -390f), new Vector2(420f, 110f));
            RegisterButton(theme, closeButton);

            var mainMenuPresenter = canvasRoot.AddComponent<MainMenuPresenter>();
            var settingsPresenter = canvasRoot.AddComponent<SettingsPanelPresenter>();

            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "playButton", playButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "upgradesButton", upgradesButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "cosmeticsButton", cosmeticsButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "settingsButton", settingsButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "softCurrencyText", softCurrencyText);
            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "selectedCosmeticText", selectedCosmeticText);
            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "sessionsText", sessionsText);
            VerticalSliceEditorUtil.SetPrivateField(mainMenuPresenter, "settingsPanelPresenter", settingsPresenter);

            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "panelRoot", settingsOverlay.Root);
            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "closeButton", closeButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "musicToggle", musicToggle.Toggle);
            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "sfxToggle", sfxToggle.Toggle);
            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "musicVolumeSlider", musicSlider.Slider);
            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "sfxVolumeSlider", sfxSlider.Slider);
            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "musicValueText", musicSlider.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(settingsPresenter, "sfxValueText", sfxSlider.ValueText);

            ApplyTheme(canvasRoot, theme);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void BuildMetaProgressionScene(VerticalSliceBuildContext context, bool force)
        {
            if (!force && File.Exists(MetaScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera("Main Camera", 5f, VerticalSliceEditorUtil.BackgroundColor);
            CreateEventSystem();

            var theme = new ThemeCollector();
            var canvasRoot = CreateCanvasRoot("MetaCanvas");
            var background = CreateStretchImage("Background", canvasRoot.transform, VerticalSliceEditorUtil.BackgroundColor);
            theme.PanelGraphics.Add(background);

            var backButton = CreateButton(canvasRoot.transform, "BackButton", "Back", context, new Vector2(-360f, 820f), new Vector2(240f, 96f));
            var upgradesTabButton = CreateButton(canvasRoot.transform, "UpgradesTabButton", "Upgrades", context, new Vector2(-240f, 660f), new Vector2(280f, 96f));
            var cosmeticsTabButton = CreateButton(canvasRoot.transform, "CosmeticsTabButton", "Cosmetics", context, new Vector2(0f, 660f), new Vector2(280f, 96f));
            var shopTabButton = CreateButton(canvasRoot.transform, "ShopTabButton", "Shop", context, new Vector2(240f, 660f), new Vector2(280f, 96f));
            RegisterButton(theme, backButton);
            RegisterButton(theme, upgradesTabButton);
            RegisterButton(theme, cosmeticsTabButton);
            RegisterButton(theme, shopTabButton);

            var titleText = CreateText(canvasRoot.transform, "TitleText", "Permanent Upgrades", context.TitleFont, 64, new Vector2(0f, 820f), new Vector2(700f, 120f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            var softCurrencyText = CreateText(canvasRoot.transform, "SoftCurrencyText", "0", context.BodyFont, 42, new Vector2(380f, 820f), new Vector2(220f, 80f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.BodyTextColor);
            theme.TitleTexts.Add(titleText);
            theme.BodyTexts.Add(softCurrencyText);

            var upgradesPanel = new GameObject("UpgradesPanel", typeof(RectTransform));
            upgradesPanel.transform.SetParent(canvasRoot.transform, false);
            ConfigureRect((RectTransform)upgradesPanel.transform, new Vector2(0f, 50f), new Vector2(1040f, 1160f));

            var cosmeticsPanel = new GameObject("CosmeticsPanel", typeof(RectTransform));
            cosmeticsPanel.transform.SetParent(canvasRoot.transform, false);
            ConfigureRect((RectTransform)cosmeticsPanel.transform, new Vector2(0f, 50f), new Vector2(1040f, 1160f));
            cosmeticsPanel.SetActive(false);

            var shopPanel = new GameObject("ShopPanel", typeof(RectTransform));
            shopPanel.transform.SetParent(canvasRoot.transform, false);
            ConfigureRect((RectTransform)shopPanel.transform, new Vector2(0f, 50f), new Vector2(1040f, 1160f));
            shopPanel.SetActive(false);

            var cardPositions = new[]
            {
                new Vector2(-250f, 250f),
                new Vector2(250f, 250f),
                new Vector2(-250f, -180f),
                new Vector2(250f, -180f)
            };

            for (var i = 0; i < context.PermanentUpgrades.Length && i < cardPositions.Length; i++)
            {
                CreatePermanentUpgradeCard(upgradesPanel.transform, context, theme, context.PermanentUpgrades[i], $"PermanentCard_{i + 1}", cardPositions[i]);
            }

            var selectedCosmeticText = CreateText(cosmeticsPanel.transform, "SelectedCosmeticText", "Selected: Classic Rush", context.BodyFont, 38, new Vector2(0f, 420f), new Vector2(760f, 60f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.Add(selectedCosmeticText);

            var cosmeticCards = new List<CosmeticCardElements>();
            for (var i = 0; i < context.Cosmetics.Length; i++)
            {
                cosmeticCards.Add(CreateCosmeticCard(
                    cosmeticsPanel.transform,
                    context,
                    theme,
                    context.Cosmetics[i],
                    $"CosmeticCard_{i + 1}",
                    new Vector2(0f, 230f - (i * 280f))));
            }

            var cosmeticsPresenter = cosmeticsPanel.AddComponent<CosmeticsPlaceholderPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(cosmeticsPresenter, "selectedCosmeticText", selectedCosmeticText);
            VerticalSliceEditorUtil.SetPrivateField(cosmeticsPresenter, "entries", BuildCosmeticEntryArray(cosmeticCards.ToArray()));

            var shopPlaceholderPanel = CreatePanel("ShopPlaceholderPanel", shopPanel.transform, new Vector2(880f, 420f), new Vector2(0f, 120f));
            theme.PanelGraphics.Add(shopPlaceholderPanel.Image);
            var shopPlaceholderText = CreateText(shopPlaceholderPanel.Content, "ShopPlaceholderText", "Reward bundles, remove-ads, and cosmetic packs will be connected here later.", context.BodyFont, 42, Vector2.zero, new Vector2(760f, 260f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.Add(shopPlaceholderText);

            var metaPresenter = canvasRoot.AddComponent<MetaProgressionPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "upgradesTabButton", upgradesTabButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "cosmeticsTabButton", cosmeticsTabButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "shopTabButton", shopTabButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "backButton", backButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "upgradesPanel", upgradesPanel);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "cosmeticsPanel", cosmeticsPanel);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "shopPanel", shopPanel);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "titleText", titleText);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "softCurrencyText", softCurrencyText);
            VerticalSliceEditorUtil.SetPrivateField(metaPresenter, "shopPlaceholderText", shopPlaceholderText);

            ApplyTheme(canvasRoot, theme);
            EditorSceneManager.SaveScene(scene, MetaScenePath);
        }

        private static void BuildGameScene(VerticalSliceBuildContext context, bool force)
        {
            if (!force && File.Exists(GameScenePath))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = CreateCamera("Main Camera", 7f, new Color(0.98f, 0.95f, 0.87f));
            CreateEventSystem();

            var inputRoot = new GameObject("InputRoot");
            inputRoot.AddComponent<TouchInputReader>();

            var playerInstance = PrefabUtility.InstantiatePrefab(context.PlayerPrefab) as GameObject;
            if (playerInstance == null)
            {
                throw new InvalidOperationException("Failed to instantiate player prefab.");
            }

            playerInstance.name = "Player";
            playerInstance.transform.position = new Vector3(0f, -1.5f, 0f);

            var playerController = playerInstance.GetComponent<PlayerController>();
            var playerHealth = playerInstance.GetComponent<Health>();

            var systemsRoot = new GameObject("Systems");
            var runDirector = systemsRoot.AddComponent<RunDirector>();
            var progressionController = systemsRoot.AddComponent<RunProgressionController>();
            var objectPool = systemsRoot.AddComponent<ObjectPool>();
            var enemySpawner = systemsRoot.AddComponent<EnemySpawner>();

            VerticalSliceEditorUtil.SetPrivateField(runDirector, "player", playerController);
            VerticalSliceEditorUtil.SetPrivateField(runDirector, "playerHealth", playerHealth);
            VerticalSliceEditorUtil.SetPrivateField(runDirector, "requestReviveOnPlayerDeath", true);

            VerticalSliceEditorUtil.SetPrivateField(progressionController, "runDirector", runDirector);
            VerticalSliceEditorUtil.SetPrivateField(progressionController, "player", playerController);
            VerticalSliceEditorUtil.SetPrivateField(progressionController, "upgradeCatalog", context.RunUpgrades);

            VerticalSliceEditorUtil.SetPrivateField(objectPool, "pools", BuildObjectPoolEntries(context));
            VerticalSliceEditorUtil.SetPrivateField(enemySpawner, "objectPool", objectPool);
            VerticalSliceEditorUtil.SetPrivateField(enemySpawner, "runDirector", runDirector);
            VerticalSliceEditorUtil.SetPrivateField(enemySpawner, "gameplayCamera", camera);
            VerticalSliceEditorUtil.SetPrivateField(enemySpawner, "spawnEntries", BuildSpawnerEntries());

            var theme = new ThemeCollector();
            var canvasRoot = CreateCanvasRoot("GameCanvas");
            var hudRoot = new GameObject("HudRoot", typeof(RectTransform));
            hudRoot.transform.SetParent(canvasRoot.transform, false);
            ConfigureRect((RectTransform)hudRoot.transform, Vector2.zero, new Vector2(1080f, 1920f));

            var timerBar = CreateStatusBar(hudRoot.transform, "TimerBar", "Time", context, theme, new Vector2(-320f, 840f), new Vector2(300f, 110f), new Color(0.92f, 0.88f, 0.80f));
            var scoreBar = CreateStatusBar(hudRoot.transform, "ScoreBar", "Score", context, theme, new Vector2(0f, 840f), new Vector2(300f, 110f), new Color(0.92f, 0.88f, 0.80f));
            var coinsBar = CreateStatusBar(hudRoot.transform, "CoinsBar", "Coins", context, theme, new Vector2(320f, 840f), new Vector2(300f, 110f), new Color(0.92f, 0.88f, 0.80f));
            var healthBar = CreateProgressBar(hudRoot.transform, "HealthBar", "Health", context, theme, new Vector2(-210f, 710f), new Vector2(480f, 110f), VerticalSliceEditorUtil.DangerColor);
            var xpBar = CreateProgressBar(hudRoot.transform, "ExperienceBar", "XP", context, theme, new Vector2(230f, 710f), new Vector2(560f, 110f), new Color(0.29f, 0.83f, 0.96f));
            var levelText = CreateText(xpBar.Root.transform, "LevelText", "Lv 1", context.BodyFont, 34, new Vector2(0f, 18f), new Vector2(140f, 40f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.Add(levelText);

            var pauseButton = CreateButton(hudRoot.transform, "PauseButton", "II", context, new Vector2(450f, 840f), new Vector2(120f, 120f));
            RegisterButton(theme, pauseButton);

            var levelUpOverlay = CreateOverlayPanel(canvasRoot.transform, "LevelUpOverlay", new Vector2(900f, 1260f), "Choose Upgrade", context, theme);
            levelUpOverlay.Root.SetActive(false);
            var levelOptions = new[]
            {
                CreateLevelUpOption(levelUpOverlay.Window, context, theme, "LevelOption_1", new Vector2(0f, 260f)),
                CreateLevelUpOption(levelUpOverlay.Window, context, theme, "LevelOption_2", new Vector2(0f, 0f)),
                CreateLevelUpOption(levelUpOverlay.Window, context, theme, "LevelOption_3", new Vector2(0f, -260f))
            };

            var pauseOverlay = CreateOverlayPanel(canvasRoot.transform, "PauseOverlay", new Vector2(720f, 620f), "Paused", context, theme);
            pauseOverlay.Root.SetActive(false);
            var resumeButton = CreateButton(pauseOverlay.Window, "ResumeButton", "Resume", context, new Vector2(0f, 40f), new Vector2(460f, 110f));
            var pauseBackButton = CreateButton(pauseOverlay.Window, "PauseBackButton", "Back To Menu", context, new Vector2(0f, -130f), new Vector2(460f, 110f));
            RegisterButton(theme, resumeButton);
            RegisterButton(theme, pauseBackButton);

            var reviveOverlay = CreateOverlayPanel(canvasRoot.transform, "ReviveOverlay", new Vector2(760f, 560f), "One More Chance", context, theme);
            reviveOverlay.Root.SetActive(false);
            var reviveBodyText = CreateText(reviveOverlay.Window, "ReviveBodyText", "Revive with 50% health and keep this run going.", context.BodyFont, 38, new Vector2(0f, 70f), new Vector2(640f, 120f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.Add(reviveBodyText);
            var reviveButton = CreateButton(reviveOverlay.Window, "ReviveButton", "Revive", context, new Vector2(-140f, -120f), new Vector2(250f, 100f));
            var skipButton = CreateButton(reviveOverlay.Window, "SkipReviveButton", "Skip", context, new Vector2(140f, -120f), new Vector2(250f, 100f));
            RegisterButton(theme, reviveButton);
            RegisterButton(theme, skipButton);

            var endOverlay = CreateOverlayPanel(canvasRoot.transform, "RunEndOverlay", new Vector2(780f, 760f), "Run Complete", context, theme);
            endOverlay.Root.SetActive(false);
            var timeSummary = CreateSummaryRow(endOverlay.Window, "TimeSummary", "Time", context, theme, new Vector2(0f, 180f));
            var scoreSummary = CreateSummaryRow(endOverlay.Window, "ScoreSummary", "Score", context, theme, new Vector2(0f, 90f));
            var coinsSummary = CreateSummaryRow(endOverlay.Window, "CoinsSummary", "Coins", context, theme, new Vector2(0f, 0f));
            var levelSummary = CreateSummaryRow(endOverlay.Window, "LevelSummary", "Level", context, theme, new Vector2(0f, -90f));
            var restartButton = CreateButton(endOverlay.Window, "RestartButton", "Play Again", context, new Vector2(0f, -240f), new Vector2(460f, 110f));
            var endBackButton = CreateButton(endOverlay.Window, "EndBackButton", "Back To Menu", context, new Vector2(0f, -390f), new Vector2(460f, 110f));
            RegisterButton(theme, restartButton);
            RegisterButton(theme, endBackButton);

            var hudPresenter = hudRoot.AddComponent<RunHudPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "runDirector", runDirector);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "progressionController", progressionController);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "playerHealth", playerHealth);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "timerText", timerBar.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "scoreText", scoreBar.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "coinsText", coinsBar.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "levelText", levelText);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "experienceText", xpBar.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "healthText", healthBar.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "experienceFillImage", xpBar.FillImage);
            VerticalSliceEditorUtil.SetPrivateField(hudPresenter, "healthFillImage", healthBar.FillImage);

            var levelUpPresenter = canvasRoot.AddComponent<LevelUpPopupPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(levelUpPresenter, "progressionController", progressionController);
            VerticalSliceEditorUtil.SetPrivateField(levelUpPresenter, "panelRoot", levelUpOverlay.Root);
            VerticalSliceEditorUtil.SetPrivateField(levelUpPresenter, "titleText", levelUpOverlay.TitleText);
            VerticalSliceEditorUtil.SetPrivateField(levelUpPresenter, "optionButtons", levelOptions);

            var pausePresenter = canvasRoot.AddComponent<PauseMenuPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(pausePresenter, "runDirector", runDirector);
            VerticalSliceEditorUtil.SetPrivateField(pausePresenter, "panelRoot", pauseOverlay.Root);
            VerticalSliceEditorUtil.SetPrivateField(pausePresenter, "pauseButton", pauseButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(pausePresenter, "resumeButton", resumeButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(pausePresenter, "backToMenuButton", pauseBackButton.Button);

            var revivePresenter = canvasRoot.AddComponent<ReviveOfferPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(revivePresenter, "runDirector", runDirector);
            VerticalSliceEditorUtil.SetPrivateField(revivePresenter, "panelRoot", reviveOverlay.Root);
            VerticalSliceEditorUtil.SetPrivateField(revivePresenter, "reviveButton", reviveButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(revivePresenter, "skipButton", skipButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(revivePresenter, "titleText", reviveOverlay.TitleText);
            VerticalSliceEditorUtil.SetPrivateField(revivePresenter, "bodyText", reviveBodyText);

            var endPresenter = canvasRoot.AddComponent<RunEndPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "runDirector", runDirector);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "progressionController", progressionController);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "panelRoot", endOverlay.Root);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "timeValueText", timeSummary.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "scoreValueText", scoreSummary.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "coinsValueText", coinsSummary.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "levelValueText", levelSummary.ValueText);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "restartButton", restartButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(endPresenter, "backToMenuButton", endBackButton.Button);

            ApplyTheme(canvasRoot, theme);
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static Array BuildObjectPoolEntries(VerticalSliceBuildContext context)
        {
            var entries = VerticalSliceEditorUtil.CreatePrivateNestedArray(typeof(ObjectPool), "PoolEntry", 3);

            var enemyEntry = VerticalSliceEditorUtil.CreatePrivateNestedInstance(typeof(ObjectPool), "PoolEntry");
            VerticalSliceEditorUtil.SetPrivateField(enemyEntry, "key", "BasicEnemy");
            VerticalSliceEditorUtil.SetPrivateField(enemyEntry, "prefab", context.BasicEnemyPrefab);
            VerticalSliceEditorUtil.SetPrivateField(enemyEntry, "initialSize", 16);
            VerticalSliceEditorUtil.SetPrivateField(enemyEntry, "canExpand", true);
            entries.SetValue(enemyEntry, 0);

            var coinEntry = VerticalSliceEditorUtil.CreatePrivateNestedInstance(typeof(ObjectPool), "PoolEntry");
            VerticalSliceEditorUtil.SetPrivateField(coinEntry, "key", "CoinPickup");
            VerticalSliceEditorUtil.SetPrivateField(coinEntry, "prefab", context.CoinPickupPrefab);
            VerticalSliceEditorUtil.SetPrivateField(coinEntry, "initialSize", 32);
            VerticalSliceEditorUtil.SetPrivateField(coinEntry, "canExpand", true);
            entries.SetValue(coinEntry, 1);

            var xpEntry = VerticalSliceEditorUtil.CreatePrivateNestedInstance(typeof(ObjectPool), "PoolEntry");
            VerticalSliceEditorUtil.SetPrivateField(xpEntry, "key", "ExperiencePickup");
            VerticalSliceEditorUtil.SetPrivateField(xpEntry, "prefab", context.ExperiencePickupPrefab);
            VerticalSliceEditorUtil.SetPrivateField(xpEntry, "initialSize", 32);
            VerticalSliceEditorUtil.SetPrivateField(xpEntry, "canExpand", true);
            entries.SetValue(xpEntry, 2);

            return entries;
        }

        private static Array BuildSpawnerEntries()
        {
            var entries = VerticalSliceEditorUtil.CreatePrivateNestedArray(typeof(EnemySpawner), "SpawnEntry", 1);
            var spawnEntry = VerticalSliceEditorUtil.CreatePrivateNestedInstance(typeof(EnemySpawner), "SpawnEntry");
            VerticalSliceEditorUtil.SetPrivateField(spawnEntry, "poolKey", "BasicEnemy");
            VerticalSliceEditorUtil.SetPrivateField(spawnEntry, "weight", 1f);
            VerticalSliceEditorUtil.SetPrivateField(spawnEntry, "unlockAtSeconds", 0f);
            entries.SetValue(spawnEntry, 0);
            return entries;
        }

        private static Array BuildCosmeticEntryArray(CosmeticCardElements[] cards)
        {
            var entries = VerticalSliceEditorUtil.CreatePrivateNestedArray(typeof(CosmeticsPlaceholderPresenter), "CosmeticEntryView", cards.Length);

            for (var i = 0; i < cards.Length; i++)
            {
                var entry = VerticalSliceEditorUtil.CreatePrivateNestedInstance(typeof(CosmeticsPlaceholderPresenter), "CosmeticEntryView");
                VerticalSliceEditorUtil.SetPrivateField(entry, "definition", cards[i].Definition);
                VerticalSliceEditorUtil.SetPrivateField(entry, "cardRoot", cards[i].Root);
                VerticalSliceEditorUtil.SetPrivateField(entry, "actionButton", cards[i].ActionButton.Button);
                VerticalSliceEditorUtil.SetPrivateField(entry, "titleText", cards[i].TitleText);
                VerticalSliceEditorUtil.SetPrivateField(entry, "stateText", cards[i].StateText);
                VerticalSliceEditorUtil.SetPrivateField(entry, "costText", cards[i].CostText);
                VerticalSliceEditorUtil.SetPrivateField(entry, "actionText", cards[i].ActionButton.LabelText);
                VerticalSliceEditorUtil.SetPrivateField(entry, "iconImage", cards[i].IconImage);
                VerticalSliceEditorUtil.SetPrivateField(entry, "colorSwatch", cards[i].ColorSwatch);
                entries.SetValue(entry, i);
            }

            return entries;
        }

        private static PermanentUpgradeCardPresenter CreatePermanentUpgradeCard(
            Transform parent,
            VerticalSliceBuildContext context,
            ThemeCollector theme,
            PermanentUpgradeDefinition definition,
            string cardName,
            Vector2 anchoredPosition)
        {
            var panel = CreatePanel(cardName, parent, new Vector2(430f, 360f), anchoredPosition);
            theme.PanelGraphics.Add(panel.Image);

            var iconImage = CreateIcon(panel.Content, "Icon", new Vector2(-150f, 105f), new Vector2(90f, 90f), VerticalSliceEditorUtil.AccentColor);
            var titleText = CreateText(panel.Content, "TitleText", definition.DisplayName, context.BodyFont, 36, new Vector2(-25f, 110f), new Vector2(240f, 50f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.BodyTextColor);
            var descriptionText = CreateText(panel.Content, "DescriptionText", definition.Description, context.BodyFont, 24, new Vector2(0f, 45f), new Vector2(360f, 80f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.SecondaryTextColor);
            var levelText = CreateText(panel.Content, "LevelText", "Lv 0/5", context.BodyFont, 28, new Vector2(-100f, -35f), new Vector2(160f, 40f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.BodyTextColor);
            var costText = CreateText(panel.Content, "CostText", "40", context.BodyFont, 28, new Vector2(120f, -35f), new Vector2(120f, 40f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.BodyTextColor);
            var bonusText = CreateText(panel.Content, "BonusText", "+1 / level", context.BodyFont, 24, new Vector2(0f, -90f), new Vector2(320f, 34f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.SecondaryTextColor);
            var buyButton = CreateButton(panel.Content, "BuyButton", "Buy", context, new Vector2(0f, -155f), new Vector2(220f, 86f));

            theme.BodyTexts.AddRange(new[] { titleText, descriptionText, levelText, costText, bonusText });
            RegisterButton(theme, buyButton);

            var presenter = panel.Root.AddComponent<PermanentUpgradeCardPresenter>();
            VerticalSliceEditorUtil.SetPrivateField(presenter, "upgradeDefinition", definition);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "cardRoot", panel.Root);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "purchaseButton", buyButton.Button);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "titleText", titleText);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "descriptionText", descriptionText);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "levelText", levelText);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "costText", costText);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "bonusText", bonusText);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "buttonText", buyButton.LabelText);
            VerticalSliceEditorUtil.SetPrivateField(presenter, "iconImage", iconImage);

            return presenter;
        }

        private static CosmeticCardElements CreateCosmeticCard(
            Transform parent,
            VerticalSliceBuildContext context,
            ThemeCollector theme,
            CosmeticDefinition definition,
            string cardName,
            Vector2 anchoredPosition)
        {
            var panel = CreatePanel(cardName, parent, new Vector2(860f, 230f), anchoredPosition);
            theme.PanelGraphics.Add(panel.Image);

            var colorSwatch = CreateIcon(panel.Content, "ColorSwatch", new Vector2(-320f, 0f), new Vector2(120f, 120f), definition.PreviewColor);
            var iconImage = CreateIcon(panel.Content, "IconImage", new Vector2(-320f, 0f), new Vector2(90f, 90f), Color.white);
            var titleText = CreateText(panel.Content, "TitleText", definition.DisplayName, context.BodyFont, 38, new Vector2(-120f, 45f), new Vector2(380f, 48f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.BodyTextColor);
            var stateText = CreateText(panel.Content, "StateText", definition.DefaultUnlocked ? "Selected" : "Locked", context.BodyFont, 28, new Vector2(-120f, -10f), new Vector2(280f, 36f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var costText = CreateText(panel.Content, "CostText", definition.UnlockCost == 0 ? "Owned" : $"{definition.UnlockCost} Coins", context.BodyFont, 28, new Vector2(-120f, -55f), new Vector2(280f, 36f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var actionButton = CreateButton(panel.Content, "ActionButton", definition.DefaultUnlocked ? "Select" : "Unlock", context, new Vector2(250f, 0f), new Vector2(220f, 88f));

            theme.BodyTexts.AddRange(new[] { titleText, stateText, costText });
            RegisterButton(theme, actionButton);

            return new CosmeticCardElements
            {
                Definition = definition,
                Root = panel.Root,
                TitleText = titleText,
                StateText = stateText,
                CostText = costText,
                ActionButton = actionButton,
                IconImage = iconImage,
                ColorSwatch = colorSwatch
            };
        }

        private static LevelUpOptionButton CreateLevelUpOption(
            Transform parent,
            VerticalSliceBuildContext context,
            ThemeCollector theme,
            string name,
            Vector2 anchoredPosition)
        {
            var buttonElements = CreateButton(parent, name, "Upgrade", context, anchoredPosition, new Vector2(760f, 210f));
            RegisterButton(theme, buttonElements);

            ConfigureRect(buttonElements.LabelText.rectTransform, new Vector2(0f, 60f), new Vector2(620f, 48f));
            buttonElements.LabelText.fontSize = 38;

            var iconImage = CreateIcon(buttonElements.Root.transform, "IconImage", new Vector2(-290f, 0f), new Vector2(90f, 90f), VerticalSliceEditorUtil.AccentColor);
            var descriptionText = CreateText(buttonElements.Root.transform, "DescriptionText", "Upgrade description.", context.BodyFont, 24, new Vector2(40f, 6f), new Vector2(470f, 56f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var stackText = CreateText(buttonElements.Root.transform, "StackText", "Lv 1/5", context.BodyFont, 22, new Vector2(0f, -56f), new Vector2(300f, 36f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.AddRange(new[] { descriptionText, stackText });

            var optionButton = buttonElements.Root.AddComponent<LevelUpOptionButton>();
            VerticalSliceEditorUtil.SetPrivateField(optionButton, "button", buttonElements.Button);
            VerticalSliceEditorUtil.SetPrivateField(optionButton, "titleText", buttonElements.LabelText);
            VerticalSliceEditorUtil.SetPrivateField(optionButton, "descriptionText", descriptionText);
            VerticalSliceEditorUtil.SetPrivateField(optionButton, "stackText", stackText);
            VerticalSliceEditorUtil.SetPrivateField(optionButton, "iconImage", iconImage);

            return optionButton;
        }

        private static ButtonElements CreateButton(Transform parent, string name, string label, VerticalSliceBuildContext context, Vector2 anchoredPosition, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            ConfigureRect((RectTransform)root.transform, anchoredPosition, size);

            var image = root.GetComponent<Image>();
            image.sprite = context.UiSprite;
            image.type = Image.Type.Sliced;
            image.color = VerticalSliceEditorUtil.AccentColor;

            var button = root.GetComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateText(root.transform, "LabelText", label, context.BodyFont, 40, Vector2.zero, size, TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);

            return new ButtonElements
            {
                Root = root,
                Button = button,
                Image = image,
                LabelText = labelText
            };
        }

        private static ToggleElements CreateToggleRow(Transform parent, string name, string label, VerticalSliceBuildContext context, Vector2 anchoredPosition, Vector2 size, bool defaultValue, ThemeCollector theme)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            ConfigureRect((RectTransform)root.transform, anchoredPosition, size);

            var labelText = CreateText(root.transform, "LabelText", label, context.BodyFont, 34, new Vector2(-100f, 0f), new Vector2(340f, 50f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.Add(labelText);

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(root.transform, false);
            ConfigureRect((RectTransform)backgroundObject.transform, new Vector2(260f, 0f), new Vector2(68f, 68f));
            var backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.sprite = context.UiSprite;
            backgroundImage.color = VerticalSliceEditorUtil.PanelColor;
            theme.PanelGraphics.Add(backgroundImage);

            var checkmarkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmarkObject.transform.SetParent(backgroundObject.transform, false);
            ConfigureRect((RectTransform)checkmarkObject.transform, Vector2.zero, new Vector2(42f, 42f));
            var checkmarkImage = checkmarkObject.GetComponent<Image>();
            checkmarkImage.sprite = context.UiSprite;
            checkmarkImage.color = VerticalSliceEditorUtil.AccentColor;
            theme.AccentGraphics.Add(checkmarkImage);

            var toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;
            toggle.isOn = defaultValue;

            return new ToggleElements
            {
                Root = root,
                Toggle = toggle,
                LabelText = labelText
            };
        }

        private static SliderElements CreateSliderRow(Transform parent, string name, string label, VerticalSliceBuildContext context, Vector2 anchoredPosition, Vector2 size, ThemeCollector theme)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            ConfigureRect((RectTransform)root.transform, anchoredPosition, size);

            var labelText = CreateText(root.transform, "LabelText", label, context.BodyFont, 30, new Vector2(-220f, 35f), new Vector2(320f, 36f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.BodyTextColor);
            var valueText = CreateText(root.transform, "ValueText", "80%", context.BodyFont, 28, new Vector2(260f, 35f), new Vector2(120f, 34f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.SecondaryTextColor);
            theme.BodyTexts.AddRange(new[] { labelText, valueText });

            var sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(root.transform, false);
            ConfigureRect((RectTransform)sliderObject.transform, new Vector2(0f, -18f), new Vector2(700f, 42f));

            var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(sliderObject.transform, false);
            ConfigureStretchRect((RectTransform)backgroundObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var backgroundImage = backgroundObject.GetComponent<Image>();
            backgroundImage.sprite = context.UiSprite;
            backgroundImage.color = new Color(0.87f, 0.83f, 0.76f);
            theme.PanelGraphics.Add(backgroundImage);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(sliderObject.transform, false);
            ConfigureStretchRect((RectTransform)fillObject.transform, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            var fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = context.UiSprite;
            fillImage.color = VerticalSliceEditorUtil.AccentColor;
            theme.AccentGraphics.Add(fillImage);

            var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(sliderObject.transform, false);
            ConfigureRect((RectTransform)handleObject.transform, Vector2.zero, new Vector2(34f, 54f));
            var handleImage = handleObject.GetComponent<Image>();
            handleImage.sprite = context.UiSprite;
            handleImage.color = VerticalSliceEditorUtil.BodyTextColor;

            var slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillObject.GetComponent<RectTransform>();
            slider.handleRect = handleObject.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            return new SliderElements
            {
                Root = root,
                Slider = slider,
                LabelText = labelText,
                ValueText = valueText
            };
        }

        private static StatusBarElements CreateStatusBar(Transform parent, string name, string label, VerticalSliceBuildContext context, ThemeCollector theme, Vector2 anchoredPosition, Vector2 size, Color panelColor)
        {
            var panel = CreatePanel(name, parent, size, anchoredPosition, panelColor);
            theme.PanelGraphics.Add(panel.Image);
            var labelText = CreateText(panel.Content, "LabelText", label, context.BodyFont, 24, new Vector2(0f, 18f), new Vector2(size.x - 32f, 30f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.SecondaryTextColor);
            var valueText = CreateText(panel.Content, "ValueText", "0", context.BodyFont, 36, new Vector2(0f, -18f), new Vector2(size.x - 32f, 40f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.AddRange(new[] { labelText, valueText });
            return new StatusBarElements { Root = panel.Root, LabelText = labelText, ValueText = valueText };
        }

        private static ProgressBarElements CreateProgressBar(Transform parent, string name, string label, VerticalSliceBuildContext context, ThemeCollector theme, Vector2 anchoredPosition, Vector2 size, Color fillColor)
        {
            var panel = CreatePanel(name, parent, size, anchoredPosition, new Color(0.92f, 0.88f, 0.80f));
            theme.PanelGraphics.Add(panel.Image);

            var labelText = CreateText(panel.Content, "LabelText", label, context.BodyFont, 24, new Vector2(-size.x * 0.33f, 22f), new Vector2(180f, 30f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var valueText = CreateText(panel.Content, "ValueText", "0/0", context.BodyFont, 24, new Vector2(size.x * 0.25f, 22f), new Vector2(180f, 30f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.AddRange(new[] { labelText, valueText });

            var barBackground = CreateStretchImage("BarBackground", panel.Content, new Color(0.84f, 0.80f, 0.72f));
            ConfigureStretchRect(barBackground.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, 12f), new Vector2(-28f, 58f));
            theme.PanelGraphics.Add(barBackground);

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(barBackground.transform, false);
            ConfigureStretchRect((RectTransform)fillObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = context.UiSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.color = fillColor;
            theme.AccentGraphics.Add(fillImage);

            return new ProgressBarElements { Root = panel.Root, LabelText = labelText, ValueText = valueText, FillImage = fillImage };
        }

        private static SummaryRowElements CreateSummaryRow(Transform parent, string name, string label, VerticalSliceBuildContext context, ThemeCollector theme, Vector2 anchoredPosition)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            ConfigureRect((RectTransform)root.transform, anchoredPosition, new Vector2(520f, 54f));

            var labelText = CreateText(root.transform, "LabelText", label, context.BodyFont, 28, new Vector2(-140f, 0f), new Vector2(180f, 40f), TextAnchor.MiddleLeft, VerticalSliceEditorUtil.SecondaryTextColor);
            var valueText = CreateText(root.transform, "ValueText", "0", context.BodyFont, 32, new Vector2(140f, 0f), new Vector2(180f, 40f), TextAnchor.MiddleRight, VerticalSliceEditorUtil.BodyTextColor);
            theme.BodyTexts.AddRange(new[] { labelText, valueText });

            return new SummaryRowElements { Root = root, LabelText = labelText, ValueText = valueText };
        }

        private static OverlayPanelElements CreateOverlayPanel(Transform parent, string name, Vector2 windowSize, string title, VerticalSliceBuildContext context, ThemeCollector theme)
        {
            var overlayRoot = new GameObject(name, typeof(RectTransform), typeof(Image));
            overlayRoot.transform.SetParent(parent, false);
            ConfigureStretchRect((RectTransform)overlayRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlayImage = overlayRoot.GetComponent<Image>();
            overlayImage.sprite = context.UiSprite;
            overlayImage.color = new Color(0.08f, 0.12f, 0.18f, 0.55f);

            var windowPanel = CreatePanel("Window", overlayRoot.transform, windowSize, Vector2.zero);
            theme.PanelGraphics.Add(windowPanel.Image);

            var titleText = CreateText(windowPanel.Content, "TitleText", title, context.TitleFont, 56, new Vector2(0f, windowSize.y * 0.5f - 110f), new Vector2(windowSize.x - 120f, 96f), TextAnchor.MiddleCenter, VerticalSliceEditorUtil.BodyTextColor);
            theme.TitleTexts.Add(titleText);

            return new OverlayPanelElements { Root = overlayRoot, Window = windowPanel.Content, TitleText = titleText };
        }

        private static PanelElements CreatePanel(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
        {
            return CreatePanel(name, parent, size, anchoredPosition, VerticalSliceEditorUtil.PanelColor);
        }

        private static PanelElements CreatePanel(string name, Transform parent, Vector2 size, Vector2 anchoredPosition, Color panelColor)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            ConfigureRect((RectTransform)root.transform, anchoredPosition, size);

            var image = root.GetComponent<Image>();
            image.color = panelColor;
            image.sprite = VerticalSliceEditorUtil.GetDefaultUiSprite();
            image.type = Image.Type.Sliced;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            ConfigureRect((RectTransform)content.transform, Vector2.zero, size - new Vector2(30f, 30f));

            return new PanelElements { Root = root, Content = content.transform, Image = image };
        }

        private static GameObject CreateCanvasRoot(string name)
        {
            var canvasRoot = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SafeAreaFitter), typeof(UiFontThemeApplier));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            ConfigureStretchRect((RectTransform)canvasRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return canvasRoot;
        }

        private static Camera CreateCamera(string name, float orthographicSize, Color backgroundColor)
        {
            var cameraObject = new GameObject(name, typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static Image CreateStretchImage(string name, Transform parent, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var rectTransform = (RectTransform)imageObject.transform;
            ConfigureStretchRect(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var image = imageObject.GetComponent<Image>();
            image.sprite = VerticalSliceEditorUtil.GetDefaultUiSprite();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int fontSize, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            ConfigureRect((RectTransform)textObject.transform, anchoredPosition, size);

            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = font != null ? font : VerticalSliceEditorUtil.GetFallbackFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.supportRichText = false;
            return text;
        }

        private static Image CreateIcon(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            ConfigureRect((RectTransform)iconObject.transform, anchoredPosition, size);
            var image = iconObject.GetComponent<Image>();
            image.sprite = VerticalSliceEditorUtil.GetDefaultUiSprite();
            image.color = color;
            return image;
        }

        private static void ConfigureRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;
        }

        private static void ConfigureStretchRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
        }

        private static void ApplyTheme(GameObject canvasRoot, ThemeCollector theme)
        {
            var applier = canvasRoot.GetComponent<UiFontThemeApplier>();
            if (applier == null)
            {
                return;
            }

            VerticalSliceEditorUtil.SetPrivateField(applier, "titleTexts", theme.TitleTexts.ToArray());
            VerticalSliceEditorUtil.SetPrivateField(applier, "bodyTexts", theme.BodyTexts.ToArray());
            VerticalSliceEditorUtil.SetPrivateField(applier, "accentTexts", theme.AccentTexts.ToArray());
            VerticalSliceEditorUtil.SetPrivateField(applier, "accentGraphics", theme.AccentGraphics.ToArray());
            VerticalSliceEditorUtil.SetPrivateField(applier, "panelGraphics", theme.PanelGraphics.ToArray());
            applier.ApplyTheme();
        }

        private static void RegisterButton(ThemeCollector theme, ButtonElements button)
        {
            theme.AccentGraphics.Add(button.Image);
            theme.AccentTexts.Add(button.LabelText);
        }

        private sealed class ThemeCollector
        {
            public readonly List<Text> TitleTexts = new List<Text>();
            public readonly List<Text> BodyTexts = new List<Text>();
            public readonly List<Text> AccentTexts = new List<Text>();
            public readonly List<Graphic> AccentGraphics = new List<Graphic>();
            public readonly List<Graphic> PanelGraphics = new List<Graphic>();
        }

        private struct PanelElements
        {
            public GameObject Root;
            public Transform Content;
            public Image Image;
        }

        private struct OverlayPanelElements
        {
            public GameObject Root;
            public Transform Window;
            public Text TitleText;
        }

        private struct ButtonElements
        {
            public GameObject Root;
            public Button Button;
            public Image Image;
            public Text LabelText;
        }

        private struct ToggleElements
        {
            public GameObject Root;
            public Toggle Toggle;
            public Text LabelText;
        }

        private struct SliderElements
        {
            public GameObject Root;
            public Slider Slider;
            public Text LabelText;
            public Text ValueText;
        }

        private struct StatusBarElements
        {
            public GameObject Root;
            public Text LabelText;
            public Text ValueText;
        }

        private struct ProgressBarElements
        {
            public GameObject Root;
            public Text LabelText;
            public Text ValueText;
            public Image FillImage;
        }

        private struct SummaryRowElements
        {
            public GameObject Root;
            public Text LabelText;
            public Text ValueText;
        }

        private struct CosmeticCardElements
        {
            public CosmeticDefinition Definition;
            public GameObject Root;
            public Text TitleText;
            public Text StateText;
            public Text CostText;
            public ButtonElements ActionButton;
            public Image IconImage;
            public Image ColorSwatch;
        }
    }
}
#endif
