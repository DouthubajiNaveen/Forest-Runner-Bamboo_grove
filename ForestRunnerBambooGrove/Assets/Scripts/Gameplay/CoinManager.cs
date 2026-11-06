using System.Collections.Generic;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Tracks all active coins. A single Update spins them (instead of one Update per coin),
    /// pulls them towards the player while the Gold Magnet is active and plays the collect animation.
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        public static CoinManager Instance { get; private set; }

        public float magnetRadius = 10f;
        public float magnetSpeed = 30f;

        readonly List<Coin> active = new List<Coin>(160);
        readonly List<Coin> collecting = new List<Coin>(16);

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Register(Coin c)
        {
            if (!active.Contains(c)) active.Add(c);
        }

        public void Unregister(Coin c)
        {
            active.Remove(c);
            collecting.Remove(c);
        }

        public void Collect(Coin c)
        {
            if (c == null || c.collected) return;
            c.collected = true;
            c.collectTimer = 0f;
            c.DisableCollider();
            active.Remove(c);
            collecting.Add(c);

            int value = 1;
            if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.DoubleCoins)) value = 2;
            if (GameManager.Instance != null) GameManager.Instance.AddCoins(value);
            if (ParticleManager.Instance != null) ParticleManager.Instance.Play(FxType.CoinSparkle, c.transform.position);
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Coin, 0.7f, 0.95f + Random.value * 0.2f);
        }

        void Update()
        {
            float t = Time.time;
            Quaternion spin = Quaternion.Euler(0f, t * 220f, 0f);
            for (int i = 0; i < active.Count; i++)
            {
                Coin c = active[i];
                if (c.visual != null) c.visual.localRotation = spin;
            }

            PlayerController player = PlayerController.Instance;
            if (player != null && !player.IsDead && PowerUpManager.Instance != null && PowerUpManager.Instance.IsActive(PowerUpType.Magnet))
            {
                Vector3 target = player.transform.position + Vector3.up * 1f;
                float r2 = magnetRadius * magnetRadius;
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    Coin c = active[i];
                    Vector3 delta = target - c.transform.position;
                    float d2 = delta.sqrMagnitude;
                    if (d2 > r2) continue;
                    if (d2 < 0.6f) { Collect(c); continue; }
                    float speed = magnetSpeed * (1.2f - Mathf.Sqrt(d2) / magnetRadius * 0.5f);
                    c.transform.position += delta.normalized * speed * Time.deltaTime;
                }
            }

            for (int i = collecting.Count - 1; i >= 0; i--)
            {
                Coin c = collecting[i];
                c.collectTimer += Time.deltaTime;
                float k = c.collectTimer / 0.3f;
                c.transform.position += Vector3.up * (6f * Time.deltaTime);
                if (c.visual != null) c.visual.localScale = Vector3.one * Mathf.Max(0f, 1.25f - k * 1.25f);
                if (k >= 1f)
                {
                    collecting.RemoveAt(i);
                    if (ObjectPooler.Instance != null) ObjectPooler.Instance.Release(c.gameObject);
                }
            }
        }
    }
}
