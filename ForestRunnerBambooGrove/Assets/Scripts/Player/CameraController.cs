using UnityEngine;

namespace ForestRunner
{
    /// <summary>Smooth third-person follow camera with shake, speed FOV and power-up effects.</summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        public Vector3 offset = new Vector3(0f, 4.3f, -7.6f);
        public float lookAhead = 7f;
        public float baseFov = 62f;

        Transform target;
        Camera cam;
        float camX, camXVel, camY, shakeAmp, shakeTime, shakeDur;
        ParticleSystem speedLines;
        bool lastBoost;

        public static CameraController Create(MaterialLibrary mats)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera c = go.AddComponent<Camera>();
            c.nearClipPlane = 0.3f;
            c.farClipPlane = 520f;
            c.fieldOfView = 62f;
            c.clearFlags = CameraClearFlags.SolidColor;
            go.AddComponent<AudioListener>();
            CameraController cc = go.AddComponent<CameraController>();
            cc.cam = c;
            cc.speedLines = ParticleManager.CreateSpeedLines(go.transform, mats);
            return cc;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Camera Cam { get { return cam; } }

        public void Init(Transform t)
        {
            target = t;
            camX = t.position.x;
            camY = offset.y;
            transform.position = new Vector3(camX, camY, t.position.z + offset.z);
            transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.5f, lookAhead) - new Vector3(0f, camY, offset.z));
        }

        public void AddShake(float amplitude, float duration)
        {
            if (amplitude >= shakeAmp || shakeTime <= 0f)
            {
                shakeAmp = amplitude;
                shakeDur = duration;
                shakeTime = duration;
            }
        }

        public void ShiftZ(float dz)
        {
            transform.position -= new Vector3(0f, 0f, dz);
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            Vector3 tp = target.position;
            PowerUpManager pm = PowerUpManager.Instance;
            GameManager gm = GameManager.Instance;

            bool boost = pm != null && pm.IsActive(PowerUpType.SpeedBoost);
            bool hover = pm != null && pm.IsActive(PowerUpType.Hover);
            float speedT = gm != null && gm.Level != null ? Mathf.InverseLerp(gm.Level.baseSpeed, gm.Level.maxSpeed, gm.BaseSpeed) : 0f;

            camX = Mathf.SmoothDamp(camX, tp.x * 0.75f, ref camXVel, 0.18f, Mathf.Infinity, dt);
            float ty = Mathf.Clamp(tp.y, -0.5f, 4f);
            float desiredY = offset.y + ty * 0.45f + (hover ? 0.8f : 0f);
            camY = Mathf.Lerp(camY, desiredY, 1f - Mathf.Exp(-dt * 6f));
            float back = offset.z - speedT * 0.9f - (boost ? 0.9f : 0f);

            Vector3 pos = new Vector3(camX, camY, tp.z + back);
            Vector3 look = new Vector3(tp.x * 0.9f, 1.4f + ty * 0.35f, tp.z + lookAhead);

            if (shakeTime > 0f)
            {
                shakeTime -= dt;
                float f = Mathf.Clamp01(shakeTime / Mathf.Max(0.01f, shakeDur));
                float t = Time.unscaledTime * 45f;
                Vector3 s = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (2f * shakeAmp * f);
                pos += s;
                look += s * 0.5f;
                if (shakeTime <= 0f) shakeAmp = 0f;
            }

            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos);

            float targetFov = baseFov + speedT * 8f + (boost ? 14f : 0f) + (hover ? 4f : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-dt * 4f));

            if (boost != lastBoost)
            {
                lastBoost = boost;
                ParticleManager.SetEmitting(speedLines, boost);
            }
        }
    }
}
