using UnityEngine;

namespace ForestRunner
{
    /// <summary>Central place for layer indices, lane geometry and tuning constants.</summary>
    public static class GameConstants
    {
        // Physics layers (names are declared in ProjectSettings/TagManager.asset)
        public const int LayerPlayer = 8;
        public const int LayerObstacle = 9;
        public const int LayerCollectible = 10;
        public const int LayerGround = 11;

        public const int LaneCount = 3;
        public const float LaneWidth = 2.6f;
        public const float RowLength = 12f;
        public const int RowsPerSection = 4;
        public const float SectionLength = RowLength * RowsPerSection; // 48 units

        public const string SceneMenu = "MainMenu";
        public const string SceneGameplay = "Gameplay";

        public const float JumpVelocity = 10.5f;
        public const float Gravity = -32f;
        public const float SlideDuration = 0.75f;
        public const float HoverHeight = 2.9f;
        public const float OriginShiftThreshold = 600f;

        public static float LaneX(int lane)
        {
            return (lane - 1) * LaneWidth;
        }

        public static int NearestLane(float x)
        {
            return Mathf.Clamp(Mathf.RoundToInt(x / LaneWidth) + 1, 0, LaneCount - 1);
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
