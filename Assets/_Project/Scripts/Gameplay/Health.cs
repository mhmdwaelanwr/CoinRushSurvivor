using System;
using System.Collections;
using UnityEngine;

namespace CoinRushSurvivor.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float maxHealth = 5f;
        [SerializeField] private bool resetHealthOnEnable = true;
        [SerializeField, Range(0f, 1f)] private float baseDamageBlockChance;

        [Header("Damage")]
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.35f;
        [SerializeField] private bool deactivateOnDeath;
        [SerializeField] private bool destroyOnDeath;

        private Coroutine invulnerabilityRoutine;
        private float baseMaxHealth;
        private float damageBlockChance;
        private bool initialized;

        public event Action<Health, float, float> HealthChanged;
        public event Action<Health, bool> InvulnerabilityChanged;
        public event Action<Health, GameObject> Damaged;
        public event Action<Health, GameObject> DamageBlocked;
        public event Action<Health, GameObject> Died;
        public event Action<Health> Revived;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public float DamageBlockChance => damageBlockChance;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();

            if (resetHealthOnEnable)
            {
                ResetHealth();
            }
        }

        public void ResetHealth()
        {
            baseMaxHealth = Mathf.Max(1f, baseMaxHealth);
            maxHealth = baseMaxHealth;
            damageBlockChance = Mathf.Clamp01(baseDamageBlockChance);
            CurrentHealth = maxHealth;
            IsDead = false;
            StopInvulnerability();
            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
        }

        public void SetMaxHealth(float value, bool healToFull = false)
        {
            InitializeIfNeeded();

            var previousMax = maxHealth;
            maxHealth = Mathf.Max(1f, value);

            if (IsDead)
            {
                return;
            }

            if (healToFull)
            {
                CurrentHealth = maxHealth;
            }
            else
            {
                CurrentHealth = Mathf.Clamp(CurrentHealth + (maxHealth - previousMax), 0f, maxHealth);
            }

            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
        }

        public bool Heal(float amount)
        {
            InitializeIfNeeded();

            if (IsDead || amount <= 0f)
            {
                return false;
            }

            var previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);

            if (!Mathf.Approximately(previousHealth, CurrentHealth))
            {
                HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
                return true;
            }

            return false;
        }

        public bool Damage(float amount, GameObject source = null, bool ignoreInvulnerability = false)
        {
            InitializeIfNeeded();

            if (IsDead || amount <= 0f)
            {
                return false;
            }

            if (!ignoreInvulnerability && IsInvulnerable)
            {
                return false;
            }

            if (!ignoreInvulnerability && damageBlockChance > 0f && UnityEngine.Random.value <= damageBlockChance)
            {
                if (invulnerabilityDuration > 0f)
                {
                    BeginInvulnerability();
                }

                DamageBlocked?.Invoke(this, source);
                return true;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            Damaged?.Invoke(this, source);
            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Die(source);
                return true;
            }

            if (!ignoreInvulnerability && invulnerabilityDuration > 0f)
            {
                BeginInvulnerability();
            }

            return true;
        }

        public bool Revive(float restoredHealthFraction = 0.5f)
        {
            if (restoredHealthFraction <= 0f)
            {
                restoredHealthFraction = 0.5f;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            InitializeIfNeeded();

            IsDead = false;
            StopInvulnerability();
            CurrentHealth = Mathf.Clamp(maxHealth * restoredHealthFraction, 1f, maxHealth);

            Revived?.Invoke(this);
            HealthChanged?.Invoke(this, CurrentHealth, maxHealth);
            return true;
        }

        public void AddDamageBlockChance(float amount)
        {
            InitializeIfNeeded();
            damageBlockChance = Mathf.Clamp01(damageBlockChance + amount);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            maxHealth = Mathf.Max(1f, maxHealth);
            baseMaxHealth = maxHealth;
            damageBlockChance = Mathf.Clamp01(baseDamageBlockChance);
            CurrentHealth = maxHealth;
            initialized = true;
        }

        private void BeginInvulnerability()
        {
            if (invulnerabilityRoutine != null)
            {
                StopCoroutine(invulnerabilityRoutine);
            }

            invulnerabilityRoutine = StartCoroutine(InvulnerabilityTimer());
        }

        private IEnumerator InvulnerabilityTimer()
        {
            SetInvulnerability(true);
            yield return new WaitForSeconds(invulnerabilityDuration);
            SetInvulnerability(false);
            invulnerabilityRoutine = null;
        }

        private void StopInvulnerability()
        {
            if (invulnerabilityRoutine != null)
            {
                StopCoroutine(invulnerabilityRoutine);
                invulnerabilityRoutine = null;
            }

            SetInvulnerability(false);
        }

        private void SetInvulnerability(bool value)
        {
            if (IsInvulnerable == value)
            {
                return;
            }

            IsInvulnerable = value;
            InvulnerabilityChanged?.Invoke(this, IsInvulnerable);
        }

        private void Die(GameObject source)
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            StopInvulnerability();
            CurrentHealth = 0f;

            Died?.Invoke(this, source);

            if (destroyOnDeath)
            {
                Destroy(gameObject);
                return;
            }

            if (deactivateOnDeath)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
