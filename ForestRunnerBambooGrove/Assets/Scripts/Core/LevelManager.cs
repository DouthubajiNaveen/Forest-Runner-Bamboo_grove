using UnityEngine;
using UnityEngine.Rendering;

namespace ForestRunner
{
    /// <summary>Applies a level's atmosphere (fog, lights, ambient, sky) and answers progression queries.</summary>
    public static class LevelManager
    {
        public static void ApplyAtmosphere(LevelConfig lv, Camera cam)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = lv.fogColor;
            RenderSettings.fogDensity = lv.fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = lv.ambientColor;
            RenderSettings.skybox = null;

            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = lv.lightColor;
            sun.intensity = lv.lightIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sunGo.transform.rotation = Quaternion.Euler(lv.sunPitch, lv.sunYaw, 0f);
            RenderSettings.sun = sun;

            GameObject fillGo = new GameObject("FillLight");
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Color.Lerp(lv.lightColor, lv.ambientColor, 0.5f);
            fill.intensity = lv.night ? 0.25f : 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(28f, 10f, 0f);

            QualitySettings.shadowDistance = 45f;

            cam.backgroundColor = lv.fogColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            SkyDome.Create(lv, cam.transform);
        }

        public static int HighestUnlocked
        {
            get { return Mathf.Clamp(SaveSystem.UnlockedLevel, 0, ConfigDatabase.Levels.Length - 1); }
        }

        public static int SafeSelectedLevel()
        {
            int idx = Mathf.Clamp(SaveSystem.SelectedLevel, 0, ConfigDatabase.Levels.Length - 1);
            if (!SaveSystem.IsLevelUnlocked(idx)) idx = 0;
            return idx;
        }

        public static string UnlockHint(int levelIndex)
        {
            if (levelIndex <= 0) return "";
            LevelConfig prev = ConfigDatabase.GetLevel(levelIndex - 1);
            return "Run " + prev.distanceToUnlockNext + " m in Level " + levelIndex + " to unlock";
        }
    }
}
