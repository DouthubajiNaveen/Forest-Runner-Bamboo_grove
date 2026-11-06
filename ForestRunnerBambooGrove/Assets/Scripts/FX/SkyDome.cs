using UnityEngine;

namespace ForestRunner
{
    /// <summary>Camera-following inside-out sphere that renders the procedural sky shader.</summary>
    public class SkyDome : MonoBehaviour
    {
        Transform cam;

        public static SkyDome Create(LevelConfig lv, Transform camera)
        {
            GameObject go = new GameObject("SkyDome");
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = ProcMesh.Sphere;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.transform.localScale = Vector3.one * 700f;

            Shader sh = Resources.Load<Shader>("Shaders/SkyDome");
            if (sh == null) sh = Shader.Find("ForestRunner/SkyDome");
            if (sh != null)
            {
                Material m = new Material(sh);
                m.SetColor("_Top", lv.skyTop);
                m.SetColor("_Horizon", lv.skyHorizon);
                m.SetColor("_Bottom", Color.Lerp(lv.fogColor, Color.black, 0.35f));
                m.SetColor("_HillColor", Color.Lerp(lv.fogColor, lv.leafColor, lv.night ? 0.15f : 0.35f));
                m.SetColor("_SunColor", lv.night ? new Color(0.8f, 0.88f, 1f, 1f) : Color.Lerp(lv.lightColor, Color.white, 0.5f));
                Vector3 sunDir = -(Quaternion.Euler(lv.sunPitch, lv.sunYaw, 0f) * Vector3.forward);
                m.SetVector("_SunDir", new Vector4(sunDir.x, sunDir.y, sunDir.z, 0f));
                m.SetFloat("_SunSize", lv.night ? 0.9985f : 0.9965f);
                m.SetFloat("_SunGlow", lv.night ? 0.25f : 0.7f);
                m.SetFloat("_Stars", lv.night ? 1f : 0f);
                mr.sharedMaterial = m;
            }
            else
            {
                // Fallback: no shader found -> camera solid colour is used instead.
                mr.enabled = false;
            }

            SkyDome d = go.AddComponent<SkyDome>();
            d.cam = camera;
            return d;
        }

        void LateUpdate()
        {
            if (cam != null) transform.position = cam.position;
        }
    }
}
