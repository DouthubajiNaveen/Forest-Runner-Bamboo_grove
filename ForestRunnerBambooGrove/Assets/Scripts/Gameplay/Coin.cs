using UnityEngine;

namespace ForestRunner
{
    /// <summary>A collectible coin. Spinning / magnet / collection animation is driven by CoinManager.</summary>
    public class Coin : MonoBehaviour, IPoolable
    {
        public Transform visual;
        [HideInInspector] public bool collected;
        [HideInInspector] public float collectTimer;
        Collider col;

        void Awake()
        {
            col = GetComponent<Collider>();
        }

        public void OnPoolGet()
        {
            collected = false;
            collectTimer = 0f;
            if (col == null) col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            if (visual != null) visual.localScale = Vector3.one;
            if (CoinManager.Instance != null) CoinManager.Instance.Register(this);
        }

        public void OnPoolRelease()
        {
            if (CoinManager.Instance != null) CoinManager.Instance.Unregister(this);
        }

        public void DisableCollider()
        {
            if (col == null) col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}
