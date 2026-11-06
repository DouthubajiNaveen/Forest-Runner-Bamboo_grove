using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForestRunner
{
    /// <summary>Pool keys for every world piece.</summary>
    public static class Pieces
    {
        public const string Section = "Section";
        public const string GroundLane = "GroundLane";
        public const string GroundSide = "GroundSide";
        public const string PitLiner = "PitLiner";
        public const string Water = "Water";
        public const string Bridge = "Bridge";
        public const string Log = "Log";
        public const string Rock = "Rock";
        public const string Barricade = "Barricade";
        public const string Hanging = "Hanging";
        public const string Spike = "Spike";
        public const string Boulder = "Boulder";
        public const string TempleWall = "TempleWall";
        public const string TempleGate = "TempleGate";
        public const string Coin = "Coin";
        public const string Bamboo = "Bamboo";
        public const string Bush = "Bush";
        public const string Grass = "Grass";
        public const string DecorRock = "DecorRock";
        public const string Flower = "Flower";
        public const string GlowPlant = "GlowPlant";
        public const string Temple = "Temple";
        public const string VineArch = "VineArch";
        public const string Waterfall = "Waterfall";
        public const string SunRay = "SunRay";

        public static string PowerUp(PowerUpType t) { return "PU_" + t.ToString(); }

        public static string ForObstacle(ObstacleType t)
        {
            switch (t)
            {
                case ObstacleType.Log: return Log;
                case ObstacleType.Rock: return Rock;
                case ObstacleType.Barricade: return Barricade;
                case ObstacleType.Hanging: return Hanging;
                case ObstacleType.Spike: return Spike;
                case ObstacleType.Boulder: return Boulder;
                case ObstacleType.TempleWall: return TempleWall;
                case ObstacleType.TempleGate: return TempleGate;
            }
            return Log;
        }
    }

    /// <summary>
    /// Builds every pooled world piece from procedural primitives. Pieces share combined meshes
    /// (MeshKit) and shared instanced materials (MaterialLibrary).
    /// </summary>
    public static class PieceFactory
    {
        static readonly Vector3 V0 = Vector3.zero;

        public static void RegisterAll(ObjectPooler pool, MaterialLibrary m)
        {
            Dictionary<string, Func<GameObject>> f = BuildFactories(m);
            foreach (KeyValuePair<string, Func<GameObject>> kv in f) pool.Register(kv.Key, kv.Value);
            pool.Prewarm(Pieces.Section, 6);
            pool.Prewarm(Pieces.GroundLane, 24);
            pool.Prewarm(Pieces.GroundSide, 24);
            pool.Prewarm(Pieces.Bamboo, 120);
            pool.Prewarm(Pieces.Bush, 30);
            pool.Prewarm(Pieces.Grass, 40);
            pool.Prewarm(Pieces.Coin, 60);
            pool.Prewarm(Pieces.Log, 6);
            pool.Prewarm(Pieces.Rock, 6);
        }

        public static Dictionary<string, Func<GameObject>> BuildFactories(MaterialLibrary m)
        {
            Dictionary<string, Func<GameObject>> d = new Dictionary<string, Func<GameObject>>();
            d[Pieces.Section] = delegate { GameObject g = new GameObject("Section"); g.AddComponent<TrackSection>(); return g; };
            d[Pieces.GroundLane] = delegate { return GroundLane(m); };
            d[Pieces.GroundSide] = delegate { return Slab(m.grass, "GroundSide", false); };
            d[Pieces.PitLiner] = delegate { return Slab(m.voidDark, "PitLiner", false); };
            d[Pieces.Water] = delegate { return Water(m); };
            d[Pieces.Bridge] = delegate { return Bridge(m); };
            d[Pieces.Log] = delegate { return Log(m); };
            d[Pieces.Rock] = delegate { return Rock(m); };
            d[Pieces.Barricade] = delegate { return Barricade(m); };
            d[Pieces.Hanging] = delegate { return Hanging(m); };
            d[Pieces.Spike] = delegate { return Spike(m); };
            d[Pieces.Boulder] = delegate { return Boulder(m); };
            d[Pieces.TempleWall] = delegate { return TempleWall(m); };
            d[Pieces.TempleGate] = delegate { return TempleGate(m); };
            d[Pieces.Coin] = delegate { return CoinPiece(m); };
            d[Pieces.Bamboo] = delegate { return Bamboo(m); };
            d[Pieces.Bush] = delegate { return Bush(m); };
            d[Pieces.Grass] = delegate { return Grass(m); };
            d[Pieces.DecorRock] = delegate { return DecorRock(m); };
            d[Pieces.Flower] = delegate { return Flower(m); };
            d[Pieces.GlowPlant] = delegate { return GlowPlant(m); };
            d[Pieces.Temple] = delegate { return Temple(m); };
            d[Pieces.VineArch] = delegate { return VineArch(m); };
            d[Pieces.Waterfall] = delegate { return Waterfall(m); };
            d[Pieces.SunRay] = delegate { return SunRay(m); };
            for (int i = 0; i < PowerUpManager.TypeCount; i++)
            {
                PowerUpType t = (PowerUpType)i;
                d[Pieces.PowerUp(t)] = delegate { return PowerUpPiece(m, t); };
            }
            return d;
        }

        // ------------------------------------------------------------ helpers

        static GameObject Node(string name, Transform parent)
        {
            GameObject g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            return g;
        }

        static GameObject MeshChild(Transform parent, string name, Mesh mesh, Material mat, bool shadows)
        {
            GameObject g = Node(name, parent);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = g.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return g;
        }

        static BoxCollider AddBox(GameObject go, Vector3 center, Vector3 size, bool trigger)
        {
            BoxCollider b = go.AddComponent<BoxCollider>();
            b.center = center; b.size = size; b.isTrigger = trigger;
            return b;
        }

        static Obstacle MakeObstacle(GameObject root, ObstacleType t, bool hoverable)
        {
            Obstacle o = root.AddComponent<Obstacle>();
            o.type = t;
            o.hoverable = hoverable;
            GameConstants.SetLayerRecursive(root, GameConstants.LayerObstacle);
            return o;
        }

        static Vector3 S(float x, float y, float z) { return new Vector3(x, y, z); }

        // ------------------------------------------------------------ ground & water

        static GameObject GroundLane(MaterialLibrary m)
        {
            GameObject g = MeshChild(null, "GroundLane", ProcMesh.Cube, m.path, false);
            BoxCollider b = g.AddComponent<BoxCollider>();
            b.size = Vector3.one;
            g.layer = GameConstants.LayerGround;
            g.GetComponent<MeshRenderer>().receiveShadows = true;
            return g;
        }

        static GameObject Slab(Material mat, string name, bool collider)
        {
            GameObject g = MeshChild(null, name, ProcMesh.Cube, mat, false);
            if (collider) { g.AddComponent<BoxCollider>(); g.layer = GameConstants.LayerGround; }
            return g;
        }

        static GameObject Water(MaterialLibrary m)
        {
            GameObject root = Node("Water", null);
            GameObject top = MeshChild(root.transform, "Surface", ProcMesh.Cube, m.water, false);
            top.transform.localScale = S(1f, 0.2f, 1f);
            top.transform.localPosition = S(0f, -0.7f, 0f);
            GameObject bed = MeshChild(root.transform, "Bed", ProcMesh.Cube, m.waterBed, false);
            bed.transform.localScale = S(1f, 0.2f, 1f);
            bed.transform.localPosition = S(0f, -2.4f, 0f);
            return root;
        }

        static GameObject Bridge(MaterialLibrary m)
        {
            GameObject root = Node("Bridge", null);
            Mesh mesh = MeshKit.Get("bridge", delegate (MeshKit.Builder b)
            {
                for (int i = 0; i < 8; i++)
                    b.Add(ProcMesh.Cube, S(0f, -0.04f, 0.5f + i), S(2.3f, 0.12f, 0.88f));
                for (int s = -1; s <= 1; s += 2)
                {
                    b.Add(ProcMesh.Cube, S(0.9f * s, -0.22f, 4f), S(0.16f, 0.16f, 8.2f));
                    b.Add(ProcMesh.Cube, S(1.15f * s, 0.85f, 4f), S(0.08f, 0.08f, 7.6f));
                    for (int k = 0; k < 3; k++)
                        b.Add(ProcMesh.Cube, S(1.15f * s, 0.4f, 0.3f + k * 3.7f), S(0.14f, 1.0f, 0.14f));
                }
            });
            MeshChild(root.transform, "Wood", mesh, m.wood, false);
            return root;
        }

        // ------------------------------------------------------------ obstacles

        static GameObject Log(MaterialLibrary m)
        {
            GameObject root = Node("Log", null);
            Mesh body = MeshKit.Get("log_body", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cylinder, S(0f, 0.4f, 0f), S(0f, 0f, 90f), S(0.8f, 1.2f, 0.8f));
                float[] xs = new float[] { -0.7f, 0.1f, 0.8f };
                for (int i = 0; i < xs.Length; i++)
                    b.Add(ProcMesh.Cylinder, S(xs[i], 0.4f, 0f), S(0f, 0f, 90f), S(0.86f, 0.06f, 0.86f));
            });
            Mesh caps = MeshKit.Get("log_caps", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cylinder, S(-1.205f, 0.4f, 0f), S(0f, 0f, 90f), S(0.64f, 0.02f, 0.64f));
                b.Add(ProcMesh.Cylinder, S(1.205f, 0.4f, 0f), S(0f, 0f, 90f), S(0.64f, 0.02f, 0.64f));
            });
            MeshChild(root.transform, "Body", body, m.bamboo, true);
            MeshChild(root.transform, "Caps", caps, m.bone, false);
            AddBox(root, S(0f, 0.4f, 0f), S(2.4f, 0.8f, 0.8f), true);
            MakeObstacle(root, ObstacleType.Log, true);
            return root;
        }

        static GameObject Rock(MaterialLibrary m)
        {
            GameObject root = Node("Rock", null);
            Mesh body = MeshKit.Get("rock_body", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Sphere, S(0f, 0.9f, 0f), S(0f, 20f, 0f), S(2.1f, 2.0f, 1.7f));
                b.Add(ProcMesh.Sphere, S(0.7f, 0.5f, -0.7f), S(0f, 0f, 0f), S(1.3f, 1.2f, 1.2f));
                b.Add(ProcMesh.Sphere, S(-0.8f, 0.4f, 0.5f), S(0f, 40f, 0f), S(1.0f, 0.9f, 1.0f));
            });
            Mesh moss = MeshKit.Get("rock_moss", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Sphere, S(0f, 1.78f, 0f), S(0f, 0f, 0f), S(1.5f, 0.45f, 1.1f));
            });
            MeshChild(root.transform, "Body", body, m.stone, true);
            MeshChild(root.transform, "Moss", moss, m.moss, false);
            AddBox(root, S(0f, 0.97f, 0f), S(2.1f, 1.95f, 1.7f), true);
            MakeObstacle(root, ObstacleType.Rock, true);
            return root;
        }

        static GameObject Barricade(MaterialLibrary m)
        {
            GameObject root = Node("Barricade", null);
            Mesh wood = MeshKit.Get("barricade", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(-1.0f, 0.6f, 0f), S(0.2f, 1.2f, 0.2f));
                b.Add(ProcMesh.Cube, S(1.0f, 0.6f, 0f), S(0.2f, 1.2f, 0.2f));
                float[] ys = new float[] { 0.35f, 0.7f, 1.05f };
                for (int i = 0; i < ys.Length; i++) b.Add(ProcMesh.Cube, S(0f, ys[i], 0.08f), S(2.3f, 0.16f, 0.1f));
                b.Add(ProcMesh.Cube, S(0f, 0.7f, -0.06f), S(0f, 0f, 28f), S(2.5f, 0.12f, 0.08f));
            });
            MeshChild(root.transform, "Wood", wood, m.wood, true);
            AddBox(root, S(0f, 0.58f, 0f), S(2.4f, 1.15f, 0.4f), true);
            MakeObstacle(root, ObstacleType.Barricade, true);
            return root;
        }

        static GameObject Hanging(MaterialLibrary m)
        {
            GameObject root = Node("Hanging", null);
            Mesh bar = MeshKit.Get("hang_bar", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cylinder, S(0f, 1.5f, 0f), S(0f, 0f, 90f), S(0.55f, 1.25f, 0.55f));
                b.Add(ProcMesh.Cylinder, S(-0.6f, 1.5f, 0f), S(0f, 0f, 90f), S(0.62f, 0.05f, 0.62f));
                b.Add(ProcMesh.Cylinder, S(0.6f, 1.5f, 0f), S(0f, 0f, 90f), S(0.62f, 0.05f, 0.62f));
            });
            Mesh rope = MeshKit.Get("hang_rope", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.CylinderLow, S(-0.95f, 3.2f, 0f), V0, S(0.06f, 1.45f, 0.06f));
                b.Add(ProcMesh.CylinderLow, S(0.95f, 3.2f, 0f), V0, S(0.06f, 1.45f, 0.06f));
            });
            Mesh leaves = MeshKit.Get("hang_leaves", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(-0.5f, 1.2f, 0.1f), S(0f, 0f, -20f), S(0.7f, 0.1f, 0.3f));
                b.Add(ProcMesh.SphereLow, S(0.5f, 1.2f, -0.1f), S(0f, 0f, 20f), S(0.7f, 0.1f, 0.3f));
            });
            MeshChild(root.transform, "Bar", bar, m.bamboo, true);
            MeshChild(root.transform, "Rope", rope, m.woodDark, false);
            MeshChild(root.transform, "Leaves", leaves, m.leaf, false);
            AddBox(root, S(0f, 1.5f, 0f), S(2.4f, 0.7f, 0.5f), true);
            MakeObstacle(root, ObstacleType.Hanging, true);
            return root;
        }

        static GameObject Spike(MaterialLibrary m)
        {
            GameObject root = Node("Spike", null);
            Mesh baseMesh = MeshKit.Get("spike_base", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(0f, 0.05f, 0f), S(2.2f, 0.1f, 1.6f));
            });
            Mesh spikes = MeshKit.Get("spike_tips", delegate (MeshKit.Builder b)
            {
                for (int ix = -1; ix <= 1; ix++)
                    for (int iz = -1; iz <= 1; iz++)
                        b.Add(ProcMesh.CapsuleLow, S(ix * 0.7f, 0.32f, iz * 0.5f), V0, S(0.14f, 0.28f, 0.14f));
            });
            MeshChild(root.transform, "Base", baseMesh, m.woodDark, true);
            MeshChild(root.transform, "Spikes", spikes, m.bone, true);
            AddBox(root, S(0f, 0.28f, 0f), S(2.2f, 0.55f, 1.6f), true);
            MakeObstacle(root, ObstacleType.Spike, true);
            return root;
        }

        static GameObject Boulder(MaterialLibrary m)
        {
            GameObject root = Node("Boulder", null);
            GameObject roll = Node("Roll", root.transform);
            roll.transform.localPosition = S(0f, 0.95f, 0f);
            Mesh body = MeshKit.Get("boulder_body", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Sphere, V0, S(1.9f, 1.9f, 1.9f));
                b.Add(ProcMesh.SphereLow, S(0.8f, 0.4f, 0.3f), S(0.7f, 0.7f, 0.7f));
                b.Add(ProcMesh.SphereLow, S(-0.7f, 0.6f, -0.3f), S(0.6f, 0.6f, 0.6f));
                b.Add(ProcMesh.SphereLow, S(0.1f, -0.8f, 0.5f), S(0.7f, 0.5f, 0.7f));
            });
            Mesh moss = MeshKit.Get("boulder_moss", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(0.3f, 0.85f, 0.2f), S(0.9f, 0.3f, 0.8f));
                b.Add(ProcMesh.SphereLow, S(-0.6f, -0.2f, 0.75f), S(0.6f, 0.3f, 0.5f));
            });
            MeshChild(roll.transform, "Body", body, m.stone, true);
            MeshChild(roll.transform, "Moss", moss, m.moss, false);
            AddBox(root, S(0f, 0.95f, 0f), S(1.8f, 1.8f, 1.8f), true);
            Obstacle o = MakeObstacle(root, ObstacleType.Boulder, true);
            o.rollVisual = roll.transform;
            o.rollSpeed = 9f;
            return root;
        }

        static GameObject TempleWall(MaterialLibrary m)
        {
            GameObject root = Node("TempleWall", null);
            Mesh stone = MeshKit.Get("twall_stone", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(0f, 1.15f, 0f), S(2.4f, 2.3f, 0.9f));
                b.Add(ProcMesh.Cube, S(0f, 2.4f, 0f), S(2.6f, 0.2f, 1.1f));
                b.Add(ProcMesh.Cube, S(0f, 0.15f, 0f), S(2.55f, 0.3f, 1.05f));
            });
            Mesh red = MeshKit.Get("twall_red", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(0f, 1.7f, 0f), S(2.42f, 0.16f, 0.92f));
                b.Add(ProcMesh.Cube, S(0f, 0.9f, -0.455f), S(1.4f, 0.8f, 0.03f));
            });
            MeshChild(root.transform, "Stone", stone, m.stone, true);
            MeshChild(root.transform, "Red", red, m.red, false);
            AddBox(root, S(0f, 1.25f, 0f), S(2.4f, 2.5f, 0.9f), true);
            MakeObstacle(root, ObstacleType.TempleWall, true);
            return root;
        }

        static GameObject TempleGate(MaterialLibrary m)
        {
            GameObject root = Node("TempleGate", null);
            Mesh stone = MeshKit.Get("tgate_stone", delegate (MeshKit.Builder b)
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    b.Add(ProcMesh.Cylinder, S(3.6f * s, 1.6f, 0f), V0, S(0.7f, 1.6f, 0.7f));
                    b.Add(ProcMesh.Cube, S(3.6f * s, 0.15f, 0f), S(1.1f, 0.3f, 1.1f));
                    b.Add(ProcMesh.Cube, S(3.6f * s, 3.3f, 0f), S(1.1f, 0.25f, 1.1f));
                }
                b.Add(ProcMesh.Cube, S(0f, 1.5f, 0f), S(7.6f, 0.7f, 0.8f));
            });
            Mesh red = MeshKit.Get("tgate_red", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(0f, 1.93f, 0f), S(7.9f, 0.14f, 0.95f));
                b.Add(ProcMesh.Cube, S(0f, 1.05f, 0f), S(7.9f, 0.1f, 0.95f));
            });
            Mesh lan = MeshKit.Get("tgate_lantern", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(-3.6f, 2.6f, -0.65f), S(0.3f, 0.4f, 0.3f));
                b.Add(ProcMesh.SphereLow, S(3.6f, 2.6f, -0.65f), S(0.3f, 0.4f, 0.3f));
            });
            MeshChild(root.transform, "Stone", stone, m.stone, true);
            MeshChild(root.transform, "Red", red, m.red, false);
            MeshChild(root.transform, "Lantern", lan, m.lantern, false);
            AddBox(root, S(0f, 1.5f, 0f), S(7.6f, 0.7f, 0.8f), true);
            MakeObstacle(root, ObstacleType.TempleGate, true);
            return root;
        }

        // ------------------------------------------------------------ collectibles

        static GameObject CoinPiece(MaterialLibrary m)
        {
            GameObject root = Node("Coin", null);
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.radius = 0.6f; sc.isTrigger = true;
            GameObject spin = Node("Spin", root.transform);
            Mesh disc = MeshKit.Get("coin_disc", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cylinder, V0, S(90f, 0f, 0f), S(0.7f, 0.07f, 0.7f));
            });
            Mesh ring = MeshKit.Get("coin_ring", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cylinder, V0, S(90f, 0f, 0f), S(0.48f, 0.09f, 0.48f));
            });
            MeshChild(spin.transform, "Disc", disc, m.gold, false);
            MeshChild(spin.transform, "Ring", ring, m.goldDark, false);
            Coin c = root.AddComponent<Coin>();
            c.visual = spin.transform;
            GameConstants.SetLayerRecursive(root, GameConstants.LayerCollectible);
            return root;
        }

        static GameObject PowerUpPiece(MaterialLibrary m, PowerUpType t)
        {
            PowerUpConfig cfg = ConfigDatabase.GetPowerUp(t);
            GameObject root = Node(Pieces.PowerUp(t), null);
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.radius = 0.95f; sc.isTrigger = true;
            GameObject spin = Node("Spin", root.transform);
            Material col = MaterialLibrary.Lit(cfg.color, 0.5f, 0.2f);
            Material orb = MaterialLibrary.Additive(new Color(cfg.color.r, cfg.color.g, cfg.color.b, 0.3f), null);

            GameObject orbObj = MeshChild(spin.transform, "Orb", ProcMesh.Sphere, orb, false);
            orbObj.transform.localScale = Vector3.one * 1.5f;

            Mesh main = MeshKit.Get("pu_main_" + t, delegate (MeshKit.Builder b)
            {
                switch (t)
                {
                    case PowerUpType.Magnet:
                        b.Add(ProcMesh.Cube, S(-0.2f, 0.02f, 0f), S(0.16f, 0.5f, 0.16f));
                        b.Add(ProcMesh.Cube, S(0.2f, 0.02f, 0f), S(0.16f, 0.5f, 0.16f));
                        b.Add(ProcMesh.Cube, S(0f, -0.2f, 0f), S(0.56f, 0.16f, 0.16f));
                        break;
                    case PowerUpType.Shield:
                        b.Add(ProcMesh.Sphere, V0, S(0.7f, 0.82f, 0.22f));
                        break;
                    case PowerUpType.SpeedBoost:
                        for (int k = 0; k < 2; k++)
                        {
                            float y = -0.18f + k * 0.34f;
                            b.Add(ProcMesh.Cube, S(-0.17f, y, 0f), S(0f, 0f, -40f), S(0.1f, 0.4f, 0.12f));
                            b.Add(ProcMesh.Cube, S(0.17f, y, 0f), S(0f, 0f, 40f), S(0.1f, 0.4f, 0.12f));
                        }
                        break;
                    case PowerUpType.DoubleCoins:
                        b.Add(ProcMesh.Cylinder, S(-0.17f, -0.05f, 0f), S(90f, 0f, 0f), S(0.55f, 0.08f, 0.55f));
                        b.Add(ProcMesh.Cylinder, S(0.17f, 0.08f, -0.1f), S(90f, 0f, 0f), S(0.55f, 0.08f, 0.55f));
                        break;
                    default:
                        b.Add(ProcMesh.SphereLow, S(-0.3f, 0.03f, 0f), S(0f, 0f, 25f), S(0.55f, 0.14f, 0.3f));
                        b.Add(ProcMesh.SphereLow, S(0.3f, 0.03f, 0f), S(0f, 0f, -25f), S(0.55f, 0.14f, 0.3f));
                        b.Add(ProcMesh.Sphere, V0, S(0.22f, 0.22f, 0.22f));
                        break;
                }
            });
            MeshChild(spin.transform, "Icon", main, t == PowerUpType.DoubleCoins ? m.gold : col, false);
            Mesh accent = MeshKit.Get("pu_acc_" + t, delegate (MeshKit.Builder b)
            {
                switch (t)
                {
                    case PowerUpType.Magnet:
                        b.Add(ProcMesh.Cube, S(-0.2f, 0.3f, 0f), S(0.17f, 0.12f, 0.17f));
                        b.Add(ProcMesh.Cube, S(0.2f, 0.3f, 0f), S(0.17f, 0.12f, 0.17f));
                        break;
                    case PowerUpType.Shield:
                        b.Add(ProcMesh.Cylinder, V0, S(90f, 0f, 0f), S(0.9f, 0.06f, 0.9f));
                        break;
                    default:
                        b.Add(ProcMesh.SphereLow, S(0f, 0f, 0f), S(0.05f, 0.05f, 0.05f));
                        break;
                }
            });
            MeshChild(spin.transform, "Accent", accent, m.white, false);

            PowerUpPickup p = root.AddComponent<PowerUpPickup>();
            p.type = t;
            p.visual = spin.transform;
            GameConstants.SetLayerRecursive(root, GameConstants.LayerCollectible);
            return root;
        }

        // ------------------------------------------------------------ decor

        static GameObject Bamboo(MaterialLibrary m)
        {
            GameObject root = Node("Bamboo", null);
            Mesh stalk = MeshKit.Get("bamboo_stalk", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.CylinderLow, S(0f, 7.5f, 0f), V0, S(0.3f, 7.5f, 0.3f));
                float[] ys = new float[] { 2.5f, 5.5f, 8.5f, 11.5f };
                for (int i = 0; i < ys.Length; i++)
                    b.Add(ProcMesh.CylinderLow, S(0f, ys[i], 0f), V0, S(0.38f, 0.06f, 0.38f));
            });
            Mesh leaves = MeshKit.Get("bamboo_leaves", delegate (MeshKit.Builder b)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 45f;
                    Vector3 off = Quaternion.Euler(0f, a, 0f) * S(0.9f, 0f, 0f);
                    b.Add(ProcMesh.SphereLow, S(0f, 14.3f, 0f) + off, S(0f, a, 14f), S(1.9f, 0.12f, 0.5f));
                }
                float[] ang = new float[] { 20f, 140f, 260f };
                float[] hy = new float[] { 10.5f, 8.2f, 12.3f };
                for (int i = 0; i < 3; i++)
                {
                    Vector3 off = Quaternion.Euler(0f, ang[i], 0f) * S(0.75f, 0f, 0f);
                    b.Add(ProcMesh.SphereLow, S(0f, hy[i], 0f) + off, S(0f, ang[i], -18f), S(1.5f, 0.1f, 0.4f));
                }
            });
            MeshChild(root.transform, "Stalk", stalk, m.bamboo, false);
            MeshChild(root.transform, "Leaves", leaves, m.leaf, false);
            return root;
        }

        static GameObject Bush(MaterialLibrary m)
        {
            GameObject root = Node("Bush", null);
            Mesh mesh = MeshKit.Get("bush", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(0f, 0.4f, 0f), S(1.3f, 0.9f, 1.2f));
                b.Add(ProcMesh.SphereLow, S(0.7f, 0.3f, 0.3f), S(1.0f, 0.8f, 0.9f));
                b.Add(ProcMesh.SphereLow, S(-0.6f, 0.3f, -0.3f), S(0.9f, 0.7f, 0.9f));
            });
            MeshChild(root.transform, "Body", mesh, m.leafDark, false);
            return root;
        }

        static GameObject Grass(MaterialLibrary m)
        {
            GameObject root = Node("Grass", null);
            Mesh mesh = MeshKit.Get("grass", delegate (MeshKit.Builder b)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f * Mathf.Deg2Rad;
                    b.Add(ProcMesh.CapsuleLow, S(Mathf.Cos(a) * 0.12f, 0.25f, Mathf.Sin(a) * 0.12f),
                        S(Mathf.Sin(a) * 22f, 0f, -Mathf.Cos(a) * 22f), S(0.09f, 0.32f, 0.09f));
                }
            });
            MeshChild(root.transform, "Blades", mesh, m.leaf, false);
            return root;
        }

        static GameObject DecorRock(MaterialLibrary m)
        {
            GameObject root = Node("DecorRock", null);
            Mesh mesh = MeshKit.Get("decor_rock", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(0f, 0.2f, 0f), S(0f, 25f, 0f), S(0.9f, 0.55f, 0.8f));
                b.Add(ProcMesh.SphereLow, S(0.5f, 0.1f, 0.2f), S(0.5f, 0.35f, 0.5f));
            });
            MeshChild(root.transform, "Body", mesh, m.stoneDark, false);
            return root;
        }

        static GameObject Flower(MaterialLibrary m)
        {
            GameObject root = Node("Flower", null);
            Mesh stems = MeshKit.Get("flower_stems", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.CylinderLow, S(-0.15f, 0.2f, 0f), S(0.03f, 0.2f, 0.03f));
                b.Add(ProcMesh.CylinderLow, S(0.1f, 0.18f, 0.1f), S(0.03f, 0.18f, 0.03f));
                b.Add(ProcMesh.CylinderLow, S(0f, 0.25f, -0.12f), S(0.03f, 0.25f, 0.03f));
            });
            Mesh heads = MeshKit.Get("flower_heads", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(-0.15f, 0.42f, 0f), S(0.15f, 0.1f, 0.15f));
                b.Add(ProcMesh.SphereLow, S(0.1f, 0.38f, 0.1f), S(0.15f, 0.1f, 0.15f));
                b.Add(ProcMesh.SphereLow, S(0f, 0.52f, -0.12f), S(0.17f, 0.11f, 0.17f));
            });
            MeshChild(root.transform, "Stems", stems, m.leafDark, false);
            MeshChild(root.transform, "Heads", heads, m.flower, false);
            return root;
        }

        static GameObject GlowPlant(MaterialLibrary m)
        {
            GameObject root = Node("GlowPlant", null);
            Mesh stems = MeshKit.Get("glow_stems", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.CylinderLow, S(0f, 0.4f, 0f), S(0.04f, 0.4f, 0.04f));
                b.Add(ProcMesh.CylinderLow, S(0.2f, 0.3f, 0.1f), S(0f, 0f, -15f), S(0.04f, 0.3f, 0.04f));
                b.Add(ProcMesh.CylinderLow, S(-0.2f, 0.25f, 0f), S(0f, 0f, 18f), S(0.04f, 0.25f, 0.04f));
            });
            Mesh bulbs = MeshKit.Get("glow_bulbs", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(0f, 0.85f, 0f), S(0.28f, 0.34f, 0.28f));
                b.Add(ProcMesh.SphereLow, S(0.3f, 0.6f, 0.1f), S(0.22f, 0.26f, 0.22f));
                b.Add(ProcMesh.SphereLow, S(-0.32f, 0.5f, 0f), S(0.2f, 0.24f, 0.2f));
                b.Add(ProcMesh.SphereLow, S(0f, 0.08f, 0f), S(0.9f, 0.1f, 0.9f));
            });
            MeshChild(root.transform, "Stems", stems, m.leafDark, false);
            MeshChild(root.transform, "Bulbs", bulbs, m.glow, false);
            return root;
        }

        static GameObject Temple(MaterialLibrary m)
        {
            GameObject root = Node("Temple", null);
            Mesh stone = MeshKit.Get("temple_stone", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(0f, 0.25f, 0f), S(9f, 0.5f, 8f));
                b.Add(ProcMesh.Cube, S(0f, 0.75f, 0f), S(8f, 0.5f, 7f));
                b.Add(ProcMesh.Cube, S(0f, 0.12f, -4.5f), S(3f, 0.25f, 1.2f));
                b.Add(ProcMesh.Cube, S(0f, 0.38f, -4.1f), S(3.4f, 0.25f, 1.0f));
            });
            Mesh wood = MeshKit.Get("temple_wood", delegate (MeshKit.Builder b)
            {
                for (int ix = -1; ix <= 1; ix++)
                    for (int s = -1; s <= 1; s += 2)
                        b.Add(ProcMesh.CylinderLow, S(3.2f * ix, 2.9f, 2.8f * s), S(0.45f, 1.9f, 0.45f));
                b.Add(ProcMesh.Cube, S(0f, 2.8f, 3.1f), S(7f, 3.6f, 0.3f));
                b.Add(ProcMesh.Cube, S(0f, 4.9f, -2.8f), S(7.4f, 0.4f, 0.4f));
                b.Add(ProcMesh.Cube, S(0f, 4.9f, 2.8f), S(7.4f, 0.4f, 0.4f));
            });
            Mesh roof = MeshKit.Get("temple_roof", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(-2.7f, 5.6f, 0f), S(0f, 0f, 25f), S(6.2f, 0.35f, 9f));
                b.Add(ProcMesh.Cube, S(2.7f, 5.6f, 0f), S(0f, 0f, -25f), S(6.2f, 0.35f, 9f));
                b.Add(ProcMesh.Cube, S(0f, 6.55f, 0f), S(0.6f, 0.4f, 9.4f));
            });
            Mesh lan = MeshKit.Get("temple_lantern", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.SphereLow, S(-2f, 3.7f, -3.3f), S(0.5f, 0.6f, 0.5f));
                b.Add(ProcMesh.SphereLow, S(2f, 3.7f, -3.3f), S(0.5f, 0.6f, 0.5f));
            });
            MeshChild(root.transform, "Stone", stone, m.stone, false);
            MeshChild(root.transform, "Wood", wood, m.red, false);
            MeshChild(root.transform, "Roof", roof, m.roof, false);
            MeshChild(root.transform, "Lantern", lan, m.lantern, false);
            return root;
        }

        static GameObject VineArch(MaterialLibrary m)
        {
            GameObject root = Node("VineArch", null);
            Mesh poles = MeshKit.Get("arch_poles", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.CylinderLow, S(-5.4f, 4.1f, 0f), S(0.3f, 4.1f, 0.3f));
                b.Add(ProcMesh.CylinderLow, S(5.4f, 4.1f, 0f), S(0.3f, 4.1f, 0.3f));
                b.Add(ProcMesh.CylinderLow, S(0f, 8.1f, 0f), S(0f, 0f, 90f), S(0.3f, 5.7f, 0.3f));
            });
            Mesh vines = MeshKit.Get("arch_vines", delegate (MeshKit.Builder b)
            {
                System.Random r = new System.Random(5);
                for (int i = 0; i < 9; i++)
                {
                    float x = -5f + i * 1.25f + (float)(r.NextDouble() - 0.5) * 0.5f;
                    float len = 1.5f + (float)r.NextDouble() * 1.7f;
                    b.Add(ProcMesh.CylinderLow, S(x, 8f - len * 0.5f, 0f), S(0.05f, len * 0.5f, 0.05f));
                    b.Add(ProcMesh.SphereLow, S(x, 8f - len, 0f), S(0.3f, 0.5f, 0.12f));
                    b.Add(ProcMesh.SphereLow, S(x + 0.1f, 8f - len * 0.55f, 0f), S(0f, 0f, 30f), S(0.28f, 0.1f, 0.08f));
                }
            });
            MeshChild(root.transform, "Poles", poles, m.bamboo, false);
            MeshChild(root.transform, "Vines", vines, m.leafDark, false);
            return root;
        }

        static GameObject Waterfall(MaterialLibrary m)
        {
            GameObject root = Node("Waterfall", null);
            Mesh cliff = MeshKit.Get("falls_cliff", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(0f, 5f, 2.5f), S(8f, 10f, 5f));
                b.Add(ProcMesh.Cube, S(-4.4f, 3f, 1.5f), S(0f, 15f, 0f), S(3.2f, 6f, 3.5f));
                b.Add(ProcMesh.Cube, S(4.4f, 2.4f, 1.5f), S(0f, -12f, 0f), S(3.2f, 4.8f, 3.5f));
            });
            Mesh sheet = MeshKit.Get("falls_sheet", delegate (MeshKit.Builder b)
            {
                b.Add(ProcMesh.Cube, S(0f, 5.2f, -0.15f), S(2.4f, 9.6f, 0.25f));
                b.Add(ProcMesh.Cube, S(0f, 0.05f, -2.2f), S(7f, 0.15f, 4.4f));
            });
            MeshChild(root.transform, "Cliff", cliff, m.stoneDark, false);
            MeshChild(root.transform, "Sheet", sheet, MaterialLibrary.Transparent(new Color(0.8f, 0.93f, 1f, 0.6f)), false);
            ParticleSystem mist = ParticleManager.CreateLoop("Mist", root.transform, m.particleAdd, new Color(0.9f, 0.97f, 1f, 0.35f),
                1.6f, 1.8f, 0.8f, 16f, false, -0.03f);
            mist.transform.localPosition = S(0f, 0.6f, -0.6f);
            mist.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.MainModule mm = mist.main;
            mm.playOnAwake = true;
            mist.Play();
            return root;
        }

        static GameObject SunRay(MaterialLibrary m)
        {
            GameObject g = MeshChild(null, "SunRay", ProcMesh.Quad, m.rayBeam, false);
            return g;
        }
    }
}
