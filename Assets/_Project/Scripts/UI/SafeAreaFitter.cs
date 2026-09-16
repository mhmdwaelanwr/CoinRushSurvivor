using UnityEngine;

namespace CoinRushSurvivor.UI
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private bool applyLeft = true;
        [SerializeField] private bool applyRight = true;
        [SerializeField] private bool applyTop = true;
        [SerializeField] private bool applyBottom = true;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }
        }

        private void OnEnable()
        {
            ApplySafeArea(true);
        }

        private void Update()
        {
            ApplySafeArea(false);
        }

        private void ApplySafeArea(bool force)
        {
            if (target == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);

            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            var currentAnchorMin = target.anchorMin;
            var currentAnchorMax = target.anchorMax;

            target.anchorMin = new Vector2(
                applyLeft ? anchorMin.x : currentAnchorMin.x,
                applyBottom ? anchorMin.y : currentAnchorMin.y);

            target.anchorMax = new Vector2(
                applyRight ? anchorMax.x : currentAnchorMax.x,
                applyTop ? anchorMax.y : currentAnchorMax.y);
        }
    }
}
