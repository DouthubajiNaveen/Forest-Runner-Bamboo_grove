using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Creates and owns all runtime materials for a level. Uses only built-in shaders that are
    /// keyword-free (Standard without keywords, Unlit/Color, Legacy transparent / particle
    /// shaders) so they survive shader variant stripping in standalone builds.
    /// </summary>
    public class MaterialLibrary
    {
        public Material path, grass, bamboo, bambooDark, leaf, leafDark, wood, woodDark, stone, stoneDark, moss;
        public Material red, bone, water, waterBed, voidDark, gold, goldDark, glow, glowWarm, lantern, roof;
        public Material flower, rayBeam;
        public Material shieldBubble, hoverGlow, magnetRing;
        public Material particleAdd, particleAlpha;
        public Material white;

        public static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader s = Shader.Find(names[i]);
                if (s != null) return s;
            }
            return Shader.Find("Standard");
        }

        public static Material Lit(Color c, float smooth, float metal)
        {
            Material m = new Material(FindShader("Standard", "Legacy Shaders/Diffuse"));
            m.color = c;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            m.enableInstancing = true;
            return m;
        }

        public static Material Unlit(Color c)
        {
            Material m = new Material(FindShader("Unlit/Color", "Sprites/Default"));
            m.color = c;
            return m;
        }

        public static Material Transparent(Color c)
        {
            Material m = new Material(FindShader("Legacy Shaders/Transparent/Diffuse", "Sprites/Default"));
            m.color = c;
            return m;
        }

        public static Material Additive(Color c, Texture tex)
        {
            Material m = new Material(FindShader("Legacy Shaders/Particles/Additive", "Sprites/Default"));
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c); else m.color = c;
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        public static Material AlphaBlend(Color c, Texture tex)
        {
            Material m = new Material(FindShader("Legacy Shaders/Particles/Alpha Blended", "Sprites/Default"));
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c); else m.color = c;
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        static Color Mul(Color c, float k)
        {
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        public static MaterialLibrary Create(LevelConfig lv)
        {
            MaterialLibrary m = new MaterialLibrary();

            m.path = Lit(Color.white, 0.05f, 0f);
            m.path.mainTexture = ProcTex.Noise(Mul(lv.pathColor, 0.82f), Mul(lv.pathColor, 1.12f), 128, 6, 11);
            m.grass = Lit(Color.white, 0.05f, 0f);
            m.grass.mainTexture = ProcTex.Noise(Mul(lv.grassColor, 0.75f), Mul(lv.grassColor, 1.2f), 128, 8, 23);
            m.bamboo = Lit(lv.bambooColor, 0.35f, 0f);
            m.bambooDark = Lit(Mul(lv.bambooColor, 0.6f), 0.3f, 0f);
            m.leaf = Lit(lv.leafColor, 0.15f, 0f);
            m.leafDark = Lit(Mul(lv.leafColor, 0.65f), 0.1f, 0f);
            m.wood = Lit(new Color(0.46f, 0.31f, 0.18f, 1f), 0.1f, 0f);
            m.woodDark = Lit(new Color(0.28f, 0.18f, 0.1f, 1f), 0.1f, 0f);
            m.stone = Lit(Color.white, 0.1f, 0f);
            m.stone.mainTexture = ProcTex.Noise(Mul(lv.stoneColor, 0.75f), Mul(lv.stoneColor, 1.1f), 128, 5, 37);
            m.stoneDark = Lit(Mul(lv.stoneColor, 0.6f), 0.1f, 0f);
            m.moss = Lit(Mul(lv.leafColor, 0.85f), 0.05f, 0f);
            m.red = Lit(new Color(0.65f, 0.12f, 0.1f, 1f), 0.25f, 0f);
            m.bone = Lit(new Color(0.88f, 0.84f, 0.7f, 1f), 0.3f, 0f);
            m.water = Transparent(lv.waterColor);
            m.waterBed = Unlit(new Color(0.02f, 0.05f, 0.08f, 1f));
            m.voidDark = Unlit(new Color(0.01f, 0.01f, 0.015f, 1f));
            m.gold = Lit(new Color(1f, 0.78f, 0.12f, 1f), 0.75f, 0.85f);
            m.goldDark = Lit(new Color(0.85f, 0.58f, 0.05f, 1f), 0.6f, 0.8f);
            m.glow = Unlit(lv.glowColor);
            m.glowWarm = Unlit(new Color(1f, 0.7f, 0.3f, 1f));
            m.lantern = Unlit(new Color(1f, 0.62f, 0.22f, 1f));
            m.roof = Lit(new Color(0.22f, 0.16f, 0.14f, 1f), 0.2f, 0f);
            m.flower = Lit(new Color(1f, 0.8f, 0.9f, 1f), 0.2f, 0f);
            m.white = Lit(Color.white, 0.2f, 0f);

            Color ray = lv.night ? new Color(0.25f, 0.35f, 0.7f, 0.5f) : Color.Lerp(lv.lightColor, Color.white, 0.4f);
            ray.a = lv.night ? 0.3f : 0.35f;
            m.rayBeam = Additive(ray, ProcTex.Beam());
            m.shieldBubble = Additive(new Color(0.25f, 0.55f, 1f, 0.28f), null);
            m.hoverGlow = Additive(new Color(0.4f, 0.95f, 1f, 0.5f), ProcTex.SoftCircle());
            m.magnetRing = Additive(new Color(1f, 0.35f, 0.3f, 0.55f), ProcTex.SoftCircle());
            m.particleAdd = Additive(Color.white, ProcTex.SoftCircle());
            m.particleAlpha = AlphaBlend(Color.white, ProcTex.SoftCircle());
            return m;
        }
    }
}
