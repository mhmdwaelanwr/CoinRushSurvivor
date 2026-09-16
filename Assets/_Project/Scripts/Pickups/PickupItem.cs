using CoinRushSurvivor.Core;
using CoinRushSurvivor.Gameplay;
using CoinRushSurvivor.Player;
using UnityEngine;

namespace CoinRushSurvivor.Pickups
{
    public enum PickupType
    {
        Coin,
        Experience
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PickupItem : MonoBehaviour, IPooledObject
    {
        [Header("Reward")]
        [SerializeField] private PickupType pickupType = PickupType.Coin;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private string poolKey = "CoinPickup";

        [Header("Movement")]
        [SerializeField, Min(0.5f)] private float maxLifetime = 12f;
        [SerializeField, Min(0.1f)] private float magnetAcceleration = 22f;
        [SerializeField, Min(0.1f)] private float maxMagnetSpeed = 12f;
        [SerializeField, Min(0.1f)] private float idleDrag = 6f;
        [SerializeField, Min(0.01f)] private float collectDistance = 0.15f;
        [SerializeField, Min(0f)] private float idleSpinSpeed = 60f;

        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private Transform visualRoot;

        private ObjectPool objectPool;
        private RunDirector runDirector;
        private PlayerController player;
        private Vector2 currentVelocity;
        private float age;
        private bool collected;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (body != null)
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
            }

            triggerCollider.isTrigger = true;
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            age += Time.deltaTime;
            if (age >= maxLifetime)
            {
                Despawn();
                return;
            }

            if (visualRoot != null && !Mathf.Approximately(idleSpinSpeed, 0f))
            {
                visualRoot.Rotate(0f, 0f, idleSpinSpeed * Time.deltaTime);
            }

            if (player == null && runDirector != null)
            {
                player = runDirector.Player;
            }
        }

        private void FixedUpdate()
        {
            if (collected)
            {
                return;
            }

            if (player == null)
            {
                ApplyMotion(Vector2.zero, idleDrag, Time.fixedDeltaTime);
                return;
            }

            var targetPosition = player.MagnetTarget.position;
            var currentPosition = body != null ? body.position : (Vector2)transform.position;
            var toTarget = (Vector2)targetPosition - currentPosition;
            var distance = toTarget.magnitude;

            if (distance <= collectDistance)
            {
                Collect();
                return;
            }

            var shouldMagnetize = distance <= player.MagnetRadius;
            var desiredVelocity = shouldMagnetize && distance > 0.001f
                ? toTarget.normalized * maxMagnetSpeed
                : Vector2.zero;
            var acceleration = shouldMagnetize ? magnetAcceleration : idleDrag;

            ApplyMotion(desiredVelocity, acceleration, Time.fixedDeltaTime);
        }

        public void Configure(PickupType type, int rewardAmount, string runtimePoolKey = null)
        {
            pickupType = type;
            amount = Mathf.Max(1, rewardAmount);

            if (!string.IsNullOrWhiteSpace(runtimePoolKey))
            {
                poolKey = runtimePoolKey;
            }
        }

        public void OnSpawned()
        {
            objectPool = ServiceLocator.TryGet<ObjectPool>(out var sharedPool) ? sharedPool : null;
            runDirector = ServiceLocator.TryGet<RunDirector>(out var director) ? director : null;
            player = runDirector != null ? runDirector.Player : null;

            age = 0f;
            collected = false;
            currentVelocity = Vector2.zero;

            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        public void OnDespawned()
        {
            currentVelocity = Vector2.zero;
            age = 0f;
            collected = false;

            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController>() != null)
            {
                Collect();
            }
        }

        private void ApplyMotion(Vector2 desiredVelocity, float acceleration, float deltaTime)
        {
            currentVelocity = Vector2.MoveTowards(currentVelocity, desiredVelocity, acceleration * deltaTime);

            if (body != null)
            {
                body.MovePosition(body.position + currentVelocity * deltaTime);
            }
            else
            {
                transform.position += (Vector3)(currentVelocity * deltaTime);
            }
        }

        private void Collect()
        {
            if (collected)
            {
                return;
            }

            collected = true;

            if (runDirector != null)
            {
                switch (pickupType)
                {
                    case PickupType.Coin:
                        runDirector.AddCoins(amount);
                        break;

                    case PickupType.Experience:
                        runDirector.AddExperience(amount);
                        break;
                }
            }

            Despawn();
        }

        private void Despawn()
        {
            if (objectPool != null)
            {
                objectPool.Despawn(poolKey, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
