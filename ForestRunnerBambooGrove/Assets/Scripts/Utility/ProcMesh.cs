using System.Collections.Generic;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Procedurally generated primitive meshes (cube, cylinder, sphere, capsule, quad).
    /// Generated in code so the project has no dependency on external model assets and the
    /// meshes are always readable (needed for mesh combining).
    /// Dimensions match Unity's built-in primitives: cube 1x1x1, cylinder h=2 r=0.5,
    /// sphere d=1, capsule h=2 r=0.5.
    /// </summary>
    public static class ProcMesh
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static Mesh Cube { get { return GetCached("cube", BuildCube); } }
        public static Mesh Quad { get { return GetCached("quad", BuildQuad); } }
        public static Mesh Cylinder { get { return GetCached("cyl16", delegate { return BuildCylinder(16); }); } }
        public static Mesh CylinderLow { get { return GetCached("cyl8", delegate { return BuildCylinder(8); }); } }
        public static Mesh Sphere { get { return GetCached("sph16", delegate { return BuildSphere(16, 10); }); } }
        public static Mesh SphereLow { get { return GetCached("sph8", delegate { return BuildSphere(8, 5); }); } }
        public static Mesh Capsule { get { return GetCached("cap12", delegate { return BuildCapsule(12, 6); }); } }
        public static Mesh CapsuleLow { get { return GetCached("cap6", delegate { return BuildCapsule(6, 3); }); } }

        delegate Mesh Builder();

        static Mesh GetCached(string key, Builder b)
        {
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = b();
            m.name = "Proc_" + key;
            cache[key] = m;
            return m;
        }

        static Mesh Finish(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            Mesh m = new Mesh();
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        static void AddQuad(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t,
                            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nrm)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            n.Add(nrm); n.Add(nrm); n.Add(nrm); n.Add(nrm);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            // Unity: triangle (a,b,c) faces Cross(b-a, c-a)
            bool ok = Vector3.Dot(Vector3.Cross(b - a, c - a), nrm) > 0f;
            if (ok)
            {
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }
            else
            {
                t.Add(i); t.Add(i + 2); t.Add(i + 1);
                t.Add(i); t.Add(i + 3); t.Add(i + 2);
            }
        }

        static Mesh BuildCube()
        {
            List<Vector3> v = new List<Vector3>(); List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>(); List<int> t = new List<int>();
            float h = 0.5f;
            AddQuad(v, n, uv, t, new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h), Vector3.forward);
            AddQuad(v, n, uv, t, new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h), Vector3.back);
            AddQuad(v, n, uv, t, new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h), Vector3.right);
            AddQuad(v, n, uv, t, new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h), Vector3.left);
            AddQuad(v, n, uv, t, new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h), Vector3.up);
            AddQuad(v, n, uv, t, new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, -h, h), new Vector3(-h, -h, h), Vector3.down);
            return Finish(v, n, uv, t);
        }

        /// <summary>Unit quad in the XY plane facing -Z (towards a camera looking along +Z).</summary>
        static Mesh BuildQuad()
        {
            List<Vector3> v = new List<Vector3>(); List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>(); List<int> t = new List<int>();
            AddQuad(v, n, uv, t, new Vector3(-0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0.5f, -0.5f, 0), Vector3.back);
            return Finish(v, n, uv, t);
        }

        /// <summary>
        /// Surface of revolution. Profile points are (radius, y, normalRadius, normalY) from bottom to top.
        /// </summary>
        static Mesh Lathe(List<Vector4> prof, int segs)
        {
            int w = segs + 1;
            List<Vector3> v = new List<Vector3>(); List<Vector3> n = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>(); List<int> t = new List<int>();
            for (int i = 0; i < prof.Count; i++)
            {
                Vector4 p = prof[i];
                for (int j = 0; j <= segs; j++)
                {
                    float th = (float)j / segs * Mathf.PI * 2f;
                    float c = Mathf.Cos(th), s = Mathf.Sin(th);
                    v.Add(new Vector3(p.x * c, p.y, p.x * s));
                    n.Add(new Vector3(p.z * c, p.w, p.z * s));
                    uv.Add(new Vector2((float)j / segs, (float)i / (prof.Count - 1)));
                }
            }
            for (int i = 0; i < prof.Count - 1; i++)
            {
                for (int j = 0; j < segs; j++)
                {
                    int a = i * w + j;
                    int b = (i + 1) * w + j;
                    int c2 = i * w + j + 1;
                    int d = (i + 1) * w + j + 1;
                    t.Add(a); t.Add(b); t.Add(c2);
                    t.Add(c2); t.Add(b); t.Add(d);
                }
            }
            return Finish(v, n, uv, t);
        }

        static Mesh BuildCylinder(int segs)
        {
            List<Vector4> p = new List<Vector4>();
            p.Add(new Vector4(0f, -1f, 0f, -1f));
            p.Add(new Vector4(0.5f, -1f, 0f, -1f));
            p.Add(new Vector4(0.5f, -1f, 1f, 0f));
            p.Add(new Vector4(0.5f, 1f, 1f, 0f));
            p.Add(new Vector4(0.5f, 1f, 0f, 1f));
            p.Add(new Vector4(0f, 1f, 0f, 1f));
            return Lathe(p, segs);
        }

        static Mesh BuildSphere(int segs, int rings)
        {
            List<Vector4> p = new List<Vector4>();
            for (int k = 0; k <= rings; k++)
            {
                float phi = Mathf.PI * k / rings;
                p.Add(new Vector4(0.5f * Mathf.Sin(phi), -0.5f * Mathf.Cos(phi), Mathf.Sin(phi), -Mathf.Cos(phi)));
            }
            return Lathe(p, segs);
        }

        static Mesh BuildCapsule(int segs, int rings)
        {
            List<Vector4> p = new List<Vector4>();
            for (int k = 0; k <= rings; k++)
            {
                float phi = (Mathf.PI * 0.5f) * k / rings;
                p.Add(new Vector4(0.5f * Mathf.Sin(phi), -0.5f - 0.5f * Mathf.Cos(phi), Mathf.Sin(phi), -Mathf.Cos(phi)));
            }
            for (int k = 0; k <= rings; k++)
            {
                float phi = Mathf.PI * 0.5f + (Mathf.PI * 0.5f) * k / rings;
                p.Add(new Vector4(0.5f * Mathf.Sin(phi), 0.5f - 0.5f * Mathf.Cos(phi), Mathf.Sin(phi), -Mathf.Cos(phi)));
            }
            return Lathe(p, segs);
        }
    }
}
