using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Combines several primitive meshes into one mesh (one draw call per material) and caches
    /// the result by key. All pooled pieces of the same kind share a single combined mesh.
    /// </summary>
    public static class MeshKit
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public class Builder
        {
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<Vector3> n = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> t = new List<int>();

            public Builder Add(Mesh m, Vector3 pos, Vector3 euler, Vector3 scale)
            {
                Matrix4x4 mat = Matrix4x4.TRS(pos, Quaternion.Euler(euler), scale);
                Matrix4x4 nmat = mat.inverse.transpose;
                Vector3[] mv = m.vertices;
                Vector3[] mn = m.normals;
                Vector2[] muv = m.uv;
                int[] mt = m.triangles;
                int off = v.Count;
                for (int i = 0; i < mv.Length; i++)
                {
                    v.Add(mat.MultiplyPoint3x4(mv[i]));
                    n.Add(nmat.MultiplyVector(mn[i]).normalized);
                    uv.Add(muv[i]);
                }
                bool flip = (scale.x * scale.y * scale.z) < 0f;
                for (int i = 0; i < mt.Length; i += 3)
                {
                    if (!flip) { t.Add(off + mt[i]); t.Add(off + mt[i + 1]); t.Add(off + mt[i + 2]); }
                    else { t.Add(off + mt[i]); t.Add(off + mt[i + 2]); t.Add(off + mt[i + 1]); }
                }
                return this;
            }

            public Builder Add(Mesh m, Vector3 pos, Vector3 scale)
            {
                return Add(m, pos, Vector3.zero, scale);
            }

            public Mesh ToMesh(string name)
            {
                Mesh m = new Mesh();
                m.name = name;
                m.SetVertices(v);
                m.SetNormals(n);
                m.SetUVs(0, uv);
                m.SetTriangles(t, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        public static Mesh Get(string key, Action<Builder> build)
        {
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            Builder b = new Builder();
            build(b);
            m = b.ToMesh("Kit_" + key);
            cache[key] = m;
            return m;
        }
    }
}
