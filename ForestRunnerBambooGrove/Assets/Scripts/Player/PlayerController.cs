using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Third-person runner controller: automatic forward motion, 3-lane switching, jump, slide,
    /// hover, ground / pit / river detection, obstacle & collectible handling, power-up visuals.
    /// Kinematic Rigidbody + BoxCollider interacting with trigger colliders on the Obstacle /
    /// Collectible layers.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        CharacterVisual visual;
        BoxCollider box;
        MaterialLibrary mats;

        int lane = 1;
        float x, y, z, xVel, vy;
        bool grounded = true, sliding, queueSlide, dead, hovering, colliderSliding, splashed, running;
        float slideTimer, jumpBuffer, invuln, dustTimer, knockVel;
        DeathCause deathCause;

        GameObject shieldBubble, hoverGlow, magnetRing;
        ParticleSystem boostTrail;

        public bool IsDead { get { return dead; } }
        public bool IsHovering { get { return hovering; } }

        public static PlayerController Create(SkinConfig skin, MaterialLibrary materials)
        {
            GameObject go = new GameObject("Player");
            go.layer = GameConstants.LayerPlayer;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.8f, 1.7f, 0.6f);
            bc.center = new Vector3(0f, 0.85f, 0f);
            PlayerController pc = go.AddComponent<PlayerController>();
            pc.box = bc;
            pc.mats = materials;
            pc.visual = CharacterVisual.Create(go.transform);
            pc.visual.ApplySkin(skin);
            pc.BuildEffects();
            return pc;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.OnChanged -= OnPowerUpChanged;
        }

        void Start()
        {
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.OnChanged += OnPowerUpChanged;
        }

        void BuildEffects()
        {
            shieldBubble = new GameObject("ShieldBubble");
            shieldBubble.transform.SetParent(transform, false);
            shieldBubble.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            shieldBubble.transform.localScale = Vector3.one * 2.4f;
            shieldBubble.AddComponent<MeshFilter>().sharedMesh = ProcMesh.Sphere;
            MeshRenderer sr = shieldBubble.AddComponent<MeshRenderer>();
            sr.sharedMaterial = mats.shieldBubble;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shieldBubble.SetActive(false);

            hoverGlow = new GameObject("HoverGlow");
            hoverGlow.transform.SetParent(transform, false);
            hoverGlow.transform.localScale = Vector3.one * 2.8f;
            hoverGlow.AddComponent<MeshFilter>().sharedMesh = ProcMesh.Quad;
            MeshRenderer hr = hoverGlow.AddComponent<MeshRenderer>();
            hr.sharedMaterial = mats.hoverGlow;
            hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hoverGlow.SetActive(false);

            magnetRing = new GameObject("MagnetRing");
            magnetRing.transform.SetParent(transform, false);
            magnetRing.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            magnetRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            magnetRing.transform.localScale = Vector3.one * 3.6f;
            magnetRing.AddComponent<MeshFilter>().sharedMesh = ProcMesh.Quad;
            MeshRenderer mr = magnetRing.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mats.magnetRing;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            magnetRing.SetActive(false);

            boostTrail = ParticleManager.CreateLoop("BoostTrail", transform, mats.particleAdd,
                new Color(1f, 0.6f, 0.15f, 0.8f), 0.45f, 0.5f, 0f, 70f, true, 0f);
            ParticleSystem.ShapeModule sh = boostTrail.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(0.6f, 1.2f, 0.3f);
            sh.position = new Vector3(0f, 0.9f, -0.4f);
            ParticleSystem.MainModule bm = boostTrail.main;
            bm.startSpeed = 0f;
            ParticleManager.SetEmitting(boostTrail, false);
        }

        void OnPowerUpChanged(PowerUpType t, bool active)
        {
            switch (t)
            {
                case PowerUpType.Shield:
                    shieldBubble.SetActive(active);
                    if (!active && !dead && ParticleManager.Instance != null)
                        ParticleManager.Instance.Play(FxType.ShieldBreak, transform.position + Vector3.up);
                    break;
                case PowerUpType.Magnet: magnetRing.SetActive(active); break;
                case PowerUpType.SpeedBoost: ParticleManager.SetEmitting(boostTrail, active); break;
            }
        }

        public void ShiftZ(float dz)
        {
            z -= dz;
            transform.position = new Vector3(x, y, z);
        }

        bool GroundBelow(out bool water)
        {
            water = false;
            if (EnvironmentSpawner.Instance == null) return true;
            return EnvironmentSpawner.Instance.GroundAt(x, z, out water);
        }

        void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            float dt = Time.deltaTime;

            if (gm.State == GameState.Playing && !dead)
            {
                HandleInput(dt);
                Move(dt, gm.CurrentSpeed);
            }
            else if (dead)
            {
                DeathMotion(dt);
            }

            transform.position = new Vector3(x, y, z);

            if (invuln > 0f) invuln -= dt;
            if (shieldBubble.activeSelf)
                shieldBubble.transform.localScale = Vector3.one * (2.4f + Mathf.Sin(Time.time * 6f) * 0.08f);
            if (magnetRing.activeSelf)
            {
                magnetRing.transform.Rotate(0f, 0f, 200f * dt, Space.Self);
                magnetRing.transform.localScale = Vector3.one * (3.4f + Mathf.Sin(Time.time * 5f) * 0.4f);
            }
            if (hoverGlow.activeSelf != (hovering && !dead)) hoverGlow.SetActive(hovering && !dead);
            if (hoverGlow.activeSelf)
            {
                hoverGlow.transform.position = new Vector3(x, 0.06f, z);
                hoverGlow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            bool wantRun = gm.State == GameState.Playing && !dead && grounded && !hovering;
            if (wantRun != running)
            {
                running = wantRun;
                if (AudioManager.Instance != null) AudioManager.Instance.SetRunning(running && !sliding, gm.CurrentSpeed);
            }
            else if (running && AudioManager.Instance != null)
            {
                AudioManager.Instance.SetRunning(!sliding, gm.CurrentSpeed);
            }

            AnimState a = AnimState.Run;
            if (dead) a = AnimState.Dead;
            else if (hovering) a = AnimState.Hover;
            else if (sliding) a = AnimState.Slide;
            else if (!grounded) a = AnimState.Jump;
            visual.SetState(a, dead && deathCause != DeathCause.Obstacle);
            visual.SetRunSpeed(gm.BaseSpeed);
            visual.SetSteer(GameConstants.LaneX(lane) - x);
        }

        void HandleInput(float dt)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ChangeLane(-1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(1);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) jumpBuffer = 0.15f;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) StartSlide();

            if (jumpBuffer > 0f)
            {
                jumpBuffer -= dt;
                if (grounded && !hovering) Jump();
            }
        }

        void ChangeLane(int dir)
        {
            int n = Mathf.Clamp(lane + dir, 0, GameConstants.LaneCount - 1);
            if (n == lane) return;
            lane = n;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Swish, 0.5f, 1f + dir * 0.05f);
        }

        void Jump()
        {
            vy = GameConstants.JumpVelocity;
            grounded = false;
            sliding = false;
            jumpBuffer = 0f;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Jump, 0.7f, 1f);
            if (ParticleManager.Instance != null) ParticleManager.Instance.Play(FxType.Dust, transform.position);
        }

        void StartSlide()
        {
            if (hovering) return;
            if (!grounded)
            {
                vy = Mathf.Min(vy, -18f);
                queueSlide = true;
                return;
            }
            sliding = true;
            slideTimer = GameConstants.SlideDuration;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Slide, 0.6f, 1f);
        }

        void Land()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Land, 0.5f, 1f);
            if (ParticleManager.Instance != null) ParticleManager.Instance.Play(FxType.Dust, transform.position);
            if (queueSlide) { queueSlide = false; sliding = true; slideTimer = GameConstants.SlideDuration; }
        }

        void Move(float dt, float speed)
        {
            z += speed * dt;
            x = Mathf.SmoothDamp(x, GameConstants.LaneX(lane), ref xVel, 0.07f, Mathf.Infinity, dt);

            PowerUpManager pm = PowerUpManager.Instance;
            bool hoverWanted = pm != null && pm.IsActive(PowerUpType.Hover);
            bool water;
            if (hoverWanted) { hovering = true; sliding = false; queueSlide = false; }
            else if (hovering && GroundBelow(out water)) hovering = false;

            if (hovering)
            {
                y = Mathf.MoveTowards(y, GameConstants.HoverHeight, 12f * dt);
                vy = 0f;
                grounded = false;
            }
            else
            {
                vy += GameConstants.Gravity * dt;
                y += vy * dt;
                if (y <= 0f && vy <= 0f)
                {
                    if (GroundBelow(out water))
                    {
                        if (!grounded && y > -0.6f) Land();
                        y = 0f; vy = 0f; grounded = true; splashed = false;
                    }
                    else
                    {
                        grounded = false;
                        sliding = false;
                        if (water && y < -0.3f && !splashed)
                        {
                            splashed = true;
                            if (ParticleManager.Instance != null) ParticleManager.Instance.Play(FxType.Splash, new Vector3(x, -0.2f, z));
                            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Splash, 1f, 1f);
                        }
                        if (y < -1.4f) Die(water ? DeathCause.Drown : DeathCause.Fall);
                    }
                }
                else grounded = false;
            }

            if (sliding)
            {
                slideTimer -= dt;
                if (slideTimer <= 0f || !grounded) sliding = false;
            }
            if (sliding != colliderSliding)
            {
                colliderSliding = sliding;
                box.size = sliding ? new Vector3(0.8f, 0.7f, 0.6f) : new Vector3(0.8f, 1.7f, 0.6f);
                box.center = sliding ? new Vector3(0f, 0.35f, 0f) : new Vector3(0f, 0.85f, 0f);
            }

            dustTimer -= dt;
            if (grounded && !sliding && dustTimer <= 0f)
            {
                dustTimer = 0.2f;
                if (ParticleManager.Instance != null) ParticleManager.Instance.Play(FxType.Dust, transform.position + Vector3.back * 0.2f);
            }
        }

        void DeathMotion(float dt)
        {
            if (deathCause == DeathCause.Obstacle)
            {
                vy += GameConstants.Gravity * dt;
                y = Mathf.Max(0f, y + vy * dt);
                if (y <= 0f) vy = 0f;
                z -= knockVel * dt;
                knockVel = Mathf.MoveTowards(knockVel, 0f, 14f * dt);
            }
            else
            {
                vy += GameConstants.Gravity * dt;
                if (deathCause == DeathCause.Drown) vy = Mathf.Max(vy, -3f);
                y += vy * dt;
            }
        }

        void Die(DeathCause cause)
        {
            if (dead) return;
            dead = true;
            deathCause = cause;
            sliding = false;
            hovering = false;
            knockVel = 6f;
            vy = cause == DeathCause.Obstacle ? 4.5f : Mathf.Min(vy, 0f);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetRunning(false, 0f);
                AudioManager.Instance.Play(cause == DeathCause.Drown ? Sfx.Splash : Sfx.Hit, 1f, 1f);
            }
            if (ParticleManager.Instance != null) ParticleManager.Instance.Play(FxType.Collision, transform.position + Vector3.up);
            if (CameraController.Instance != null) CameraController.Instance.AddShake(cause == DeathCause.Obstacle ? 0.7f : 0.3f, 0.5f);
            if (PowerUpManager.Instance != null) PowerUpManager.Instance.ClearAll();
            ParticleManager.SetEmitting(boostTrail, false);
            if (UIManager.Instance != null) UIManager.Instance.Flash(new Color(1f, 0.1f, 0.05f, 0.45f));
            if (GameManager.Instance != null) GameManager.Instance.PlayerDied(cause);
        }

        void OnTriggerEnter(Collider other)
        {
            if (dead) return;
            Coin coin;
            if (other.TryGetComponent<Coin>(out coin))
            {
                if (CoinManager.Instance != null) CoinManager.Instance.Collect(coin);
                return;
            }
            PowerUpPickup pu;
            if (other.TryGetComponent<PowerUpPickup>(out pu))
            {
                pu.Collect();
                return;
            }
            Obstacle o = other.GetComponentInParent<Obstacle>();
            if (o != null) HandleObstacle(o);
        }

        void HandleObstacle(Obstacle o)
        {
            if (o.smashed) return;
            PowerUpManager pm = PowerUpManager.Instance;
            if (pm != null && pm.IsActive(PowerUpType.SpeedBoost))
            {
                o.Smash();
                if (CameraController.Instance != null) CameraController.Instance.AddShake(0.25f, 0.2f);
                return;
            }
            if (hovering && o.hoverable) return;
            if (invuln > 0f) return;
            if (pm != null && pm.ConsumeShield())
            {
                o.Smash();
                invuln = 1.5f;
                if (CameraController.Instance != null) CameraController.Instance.AddShake(0.4f, 0.3f);
                return;
            }
            Die(DeathCause.Obstacle);
        }
    }
}
