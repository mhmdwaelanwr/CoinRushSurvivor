using CoinRushSurvivor.Core;
using CoinRushSurvivor.Gameplay;
using UnityEngine;

namespace CoinRushSurvivor.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyController : MonoBehaviour, IPooledObject
    {
        [Header("Pool")]
        [SerializeField] private string poolKey = "Enemy";

        [Header("Stats")]
        [SerializeField, Min(0.5f)] private float baseMoveSpeed = 2.25f;
        [SerializeField, Min(0.5f)] private float baseMaxHealth = 3f;
        [SerializeField, Min(0.5f)] private float baseContactDamage = 1f;
        [SerializeField, Min(0.1f)] private float contactDamageCooldown = 0.75f;
        [SerializeField, Min(0)] private int scoreValue = 10;

        [Header("Drops")]
        [SerializeField, Min(0)] private int coinDropCount = 1;
        [SerializeField, Min(0)] private int experienceDropCount = 1;
        [SerializeField, Min(0f)] private float dropScatterRadius = 0.45f;
        [SerializeField] private string coinPickupPoolKey = "CoinPickup";
        [SerializeField] private string experiencePickupPoolKey = "ExperiencePickup";

        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Health health;

        private ObjectPool objectPool;
        private RunDirector runDirector;
        private Transform target;
        private float currentMoveSpeed;
        private float currentContactDamage;
        private float nextContactDamageTime;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;

            health.Died += HandleDeath;
        }

        private void FixedUpdate()
        {
            if (health.IsDead || target == null)
            {
                return;
            }

            if (runDirector != null && !runDirector.IsRunActive)
            {
                return;
            }

            var direction = ((Vector2)target.position - body.position).normalized;
            var nextPosition = body.position + direction * currentMoveSpeed * Time.fixedDeltaTime;
            body.MovePosition(nextPosition);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        public void Configure(Transform targetTransform, ObjectPool sourcePool, string assignedPoolKey, float difficultyScalar)
        {
            target = targetTransform;
            objectPool = sourcePool;
            runDirector = ServiceLocator.TryGet<RunDirector>(out var director) ? director : null;

            if (!string.IsNullOrWhiteSpace(assignedPoolKey))
            {
                poolKey = assignedPoolKey;
            }

            var difficulty = Mathf.Max(1f, difficultyScalar);

            currentMoveSpeed = baseMoveSpeed * (1f + (difficulty - 1f) * 0.4f);
            currentContactDamage = baseContactDamage * (1f + (difficulty - 1f) * 0.5f);

            var scaledMaxHealth = baseMaxHealth * (1f + (difficulty - 1f) * 0.75f);
            health.SetMaxHealth(scaledMaxHealth, true);

            nextContactDamageTime = 0f;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public void OnSpawned()
        {
            nextContactDamageTime = 0f;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        public void OnDespawned()
        {
            target = null;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDealContactDamage(collision.collider);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDealContactDamage(other);
        }

        private void TryDealContactDamage(Component other)
        {
            if (Time.time < nextContactDamageTime || health.IsDead)
            {
                return;
            }

            var otherHealth = other.GetComponentInParent<Health>();
            if (otherHealth == null || otherHealth == health || otherHealth.IsDead)
            {
                return;
            }

            if (otherHealth.Damage(currentContactDamage, gameObject))
            {
                nextContactDamageTime = Time.time + contactDamageCooldown;
            }
        }

        private void HandleDeath(Health targetHealth, GameObject source)
        {
            if (runDirector != null)
            {
                runDirector.AddScore(scoreValue);
            }

            SpawnDrops();

            if (objectPool != null)
            {
                objectPool.Despawn(poolKey, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void SpawnDrops()
        {
            if (objectPool == null)
            {
                return;
            }

            SpawnPickupBurst(coinPickupPoolKey, coinDropCount);
            SpawnPickupBurst(experiencePickupPoolKey, experienceDropCount);
        }

        private void SpawnPickupBurst(string pickupPoolKey, int amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(pickupPoolKey))
            {
                return;
            }

            for (var i = 0; i < amount; i++)
            {
                var offset = Random.insideUnitCircle * dropScatterRadius;
                var spawnPosition = (Vector2)transform.position + offset;
                objectPool.Spawn(pickupPoolKey, spawnPosition, Quaternion.identity);
            }
        }
    }
}
