using System;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Owns all power-up timers. Other systems query IsActive(type); the HUD reads Fraction(type).
    /// Durations come from PowerUpConfig + the upgrade levels bought in the shop.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        public const int TypeCount = 5;
        readonly float[] remaining = new float[TypeCount];
        readonly float[] total = new float[TypeCount];
        int activeCount;

        /// <summary>Raised when a power-up starts (true) or ends (false).</summary>
        public event Action<PowerUpType, bool> OnChanged;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsActive(PowerUpType t)
        {
            return remaining[(int)t] > 0f;
        }

        public float Remaining(PowerUpType t)
        {
            return Mathf.Max(0f, remaining[(int)t]);
        }

        public float Fraction(PowerUpType t)
        {
            int i = (int)t;
            return total[i] > 0f ? Mathf.Clamp01(remaining[i] / total[i]) : 0f;
        }

        public void Activate(PowerUpType t)
        {
            int i = (int)t;
            PowerUpConfig cfg = ConfigDatabase.GetPowerUp(t);
            float d = cfg.GetDuration(SaveSystem.GetUpgradeLevel(t));
            bool was = remaining[i] > 0f;
            remaining[i] = d;
            total[i] = d;
            if (!was) activeCount++;
            if (OnChanged != null) OnChanged(t, true);
        }

        public void Deactivate(PowerUpType t)
        {
            int i = (int)t;
            if (remaining[i] <= 0f) return;
            remaining[i] = 0f;
            activeCount = Mathf.Max(0, activeCount - 1);
            if (OnChanged != null) OnChanged(t, false);
        }

        public bool ConsumeShield()
        {
            if (!IsActive(PowerUpType.Shield)) return false;
            Deactivate(PowerUpType.Shield);
            return true;
        }

        public void ClearAll()
        {
            for (int i = 0; i < TypeCount; i++) Deactivate((PowerUpType)i);
        }

        void Update()
        {
            if (activeCount == 0) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < TypeCount; i++)
            {
                if (remaining[i] <= 0f) continue;
                remaining[i] -= dt;
                if (remaining[i] <= 0f)
                {
                    remaining[i] = 0.0001f; // make Deactivate see it as active
                    Deactivate((PowerUpType)i);
                }
            }
        }
    }
}
