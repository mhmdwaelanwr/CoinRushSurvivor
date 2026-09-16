using UnityEngine;

namespace CoinRushSurvivor.Core
{
    public enum MetaScreenTab
    {
        Upgrades,
        Cosmetics,
        Shop
    }

    [DisallowMultipleComponent]
    public sealed class MenuNavigationState : MonoBehaviour
    {
        [SerializeField] private MetaScreenTab defaultMetaTab = MetaScreenTab.Upgrades;

        private static MenuNavigationState instance;
        private bool hasPendingTab;
        private MetaScreenTab pendingTab;

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

            if (ServiceLocator.TryGet<MenuNavigationState>(out var registeredState) &&
                ReferenceEquals(registeredState, this))
            {
                ServiceLocator.Unregister(this);
            }

            instance = null;
        }

        public void RequestMetaTab(MetaScreenTab tab)
        {
            pendingTab = tab;
            hasPendingTab = true;
        }

        public MetaScreenTab ConsumeRequestedMetaTab()
        {
            if (!hasPendingTab)
            {
                return defaultMetaTab;
            }

            hasPendingTab = false;
            return pendingTab;
        }
    }
}
