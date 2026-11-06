using UnityEngine;
using UnityEngine.Rendering;

namespace ForestRunner
{
    public enum FxType { CoinSparkle = 0, PowerUpPickup = 1, Collision = 2, ShieldBreak = 3, Splash = 4, Dust = 5, Smash = 6 }

    /// <summary>
    /// Burst particle effects through two shared world-space particle systems (additive and
    /// alpha-blended) so there are no per-effect allocations. Also hosts factory helpers for
    /// continuous particle systems (speed lines, ambient pollen / fireflies, power-up trails).
    /// </summary>
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        struct FxDef
        {
            public Color color; public int count; public float speed; public float size;
            public float life; public bool additive; public float spread; public bool upBias;
        }

        FxDef[] defs;
        ParticleSystem addPS, alphaPS;

        public static ParticleManager Create(MaterialLibrary mats)
        {
            GameObject go = new GameObject("ParticleManager");
            ParticleManager pm = go.AddComponent<ParticleManager>();
            pm.Init(mats);
            return pm;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static FxDef D(Color c, int count, float speed, float size, float life, bool add, float spread, bool up)
        {
            FxDef d = new FxDef();
            d.color = c; d.count = count; d.speed = speed; d.size = size; d.life = life;
            d.additive = add; d.spread = spread; d.upBias = up;
            return d;
        }

        void Init(MaterialLibrary mats)
        {
            Instance = this;
            defs = new FxDef[7];
            defs[(int)FxType.CoinSparkle] = D(new Color(1f, 0.85f, 0.25f, 1f), 8, 3.5f, 0.28f, 0.55f, true, 0.3f, true);
            defs[(int)FxType.PowerUpPickup] = D(Color.white, 26, 6f, 0.4f, 0.9f, true, 0.6f, false);
            defs[(int)FxType.Collision] = D(new Color(1f, 0.55f, 0.2f, 1f), 30, 7f, 0.45f, 0.7f, true, 0.5f, true);
            defs[(int)FxType.ShieldBreak] = D(new Color(0.4f, 0.7f, 1f, 1f), 36, 8f, 0.4f, 0.8f, true, 0.8f, false);
            defs[(int)FxType.Splash] = D(new Color(0.75f, 0.9f, 1f, 1f), 28, 5.5f, 0.35f, 0.9f, false, 0.4f, true);
            defs[(int)FxType.Dust] = D(new Color(0.75f, 0.68f, 0.52f, 0.6f), 4, 1.4f, 0.55f, 0.6f, false, 0.2f, true);
            defs[(int)FxType.Smash] = D(new Color(0.65f, 0.5f, 0.35f, 0.9f), 22, 6f, 0.5f, 0.8f, false, 0.7f, true);

            addPS = CreateBurstSystem("BurstAdditive", transform, mats.particleAdd, 0.35f);
            alphaPS = CreateBurstSystem("BurstAlpha", transform, mats.particleAlpha, 1.0f);
        }

        static ParticleSystem CreateBurstSystem(string name, Transform parent, Material mat, float gravity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            main.gravityModifier = gravity;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 0.3f;
            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = false;
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = false;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient();
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        static Gradient FadeGradient()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        public void Play(FxType t, Vector3 pos)
        {
            Play(t, pos, new Color(0f, 0f, 0f, 0f));
        }

        public void Play(FxType t, Vector3 pos, Color tint)
        {
            if (defs == null) return;
            FxDef d = defs[(int)t];
            Color c = tint.a > 0.01f ? tint : d.color;
            ParticleSystem ps = d.additive ? addPS : alphaPS;
            if (ps == null) return;
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < d.count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                if (d.upBias) dir.y = Mathf.Abs(dir.y) + 0.4f;
                ep.position = pos + dir * (d.spread * 0.3f);
                ep.velocity = dir * (d.speed * Random.Range(0.5f, 1.2f));
                ep.startLifetime = d.life * Random.Range(0.7f, 1.2f);
                ep.startSize = d.size * Random.Range(0.6f, 1.3f);
                ep.startColor = c;
                ps.Emit(ep, 1);
            }
        }

        // ------------------------------------------------------------------
        // Continuous effect helpers
        // ------------------------------------------------------------------

        public static ParticleSystem CreateLoop(string name, Transform parent, Material mat, Color color,
            float size, float life, float speed, float rate, bool worldSpace, float gravity)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = 600;
            main.gravityModifier = gravity;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rate;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = FadeGradient();
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        public static void SetEmitting(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule em = ps.emission;
            em.enabled = on;
            if (on && !ps.isPlaying) ps.Play();
        }

        /// <summary>Drifting pollen / leaves (day) or fireflies (night) around the camera.</summary>
        public static ParticleSystem CreateAmbient(Transform cam, LevelConfig lv, MaterialLibrary mats)
        {
            Color c = lv.night ? new Color(lv.glowColor.r, lv.glowColor.g, lv.glowColor.b, 0.9f) : new Color(1f, 0.98f, 0.8f, 0.6f);
            float size = lv.night ? 0.22f : 0.12f;
            float rate = lv.night ? 22f : 14f;
            ParticleSystem ps = CreateLoop("AmbientParticles", cam, mats.particleAdd, c, size, 9f, 0.4f, rate, true, -0.01f);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(40f, 12f, 60f);
            sh.position = new Vector3(0f, 1f, 28f);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;
            // pre-fill so the effect is visible immediately
            ps.Simulate(6f, true, true);
            ps.Play();
            return ps;
        }

        /// <summary>Streaks flying past the camera; enabled during Speed Boost.</summary>
        public static ParticleSystem CreateSpeedLines(Transform cam, MaterialLibrary mats)
        {
            ParticleSystem ps = CreateLoop("SpeedLines", cam, mats.particleAdd, new Color(1f, 1f, 1f, 0.35f), 0.07f, 0.45f, -55f, 90f, false, 0f);
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(16f, 9f, 1f);
            sh.position = new Vector3(0f, 0f, 14f);
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 8f;
            r.velocityScale = 0.05f;
            SetEmitting(ps, false);
            return ps;
        }
    }
}
