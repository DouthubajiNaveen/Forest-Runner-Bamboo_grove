using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Obstacle behaviour: stores how it can be avoided, can be smashed (shield / speed boost)
    /// and, for boulders, rolls towards the player. Update is only enabled for rolling boulders.
    /// </summary>
    public class Obstacle : MonoBehaviour, IPoolable
    {
        public ObstacleType type;
        /// <summary>True if the Hover power-up lets the player fly over this obstacle.</summary>
        public bool hoverable = true;
        public float rollSpeed = 9f;
        public Transform rollVisual;

        Renderer[] renderers;
        Collider[] colliders;
        [HideInInspector] public bool smashed;
        bool rolling;

        void CacheComponents()
        {
            if (renderers == null) renderers = GetComponentsInChildren<Renderer>(true);
            if (colliders == null) colliders = GetComponentsInChildren<Collider>(true);
        }

        public void OnPoolGet()
        {
            CacheComponents();
            smashed = false;
            rolling = false;
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = true;
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = true;
            enabled = (type == ObstacleType.Boulder);
        }

        public void OnPoolRelease()
        {
            enabled = false;
        }

        public void Smash()
        {
            if (smashed) return;
            CacheComponents();
            smashed = true;
            for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
            if (ParticleManager.Instance != null) ParticleManager.Instance.Play(FxType.Smash, transform.position + Vector3.up);
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.ShieldBreak, 0.8f, 0.8f);
        }

        void Update()
        {
            if (smashed) return;
            PlayerController p = PlayerController.Instance;
            if (p == null) return;
            float dz = transform.position.z - p.transform.position.z;
            if (!rolling && dz < 55f && dz > 0f) rolling = true;
            if (rolling)
            {
                float dt = Time.deltaTime;
                transform.position += new Vector3(0f, 0f, -rollSpeed * dt);
                if (rollVisual != null) rollVisual.Rotate(-rollSpeed * dt * 70f, 0f, 0f, Space.Self);
            }
        }
    }
}
