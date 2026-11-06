using UnityEngine;

namespace ForestRunner
{
    /// <summary>Procedurally generated textures (noise, soft circle, light beam). No image assets needed.</summary>
    public static class ProcTex
    {
        static Texture2D soft, beam;

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h = h ^ (h >> 16);
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        static float ValueNoise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            int x0 = ((xi % period) + period) % period, x1 = (x0 + 1) % period;
            int y0 = ((yi % period) + period) % period, y1 = (y0 + 1) % period;
            float a = Hash(x0, y0, seed), b = Hash(x1, y0, seed), c = Hash(x0, y1, seed), d = Hash(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>Tileable multi-octave noise blending between two colours.</summary>
        public static Texture2D Noise(Color a, Color b, int size, int baseFreq, int seed)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size;
                    float n = 0f, amp = 0.55f, norm = 0f;
                    int f = baseFreq;
                    for (int o = 0; o < 3; o++)
                    {
                        n += ValueNoise(u * f, v * f, f, seed + o) * amp;
                        norm += amp; amp *= 0.5f; f *= 2;
                    }
                    n /= norm;
                    px[y * size + x] = Color.Lerp(a, b, n);
                }
            }
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(true);
            return tex;
        }

        /// <summary>Soft white radial gradient with alpha falloff (for particles and glows).</summary>
        public static Texture2D SoftCircle()
        {
            if (soft != null) return soft;
            int s = 64;
            soft = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    float a = d * d * (3f - 2f * d);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            soft.SetPixels32(px);
            soft.wrapMode = TextureWrapMode.Clamp;
            soft.Apply(false);
            return soft;
        }

        /// <summary>Vertical light-shaft gradient (soft sides, fading to the bottom).</summary>
        public static Texture2D Beam()
        {
            if (beam != null) return beam;
            int w = 32, h = 128;
            beam = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                    float side = Mathf.Clamp01(1f - u); side = side * side;
                    float vert = Mathf.Clamp01((float)y / h);
                    float a = side * Mathf.Pow(vert, 0.6f) * 0.5f;
                    px[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            beam.SetPixels32(px);
            beam.wrapMode = TextureWrapMode.Clamp;
            beam.Apply(false);
            return beam;
        }
    }
}
