using UnityEngine;

namespace ForestRunner
{
    /// <summary>World pickup for a power-up. Floats and rotates; activated by the player on touch.</summary>
    public class PowerUpPickup : MonoBehaviour, IPoolable
    {
        public PowerUpType type;
        public Transform visual;
        Vector3 baseLocal;
        bool taken;
        float seed;

        public void OnPoolGet()
        {
            taken = false;
            seed = Random.value * 6f;
            GetComponent<Collider>().enabled = true;
            enabled = true;
        }

        public void OnPoolRelease()
        {
            enabled = false;
        }

        void Update()
        {
            if (visual == null) return;
            float t = Time.time + seed;
            visual.localRotation = Quaternion.Euler(0f, t * 120f, 0f);
            visual.localPosition = new Vector3(0f, Mathf.Sin(t * 3f) * 0.18f, 0f);
        }

        public void Collect()
        {
            if (taken) return;
            taken = true;
            GetComponent<Collider>().enabled = false;
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.Activate(type);
            if (ParticleManager.Instance != null)
                ParticleManager.Instance.Play(FxType.PowerUpPickup, transform.position + Vector3.up * 0.5f, ConfigDatabase.GetPowerUp(type).color);
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.PowerUp, 0.9f, 1f);
            if (ObjectPooler.Instance != null) ObjectPooler.Instance.Release(gameObject);
        }
    }
}
