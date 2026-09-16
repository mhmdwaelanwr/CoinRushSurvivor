using UnityEngine;
using UnityEngine.EventSystems;

namespace CoinRushSurvivor.Player
{
    [DisallowMultipleComponent]
    public sealed class TouchInputReader : MonoBehaviour
    {
        private const int NoPointer = int.MinValue;

        [Header("Touch Drag")]
        [SerializeField, Min(0f)] private float deadZonePixels = 12f;
        [SerializeField, Min(16f)] private float maxDragPixels = 140f;
        [SerializeField] private bool ignoreTouchesOverUi = true;

        [Header("Editor Fallback")]
        [SerializeField] private bool useKeyboardFallbackInEditor = true;

        private int activePointerId = NoPointer;
        private bool mousePointerActive;

        public Vector2 MoveVector { get; private set; }
        public bool IsDragging { get; private set; }
        public Vector2 DragStartScreenPosition { get; private set; }
        public Vector2 CurrentScreenPosition { get; private set; }

        private void Update()
        {
            var hasNativeTouch = Input.touchCount > 0;

            if (hasNativeTouch)
            {
                ReadTouchInput();
            }
            else
            {
                if (activePointerId != NoPointer)
                {
                    ResetDrag();
                }

                ReadMouseInput();
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            if (!IsDragging && useKeyboardFallbackInEditor)
            {
                MoveVector = Vector2.ClampMagnitude(
                    new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                    1f);
            }
#endif
        }

        private void ReadTouchInput()
        {
            if (activePointerId == NoPointer)
            {
                TryBeginTouchDrag();
                return;
            }

            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.fingerId != activePointerId)
                {
                    continue;
                }

                CurrentScreenPosition = touch.position;

                switch (touch.phase)
                {
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        ResetDrag();
                        break;

                    default:
                        UpdateMoveVector(CurrentScreenPosition);
                        break;
                }

                return;
            }

            ResetDrag();
        }

        private void TryBeginTouchDrag()
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                {
                    continue;
                }

                if (ignoreTouchesOverUi &&
                    EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    continue;
                }

                activePointerId = touch.fingerId;
                DragStartScreenPosition = touch.position;
                CurrentScreenPosition = touch.position;
                IsDragging = true;
                MoveVector = Vector2.zero;
                return;
            }
        }

        private void ReadMouseInput()
        {
            var pointerPosition = (Vector2)Input.mousePosition;

            if (!mousePointerActive && Input.GetMouseButtonDown(0))
            {
                if (ignoreTouchesOverUi &&
                    EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                mousePointerActive = true;
                IsDragging = true;
                DragStartScreenPosition = pointerPosition;
                CurrentScreenPosition = pointerPosition;
                MoveVector = Vector2.zero;
                return;
            }

            if (mousePointerActive && Input.GetMouseButton(0))
            {
                CurrentScreenPosition = pointerPosition;
                UpdateMoveVector(CurrentScreenPosition);
            }

            if (mousePointerActive && Input.GetMouseButtonUp(0))
            {
                ResetDrag();
            }
        }

        private void UpdateMoveVector(Vector2 currentPosition)
        {
            var delta = currentPosition - DragStartScreenPosition;
            var distance = delta.magnitude;

            if (distance <= deadZonePixels)
            {
                MoveVector = Vector2.zero;
                return;
            }

            var effectiveRange = Mathf.Max(1f, maxDragPixels - deadZonePixels);
            var strength = Mathf.Clamp01((distance - deadZonePixels) / effectiveRange);

            MoveVector = delta.normalized * strength;
        }

        private void ResetDrag()
        {
            activePointerId = NoPointer;
            mousePointerActive = false;
            IsDragging = false;
            MoveVector = Vector2.zero;
        }
    }
}
