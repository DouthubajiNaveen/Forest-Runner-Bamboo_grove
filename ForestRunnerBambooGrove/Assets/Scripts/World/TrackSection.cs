using System.Collections.Generic;
using UnityEngine;

namespace ForestRunner
{
    /// <summary>
    /// One 48-unit stretch of track. Owns the pooled pieces placed on it (ground, obstacles,
    /// coins, decor) and knows where holes (pits / rivers) are so the player can query ground.
    /// </summary>
    public class TrackSection : MonoBehaviour, IPoolable
    {
        public struct Hole
        {
            public float a, b;
            public bool water;
            public bool visualOnly; // gap in the ground mesh that has a bridge (still walkable)
        }

        public readonly List<GameObject> pieces = new List<GameObject>(160);
        public readonly List<Hole>[] holes = new List<Hole>[GameConstants.LaneCount];
        public readonly bool[] rowIsRiver = new bool[GameConstants.RowsPerSection];

        void Awake()
        {
            for (int i = 0; i < holes.Length; i++) holes[i] = new List<Hole>(4);
        }

        public float StartZ { get { return transform.position.z; } }
        public float EndZ { get { return transform.position.z + GameConstants.SectionLength; } }

        public void OnPoolGet()
        {
            if (holes[0] == null) Awake();
            for (int i = 0; i < holes.Length; i++) holes[i].Clear();
            for (int i = 0; i < rowIsRiver.Length; i++) rowIsRiver[i] = false;
            pieces.Clear();
        }

        public void OnPoolRelease()
        {
            ObjectPooler pool = ObjectPooler.Instance;
            if (pool != null)
            {
                for (int i = 0; i < pieces.Count; i++) pool.Release(pieces[i]);
            }
            pieces.Clear();
        }

        public bool InRiverZone(float localZ)
        {
            for (int r = 0; r < rowIsRiver.Length; r++)
            {
                if (!rowIsRiver[r]) continue;
                float z0 = r * GameConstants.RowLength;
                if (localZ > z0 && localZ < z0 + GameConstants.RowLength) return true;
            }
            return false;
        }

        /// <summary>True if there is walkable ground in the lane at the given world z.</summary>
        public bool GroundAt(int lane, float worldZ, out bool water)
        {
            water = false;
            float lz = worldZ - transform.position.z;
            List<Hole> list = holes[lane];
            for (int i = 0; i < list.Count; i++)
            {
                Hole h = list[i];
                if (h.visualOnly) continue;
                if (lz > h.a + 0.35f && lz < h.b - 0.35f)
                {
                    water = h.water;
                    return false;
                }
            }
            return true;
        }
    }
}
