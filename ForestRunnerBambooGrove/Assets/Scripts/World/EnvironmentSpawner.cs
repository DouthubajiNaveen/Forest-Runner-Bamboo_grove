using System.Collections.Generic;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// Endless procedural track generator. Builds 48-unit sections out of pooled pieces, ahead of
    /// the player, and recycles sections behind. Each section = 4 rows of 12 units. Rows are
    /// chosen with weighted randomness from the LevelConfig and constrained so that every row
    /// always has a fair way through (a free lane, a jump-over, a slide-under or a bridge) and
    /// hazards are always followed by recovery rows.
    /// </summary>
    public class EnvironmentSpawner : MonoBehaviour
    {
        public static EnvironmentSpawner Instance { get; private set; }

        public float spawnAhead = 200f;
        public float despawnBehind = 45f;

        LevelConfig lv;
        ObjectPooler pool;
        Transform root;
        Transform focus;
        bool peaceful;

        readonly List<TrackSection> active = new List<TrackSection>(10);
        float nextZ;
        int safeRows;
        int cooldown;
        float distSincePU;
        float nextPU = 260f;

        const float L = GameConstants.SectionLength;
        const float RowLen = GameConstants.RowLength;

        public static EnvironmentSpawner Create(LevelConfig level, ObjectPooler pooler, Transform focusTarget, bool peacefulMode)
        {
            GameObject go = new GameObject("EnvironmentSpawner");
            EnvironmentSpawner e = go.AddComponent<EnvironmentSpawner>();
            e.lv = level;
            e.pool = pooler;
            e.focus = focusTarget;
            e.peaceful = peacefulMode;
            e.root = new GameObject("World").transform;
            e.nextZ = -L;
            e.safeRows = 10;
            e.Prime();
            return e;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        float FocusZ { get { return focus != null ? focus.position.z : 0f; } }

        void Prime()
        {
            while (nextZ < FocusZ + spawnAhead) BuildSection();
        }

        void Update()
        {
            float fz = FocusZ;
            while (nextZ < fz + spawnAhead) BuildSection();
            while (active.Count > 0 && active[0].EndZ < fz - despawnBehind)
            {
                TrackSection s = active[0];
                active.RemoveAt(0);
                pool.Release(s.gameObject);
            }
        }

        public void ShiftOrigin(float dz)
        {
            for (int i = 0; i < active.Count; i++)
            {
                Transform t = active[i].transform;
                t.position = new Vector3(0f, 0f, t.position.z - dz);
            }
            nextZ -= dz;
        }

        public bool GroundAt(float x, float z, out bool water)
        {
            water = false;
            int lane = GameConstants.NearestLane(x);
            for (int i = 0; i < active.Count; i++)
            {
                TrackSection s = active[i];
                if (z >= s.StartZ && z < s.EndZ) return s.GroundAt(lane, z, out water);
            }
            return true;
        }

        // ------------------------------------------------------------------ building

        void BuildSection()
        {
            GameObject sgo = pool.Get(Pieces.Section, root);
            TrackSection sec = sgo.GetComponent<TrackSection>();
            sgo.transform.position = new Vector3(0f, 0f, nextZ);
            sgo.transform.rotation = Quaternion.identity;
            sgo.transform.localScale = Vector3.one;

            float diff = GameManager.Instance != null ? GameManager.Instance.Difficulty01 : 0f;
            for (int r = 0; r < GameConstants.RowsPerSection; r++) BuildRow(sec, r, diff);
            BuildGround(sec);
            BuildDecor(sec);
            active.Add(sec);
            nextZ += L;
        }

        GameObject Place(TrackSection sec, string key, Vector3 pos, float yaw, Vector3 scale)
        {
            return PlaceRot(sec, key, pos, Quaternion.Euler(0f, yaw, 0f), scale);
        }

        GameObject PlaceRot(TrackSection sec, string key, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            GameObject go = pool.Get(key, sec.transform);
            Transform t = go.transform;
            t.localPosition = pos;
            t.localRotation = rot;
            t.localScale = scale;
            sec.pieces.Add(go);
            return go;
        }

        void PlaceCoin(TrackSection sec, int lane, float y, float z)
        {
            Place(sec, Pieces.Coin, new Vector3(GameConstants.LaneX(lane), y, z), 0f, Vector3.one);
        }

        static int Bits(int m)
        {
            int c = 0;
            for (int i = 0; i < 3; i++) if ((m & (1 << i)) != 0) c++;
            return c;
        }

        static int LaneMask(int minC, int maxC)
        {
            int count = Random.Range(minC, maxC + 1);
            int mask = 0;
            while (Bits(mask) < count) mask |= 1 << Random.Range(0, 3);
            return mask;
        }

        static int FreeLane(int blockedMask)
        {
            int[] free = new int[3];
            int n = 0;
            for (int i = 0; i < 3; i++) if ((blockedMask & (1 << i)) == 0) free[n++] = i;
            return n == 0 ? 1 : free[Random.Range(0, n)];
        }

        void BuildRow(TrackSection sec, int r, float diff)
        {
            float z0 = r * RowLen;
            float zc = z0 + RowLen * 0.5f;
            distSincePU += RowLen;
            if (peaceful) return;

            if (safeRows > 0) { safeRows--; SafeRow(sec, z0, zc, false); return; }
            if (cooldown > 0) { cooldown--; SafeRow(sec, z0, zc, true); return; }

            float chance = Mathf.Clamp(lv.obstacleChance + 0.2f * diff, 0.1f, 0.88f);
            if (Random.value > chance) { SafeRow(sec, z0, zc, true); return; }
            Hazard(sec, r, z0, zc, diff);
        }

        void SafeRow(TrackSection sec, float z0, float zc, bool allowPowerUp)
        {
            if (allowPowerUp && distSincePU > nextPU)
            {
                distSincePU = 0f;
                nextPU = Random.Range(220f, 380f);
                PlacePowerUp(sec, zc);
                return;
            }
            if (Random.value > lv.coinChance) return;
            int pat = Random.Range(0, 4);
            if (pat == 0)
            {
                int lane = Random.Range(0, 3);
                for (int i = 0; i < 6; i++) PlaceCoin(sec, lane, 1f, z0 + 1.2f + i * 1.9f);
            }
            else if (pat == 1)
            {
                for (int k = 0; k < 3; k++)
                    for (int l = 0; l < 3; l++) PlaceCoin(sec, l, 1f, z0 + 2.5f + k * 3.5f);
            }
            else if (pat == 2)
            {
                int[] seq = new int[] { 0, 1, 2, 1, 0, 1, 2 };
                for (int i = 0; i < seq.Length; i++) PlaceCoin(sec, seq[i], 1f, z0 + 1f + i * 1.6f);
            }
            else
            {
                int a = Random.Range(0, 2);
                for (int i = 0; i < 5; i++)
                {
                    PlaceCoin(sec, a, 1f, z0 + 1.5f + i * 2f);
                    PlaceCoin(sec, a + 1, 1f, z0 + 1.5f + i * 2f);
                }
            }
        }

        void PlacePowerUp(TrackSection sec, float zc)
        {
            PowerUpConfig[] cfg = ConfigDatabase.PowerUps;
            float sum = 0f;
            for (int i = 0; i < cfg.Length; i++) sum += cfg[i].spawnWeight;
            float r = Random.value * sum;
            PowerUpType t = PowerUpType.Magnet;
            for (int i = 0; i < cfg.Length; i++)
            {
                r -= cfg[i].spawnWeight;
                if (r <= 0f) { t = cfg[i].type; break; }
            }
            int lane = Random.Range(0, 3);
            Place(sec, Pieces.PowerUp(t), new Vector3(GameConstants.LaneX(lane), 1.2f, zc), 0f, Vector3.one);
        }

        void CoinLine(TrackSection sec, int lane, float z0)
        {
            if (Random.value > lv.coinChance) return;
            for (int i = 0; i < 5; i++) PlaceCoin(sec, lane, 1f, z0 + 1.5f + i * 2.2f);
        }

        void CoinArc(TrackSection sec, int lane, float zc)
        {
            for (int i = 0; i < 6; i++)
            {
                float t = (i - 2.5f) / 2.5f;
                PlaceCoin(sec, lane, 0.9f + 1.6f * (1f - t * t), zc + t * 4f);
            }
        }

        static ObstacleType Pick(float[] w)
        {
            float sum = 0f;
            for (int i = 0; i < w.Length; i++) sum += w[i];
            if (sum <= 0f) return ObstacleType.Log;
            float r = Random.value * sum;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r <= 0f) return (ObstacleType)i;
            }
            return ObstacleType.Log;
        }

        void Hazard(TrackSection sec, int r, float z0, float zc, float diff)
        {
            float[] w = new float[] { lv.wLog, lv.wRock, lv.wBarricade, lv.wHanging, lv.wPit, lv.wRiver, lv.wSpike, lv.wBoulder, lv.wTempleWall, lv.wTempleGate };

            if (diff > 0.25f && Random.value < 0.3f)
            {
                MixedRow(sec, z0, zc, w);
                cooldown = 1;
                return;
            }

            ObstacleType t = Pick(w);
            int mask;
            int baseCooldown = lv.difficulty <= 2 ? 1 : (Random.value < 0.55f - 0.3f * diff ? 1 : 0);
            cooldown = baseCooldown;

            switch (t)
            {
                case ObstacleType.Log:
                case ObstacleType.Barricade:
                case ObstacleType.Spike:
                case ObstacleType.Hanging:
                    mask = Random.value < 0.3f ? 7 : LaneMask(1, 2);
                    for (int l = 0; l < 3; l++)
                    {
                        if ((mask & (1 << l)) == 0) continue;
                        Place(sec, Pieces.ForObstacle(t), new Vector3(GameConstants.LaneX(l), 0f, zc), 0f, Vector3.one);
                        if (t != ObstacleType.Hanging && Random.value < 0.5f) CoinArc(sec, l, zc);
                    }
                    if (mask == 7) cooldown = 1; else CoinLine(sec, FreeLane(mask), z0);
                    break;

                case ObstacleType.Rock:
                    mask = LaneMask(1, 2);
                    for (int l = 0; l < 3; l++)
                        if ((mask & (1 << l)) != 0)
                            Place(sec, Pieces.Rock, new Vector3(GameConstants.LaneX(l), 0f, zc), Random.Range(0f, 360f), Vector3.one);
                    CoinLine(sec, FreeLane(mask), z0);
                    break;

                case ObstacleType.TempleWall:
                    mask = LaneMask(2, 2);
                    for (int l = 0; l < 3; l++)
                        if ((mask & (1 << l)) != 0)
                            Place(sec, Pieces.TempleWall, new Vector3(GameConstants.LaneX(l), 0f, zc), 0f, Vector3.one);
                    CoinLine(sec, FreeLane(mask), z0);
                    break;

                case ObstacleType.TempleGate:
                    Place(sec, Pieces.TempleGate, new Vector3(0f, 0f, zc), 0f, Vector3.one);
                    for (int i = 0; i < 4; i++) PlaceCoin(sec, 1, 0.5f, z0 + 3.2f + i * 1.8f);
                    cooldown = 1;
                    break;

                case ObstacleType.Boulder:
                    {
                        int lane = Random.Range(0, 3);
                        Place(sec, Pieces.Boulder, new Vector3(GameConstants.LaneX(lane), 0f, zc + 2f), 0f, Vector3.one);
                        CoinLine(sec, FreeLane(1 << lane), z0);
                        cooldown = 1;
                    }
                    break;

                case ObstacleType.Pit:
                    mask = Random.value < 0.3f ? 7 : LaneMask(1, 2);
                    for (int l = 0; l < 3; l++)
                    {
                        if ((mask & (1 << l)) == 0) continue;
                        TrackSection.Hole h = new TrackSection.Hole();
                        h.a = z0 + 3f; h.b = z0 + 9f; h.water = false; h.visualOnly = false;
                        sec.holes[l].Add(h);
                        if (Random.value < 0.6f) CoinArc(sec, l, zc);
                    }
                    if (mask != 7) CoinLine(sec, FreeLane(mask), z0);
                    cooldown = 1;
                    break;

                case ObstacleType.River:
                    {
                        int bridges = diff > 0.5f ? 1 : Random.Range(1, 3);
                        int bm = LaneMask(bridges, bridges);
                        sec.rowIsRiver[r] = true;
                        for (int l = 0; l < 3; l++)
                        {
                            TrackSection.Hole h = new TrackSection.Hole();
                            h.a = z0 + 2f; h.b = z0 + 10f; h.water = true;
                            h.visualOnly = (bm & (1 << l)) != 0;
                            sec.holes[l].Add(h);
                            if (h.visualOnly)
                            {
                                Place(sec, Pieces.Bridge, new Vector3(GameConstants.LaneX(l), 0f, z0 + 2f), 0f, Vector3.one);
                                if (Random.value < lv.coinChance)
                                    for (int i = 0; i < 4; i++) PlaceCoin(sec, l, 1f, z0 + 3.2f + i * 1.8f);
                            }
                        }
                        Place(sec, Pieces.Water, new Vector3(0f, 0f, z0 + 6f), 0f, new Vector3(100f, 1f, 8f));
                        cooldown = 1;
                    }
                    break;
            }
        }

        void MixedRow(TrackSection sec, float z0, float zc, float[] w)
        {
            List<ObstacleType> opts = new List<ObstacleType>();
            ObstacleType[] single = new ObstacleType[] { ObstacleType.Log, ObstacleType.Rock, ObstacleType.Barricade, ObstacleType.Hanging, ObstacleType.Spike };
            for (int i = 0; i < single.Length; i++) if (w[(int)single[i]] > 0f) opts.Add(single[i]);
            if (opts.Count == 0) opts.Add(ObstacleType.Log);
            int free = Random.Range(0, 3);
            for (int l = 0; l < 3; l++)
            {
                if (l == free) continue;
                ObstacleType t = opts[Random.Range(0, opts.Count)];
                Place(sec, Pieces.ForObstacle(t), new Vector3(GameConstants.LaneX(l), 0f, zc), 0f, Vector3.one);
            }
            CoinLine(sec, free, z0);
        }

        // ------------------------------------------------------------------ ground & decor

        void BuildGround(TrackSection sec)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                List<TrackSection.Hole> hs = sec.holes[lane];
                hs.Sort(delegate (TrackSection.Hole p, TrackSection.Hole q) { return p.a.CompareTo(q.a); });
                float cur = 0f;
                for (int i = 0; i < hs.Count; i++)
                {
                    TrackSection.Hole h = hs[i];
                    if (h.a > cur + 0.01f) GroundSeg(sec, lane, cur, h.a);
                    if (!h.water && !h.visualOnly)
                        Place(sec, Pieces.PitLiner, new Vector3(GameConstants.LaneX(lane), -3.2f, (h.a + h.b) * 0.5f), 0f, new Vector3(GameConstants.LaneWidth, 0.2f, h.b - h.a));
                    if (h.b > cur) cur = h.b;
                }
                if (cur < L - 0.01f) GroundSeg(sec, lane, cur, L);
            }
            for (int r = 0; r < GameConstants.RowsPerSection; r++)
            {
                float z0 = r * RowLen;
                if (sec.rowIsRiver[r]) { SideSeg(sec, z0, z0 + 2f); SideSeg(sec, z0 + 10f, z0 + RowLen); }
                else SideSeg(sec, z0, z0 + RowLen);
            }
        }

        void GroundSeg(TrackSection sec, int lane, float a, float b)
        {
            Place(sec, Pieces.GroundLane, new Vector3(GameConstants.LaneX(lane), -0.5f, (a + b) * 0.5f), 0f,
                new Vector3(GameConstants.LaneWidth, 1f, b - a));
        }

        void SideSeg(TrackSection sec, float a, float b)
        {
            float edge = GameConstants.LaneWidth * 1.5f;
            for (int s = -1; s <= 1; s += 2)
                Place(sec, Pieces.GroundSide, new Vector3(s * (edge + 20f), -0.65f, (a + b) * 0.5f), 0f, new Vector3(40f, 1f, b - a));
        }

        void BuildDecor(TrackSection sec)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < lv.bambooPerSide; i++)
                {
                    float z = Random.Range(0f, L);
                    if (sec.InRiverZone(z)) continue;
                    float x = side * (4.8f + Mathf.Pow(Random.value, 1.6f) * 18f);
                    Place(sec, Pieces.Bamboo, new Vector3(x, -0.2f, z), Random.Range(0f, 360f), Vector3.one * Random.Range(0.8f, 1.35f));
                }
                ScatterDecor(sec, side, Pieces.Bush, 5, 4.4f, 9f, 0.8f, 1.5f);
                ScatterDecor(sec, side, Pieces.Grass, 9, 4.0f, 8f, 0.8f, 1.4f);
                ScatterDecor(sec, side, Pieces.DecorRock, 2, 5f, 10f, 0.8f, 1.6f);
                ScatterDecor(sec, side, Pieces.DecorRock, 4, 3.95f, 4.4f, 0.35f, 0.6f);
                if (lv.glowPlants) ScatterDecor(sec, side, Pieces.GlowPlant, 6, 4.2f, 9f, 0.8f, 1.5f);
                else ScatterDecor(sec, side, Pieces.Flower, 3, 4.2f, 8f, 0.9f, 1.4f);
            }

            if (Random.value < lv.vineArchChance)
            {
                float z = Random.Range(10f, 38f);
                if (!sec.InRiverZone(z)) Place(sec, Pieces.VineArch, new Vector3(0f, -0.1f, z), 0f, Vector3.one);
            }
            if (Random.value < lv.templeChance)
            {
                float z = Random.Range(10f, 36f);
                int side = Random.value < 0.5f ? -1 : 1;
                if (!sec.InRiverZone(z))
                    Place(sec, Pieces.Temple, new Vector3(side * Random.Range(11.5f, 14f), -0.15f, z), side > 0 ? 90f : -90f, Vector3.one);
            }
            if (lv.hasWaterfall)
            {
                for (int r = 0; r < GameConstants.RowsPerSection; r++)
                {
                    if (!sec.rowIsRiver[r]) continue;
                    int side = Random.value < 0.5f ? -1 : 1;
                    Place(sec, Pieces.Waterfall, new Vector3(side * Random.Range(13f, 15f), -0.15f, r * RowLen + 6f), side > 0 ? 90f : -90f, Vector3.one);
                    break;
                }
            }
            if (!lv.night && Random.value < 0.7f)
            {
                int side = Random.value < 0.5f ? -1 : 1;
                Quaternion rot = Quaternion.Euler(0f, 0f, side * Random.Range(10f, 22f));
                PlaceRot(sec, Pieces.SunRay, new Vector3(side * Random.Range(3f, 9f), 10f, Random.Range(4f, 44f)), rot, new Vector3(3.5f, 24f, 1f));
            }
        }

        void ScatterDecor(TrackSection sec, int side, string key, int count, float xMin, float xMax, float sMin, float sMax)
        {
            for (int i = 0; i < count; i++)
            {
                float z = Random.Range(0f, L);
                if (sec.InRiverZone(z)) continue;
                float x = side * Random.Range(xMin, xMax);
                Place(sec, key, new Vector3(x, -0.15f, z), Random.Range(0f, 360f), Vector3.one * Random.Range(sMin, sMax));
            }
        }
    }
}
