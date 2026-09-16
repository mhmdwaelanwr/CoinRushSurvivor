using CoinRushSurvivor.Core;
using CoinRushSurvivor.Gameplay;
using UnityEngine;

namespace CoinRushSurvivor.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TouchInputReader inputReader;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Health health;
        [SerializeField] private Camera gameplayCamera;

        [Header("Movement")]
        [SerializeField, Min(0.5f)] private float moveSpeed = 5f;
        [SerializeField, Min(0.1f)] private float acceleration = 18f;
        [SerializeField, Min(0.1f)] private float deceleration = 24f;
        [SerializeField, Min(0f)] private float worldPadding = 0.35f;
        [SerializeField] private bool clampToCameraBounds = true;

        [Header("Damage Aura")]
        [SerializeField] private Transform auraOrigin;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField, Min(0.1f)] private float auraRadius = 1.35f;
        [SerializeField, Min(0.1f)] private float auraDamage = 1f;
        [SerializeField, Min(0.05f)] private float auraTickInterval = 0.3f;
        [SerializeField, Min(4)] private int auraBufferSize = 24;

        [Header("Pickup Magnet")]
        [SerializeField, Min(0.1f)] private float magnetRadius = 2.2f;

        private Collider2D[] auraHits;
        private RunDirector runDirector;
        private Vector2 currentVelocity;
        private float auraTickTimer;
        private float baseMoveSpeed;
        private float baseAuraDamage;
        private float baseMagnetRadius;

        public float MoveSpeed => moveSpeed;
        public float MagnetRadius => magnetRadius;
        public Transform MagnetTarget => auraOrigin != null ? auraOrigin : transform;
        public Health Health => health;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = FindFirstObjectByType<TouchInputReader>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (auraOrigin == null)
            {
                auraOrigin = transform;
            }

            baseMoveSpeed = moveSpeed;
            baseAuraDamage = auraDamage;
            baseMagnetRadius = magnetRadius;

            body.gravityScale = 0f;
            body.freezeRotation = true;
            auraHits = new Collider2D[Mathf.Max(4, auraBufferSize)];
        }

        private void Start()
        {
            if (!ServiceLocator.TryGet(out runDirector))
            {
                runDirector = FindFirstObjectByType<RunDirector>();
            }

            if (runDirector != null)
            {
                runDirector.RegisterPlayer(this);
            }
        }

        private void Update()
        {
            if (!CanProcessGameplay())
            {
                return;
            }

            auraTickTimer -= Time.deltaTime;
            if (auraTickTimer > 0f)
            {
                return;
            }

            auraTickTimer = auraTickInterval;
            TickAuraDamage();
        }

        private void FixedUpdate()
        {
            var desiredVelocity = GetDesiredVelocity();
            var blendRate = desiredVelocity.sqrMagnitude > 0.001f ? acceleration : deceleration;

            currentVelocity = Vector2.MoveTowards(
                currentVelocity,
                desiredVelocity,
                blendRate * Time.fixedDeltaTime);

            var nextPosition = body.position + currentVelocity * Time.fixedDeltaTime;
            if (clampToCameraBounds)
            {
                nextPosition = ClampToCamera(nextPosition);
            }

            body.MovePosition(nextPosition);
        }

        public void AddMoveSpeed(float amount)
        {
            moveSpeed = Mathf.Max(0.5f, moveSpeed + amount);
        }

        public void AddMagnetRadius(float amount)
        {
            magnetRadius = Mathf.Max(0.5f, magnetRadius + amount);
        }

        public void AddAuraDamage(float amount)
        {
            auraDamage = Mathf.Max(0.1f, auraDamage + amount);
        }

        public void AddShieldChance(float amount)
        {
            if (health == null)
            {
                return;
            }

            health.AddDamageBlockChance(amount);
        }

        public void AddMaxHealth(float amount, bool healToFull = true)
        {
            if (health == null)
            {
                return;
            }

            health.SetMaxHealth(health.MaxHealth + amount, healToFull);
        }

        public void ResetRuntimeStats()
        {
            moveSpeed = baseMoveSpeed;
            auraDamage = baseAuraDamage;
            magnetRadius = baseMagnetRadius;
            auraTickTimer = 0f;
            currentVelocity = Vector2.zero;
        }

        private Vector2 GetDesiredVelocity()
        {
            if (!CanProcessGameplay() || inputReader == null)
            {
                return Vector2.zero;
            }

            return inputReader.MoveVector * moveSpeed;
        }

        private bool CanProcessGameplay()
        {
            if (health != null && health.IsDead)
            {
                return false;
            }

            return runDirector == null || runDirector.IsRunActive;
        }

        private Vector2 ClampToCamera(Vector2 position)
        {
            var cameraToUse = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (cameraToUse == null || !cameraToUse.orthographic)
            {
                return position;
            }

            var cameraPosition = cameraToUse.transform.position;
            var halfHeight = cameraToUse.orthographicSize;
            var halfWidth = halfHeight * cameraToUse.aspect;

            position.x = Mathf.Clamp(
                position.x,
                cameraPosition.x - halfWidth + worldPadding,
                cameraPosition.x + halfWidth - worldPadding);

            position.y = Mathf.Clamp(
                position.y,
                cameraPosition.y - halfHeight + worldPadding,
                cameraPosition.y + halfHeight - worldPadding);

            return position;
        }

        private void TickAuraDamage()
        {
            var origin = (Vector2)(auraOrigin != null ? auraOrigin.position : transform.position);
            var hitCount = Physics2D.OverlapCircleNonAlloc(origin, auraRadius, auraHits, enemyLayers);

            for (var i = 0; i < hitCount; i++)
            {
                var colliderHit = auraHits[i];
                if (colliderHit == null)
                {
                    continue;
                }

                var targetHealth = colliderHit.GetComponentInParent<Health>();
                if (targetHealth == null || targetHealth == health || targetHealth.IsDead)
                {
                    continue;
                }

                targetHealth.Damage(auraDamage, gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = auraOrigin != null ? auraOrigin.position : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, auraRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, magnetRadius);
        }
    }
}
