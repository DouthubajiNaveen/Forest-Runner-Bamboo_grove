using UnityEngine;

namespace ForestRunner
{
    public enum AnimState { Idle, Run, Jump, Slide, Dead, Hover }

    /// <summary>
    /// Procedural placeholder explorer built from primitives, animated in code
    /// (run cycle, jump pose, slide, hover glide, death / tumble). Origin is at the feet.
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        Transform body, legL, legR, armL, armR;
        Material shirtMat, pantsMat, hatMat, skinMat, packMat;
        AnimState state = AnimState.Run;
        bool tumble;
        float phase, runSpeed = 15f;
        float steerTarget, steer;
        float cLegL, cLegR, cArmL, cArmR, cArmRollL, cArmRollR, cPitch, cBodyY = 0.85f;

        public static CharacterVisual Create(Transform parent)
        {
            GameObject go = new GameObject("Visual");
            go.transform.SetParent(parent, false);
            CharacterVisual cv = go.AddComponent<CharacterVisual>();
            cv.Build();
            return cv;
        }

        static Transform Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            GameObject g = new GameObject("part");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return g.transform;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            return g.transform;
        }

        void Build()
        {
            shirtMat = MaterialLibrary.Lit(new Color(0.8f, 0.3f, 0.2f), 0.2f, 0f);
            pantsMat = MaterialLibrary.Lit(new Color(0.25f, 0.3f, 0.45f), 0.2f, 0f);
            hatMat = MaterialLibrary.Lit(new Color(0.75f, 0.6f, 0.3f), 0.2f, 0f);
            skinMat = MaterialLibrary.Lit(new Color(0.95f, 0.76f, 0.6f), 0.3f, 0f);
            packMat = MaterialLibrary.Lit(new Color(0.4f, 0.28f, 0.16f), 0.1f, 0f);
            Material dark = MaterialLibrary.Unlit(new Color(0.05f, 0.05f, 0.05f));
            Material shoe = MaterialLibrary.Lit(new Color(0.2f, 0.14f, 0.1f), 0.1f, 0f);

            body = Pivot(transform, "Body", new Vector3(0f, 0.85f, 0f));
            Part(body, ProcMesh.Capsule, shirtMat, new Vector3(0f, 0.3f, 0f), new Vector3(0.44f, 0.3f, 0.28f));
            Part(body, ProcMesh.Sphere, skinMat, new Vector3(0f, 0.78f, 0f), new Vector3(0.3f, 0.32f, 0.3f));
            Part(body, ProcMesh.Cylinder, hatMat, new Vector3(0f, 0.93f, 0f), new Vector3(0.52f, 0.025f, 0.52f));
            Part(body, ProcMesh.Cylinder, hatMat, new Vector3(0f, 1f, 0f), new Vector3(0.27f, 0.07f, 0.27f));
            Part(body, ProcMesh.Cube, packMat, new Vector3(0f, 0.3f, -0.2f), new Vector3(0.3f, 0.4f, 0.14f));
            Part(body, ProcMesh.Sphere, dark, new Vector3(-0.07f, 0.8f, 0.13f), Vector3.one * 0.05f);
            Part(body, ProcMesh.Sphere, dark, new Vector3(0.07f, 0.8f, 0.13f), Vector3.one * 0.05f);

            legL = Pivot(body, "LegL", new Vector3(-0.12f, 0f, 0f));
            legR = Pivot(body, "LegR", new Vector3(0.12f, 0f, 0f));
            Transform[] legs = new Transform[] { legL, legR };
            for (int i = 0; i < 2; i++)
            {
                Part(legs[i], ProcMesh.Capsule, pantsMat, new Vector3(0f, -0.4f, 0f), new Vector3(0.17f, 0.4f, 0.17f));
                Part(legs[i], ProcMesh.Cube, shoe, new Vector3(0f, -0.8f, 0.05f), new Vector3(0.16f, 0.08f, 0.26f));
            }
            armL = Pivot(body, "ArmL", new Vector3(-0.3f, 0.5f, 0f));
            armR = Pivot(body, "ArmR", new Vector3(0.3f, 0.5f, 0f));
            Transform[] arms = new Transform[] { armL, armR };
            for (int i = 0; i < 2; i++)
            {
                Part(arms[i], ProcMesh.Capsule, shirtMat, new Vector3(0f, -0.27f, 0f), new Vector3(0.12f, 0.28f, 0.12f));
                Part(arms[i], ProcMesh.Sphere, skinMat, new Vector3(0f, -0.57f, 0f), Vector3.one * 0.11f);
            }
        }

        public void ApplySkin(SkinConfig s)
        {
            if (s == null || shirtMat == null) return;
            shirtMat.color = s.shirt;
            pantsMat.color = s.pants;
            hatMat.color = s.hat;
            skinMat.color = s.skinTone;
            packMat.color = Color.Lerp(s.hat, Color.black, 0.45f);
        }

        public void SetState(AnimState s, bool tumbling)
        {
            state = s;
            tumble = tumbling;
        }

        public void SetRunSpeed(float v) { runSpeed = v; }
        public void SetSteer(float dx) { steerTarget = Mathf.Clamp(dx, -2.6f, 2.6f); }

        void Update()
        {
            if (body == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float tLegL = 0f, tLegR = 0f, tArmL = 0f, tArmR = 0f, tRollL = 0f, tRollR = 0f, tPitch = 0f, tY = 0.85f;
            float k = 1f - Mathf.Exp(-dt * 22f);

            switch (state)
            {
                case AnimState.Run:
                    phase += dt * (8f + runSpeed * 0.18f);
                    float s = Mathf.Sin(phase);
                    tLegL = s * 55f; tLegR = -s * 55f;
                    tArmL = -s * 50f; tArmR = s * 50f;
                    tPitch = 9f;
                    tY = 0.85f + Mathf.Abs(Mathf.Cos(phase)) * 0.07f;
                    k = 1f - Mathf.Exp(-dt * 35f);
                    break;
                case AnimState.Jump:
                    tLegL = -45f; tLegR = 25f; tArmL = -150f; tArmR = -150f; tPitch = 6f; tRollL = -10f; tRollR = 10f;
                    break;
                case AnimState.Slide:
                    tPitch = -78f; tY = 0.3f; tLegL = 0f; tLegR = 8f; tArmL = -20f; tArmR = -20f;
                    break;
                case AnimState.Hover:
                    phase += dt * 4f;
                    tLegL = 10f + Mathf.Sin(phase) * 8f; tLegR = -6f - Mathf.Sin(phase) * 8f;
                    tArmL = 0f; tArmR = 0f; tRollL = -75f; tRollR = 75f; tPitch = 25f; tY = 0.85f;
                    break;
                case AnimState.Dead:
                    tPitch = -90f; tY = 0.22f; tLegL = 12f; tLegR = -8f; tArmL = -100f; tArmR = -70f; tRollL = -30f; tRollR = 30f;
                    break;
                default:
                    tLegL = 0f; tLegR = 0f;
                    break;
            }

            cLegL = Mathf.Lerp(cLegL, tLegL, k);
            cLegR = Mathf.Lerp(cLegR, tLegR, k);
            cArmL = Mathf.Lerp(cArmL, tArmL, k);
            cArmR = Mathf.Lerp(cArmR, tArmR, k);
            cArmRollL = Mathf.Lerp(cArmRollL, tRollL, k);
            cArmRollR = Mathf.Lerp(cArmRollR, tRollR, k);
            cPitch = Mathf.Lerp(cPitch, tPitch, k);
            cBodyY = Mathf.Lerp(cBodyY, tY, k);

            legL.localRotation = Quaternion.Euler(cLegL, 0f, 0f);
            legR.localRotation = Quaternion.Euler(cLegR, 0f, 0f);
            armL.localRotation = Quaternion.Euler(cArmL, 0f, cArmRollL);
            armR.localRotation = Quaternion.Euler(cArmR, 0f, cArmRollR);

            steer = Mathf.Lerp(steer, steerTarget, 1f - Mathf.Exp(-dt * 12f));
            if (state == AnimState.Dead && tumble)
            {
                body.Rotate(new Vector3(280f, 40f, 160f) * dt, Space.Self);
                body.localPosition = new Vector3(0f, 0.85f, 0f);
            }
            else
            {
                body.localPosition = new Vector3(0f, cBodyY, 0f);
                body.localRotation = Quaternion.Euler(cPitch, steer * 9f, -steer * 7f);
            }
        }
    }
}
