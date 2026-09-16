using CoinRushSurvivor.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoinRushSurvivor.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SaveService))]
    [RequireComponent(typeof(PlayerProfileService))]
    [RequireComponent(typeof(MenuNavigationState))]
    [RequireComponent(typeof(AudioService))]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private string bootstrapSceneName = "Bootstrap";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool loadMainMenuFromBootstrap = true;

        [Header("Application")]
        [SerializeField, Range(30, 120)] private int targetFrameRate = 60;
        [SerializeField] private bool keepScreenAwake = true;
        [SerializeField] private bool runInBackground;

        private static GameBootstrap instance;

        public static GameBootstrap Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            ConfigureApplication();
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (!loadMainMenuFromBootstrap)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name == bootstrapSceneName)
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            Time.timeScale = 1f;
            ServiceLocator.Unregister(this);
            instance = null;
        }

        private void ConfigureApplication()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = runInBackground;

            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;

            Screen.sleepTimeout = keepScreenAwake
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
        }
    }
}
