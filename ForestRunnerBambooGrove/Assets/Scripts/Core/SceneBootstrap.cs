using UnityEngine;
using UnityEngine.Rendering;

namespace ForestRunner
{
    /// <summary>
    /// The only component stored in the .unity scene files. On Awake it constructs the entire
    /// scene content procedurally (camera, lights, sky, environment, player, UI, managers).
    /// This guarantees there are no missing references in the scene files.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        public enum Mode { MainMenu = 0, Gameplay = 1 }
        public Mode mode = Mode.MainMenu;

        void Awake()
        {
            int q = SaveSystem.QualityLevel;
            if (q >= 0 && q < QualitySettings.names.Length) QualitySettings.SetQualityLevel(q, true);
            if (mode == Mode.MainMenu) BuildMenu(); else BuildGameplay();
        }

        // ------------------------------------------------------------------ main menu

        static void BuildMenu()
        {
            Time.timeScale = 1f;
            AudioManager am = AudioManager.Ensure();
            int idx = LevelManager.SafeSelectedLevel();
            LevelConfig lv = ConfigDatabase.GetLevel(idx);

            GameObject systems = new GameObject("_Systems");
            ObjectPooler pool = systems.AddComponent<ObjectPooler>();
            MaterialLibrary mats = MaterialLibrary.Create(lv);
            PieceFactory.RegisterAll(pool, mats);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 520f;
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();
            LevelManager.ApplyAtmosphere(lv, cam);
            ParticleManager.CreateAmbient(camGo.transform, lv, mats);

            GameObject runner = new GameObject("MenuRunner");
            CharacterVisual cv = CharacterVisual.Create(runner.transform);
            cv.ApplySkin(ConfigDatabase.GetSkin(SaveSystem.SelectedSkin));
            cv.SetState(AnimState.Run, false);
            cv.SetRunSpeed(18f);

            EnvironmentSpawner env = EnvironmentSpawner.Create(lv, pool, null, true);
            MainMenuManager.Create(cam, env, cv);
            am.StartLevelAudio(lv.night, idx);
            am.StopRunLoop();
        }

        // ------------------------------------------------------------------ gameplay

        static void BuildGameplay()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.01f;
            Physics.autoSyncTransforms = true;
            // Only Player<->Obstacle and Player<->Collectible interact; everything else is ignored.
            int[] layers = new int[] { GameConstants.LayerPlayer, GameConstants.LayerObstacle, GameConstants.LayerCollectible, GameConstants.LayerGround };
            for (int a = 0; a < layers.Length; a++)
                for (int b = a; b < layers.Length; b++)
                {
                    bool keep = layers[a] == GameConstants.LayerPlayer &&
                                (layers[b] == GameConstants.LayerObstacle || layers[b] == GameConstants.LayerCollectible);
                    if (!keep) Physics.IgnoreLayerCollision(layers[a], layers[b], true);
                }

            AudioManager am = AudioManager.Ensure();
            int idx = LevelManager.SafeSelectedLevel();
            LevelConfig lv = ConfigDatabase.GetLevel(idx);

            GameObject systems = new GameObject("_Systems");
            ObjectPooler pool = systems.AddComponent<ObjectPooler>();
            MaterialLibrary mats = MaterialLibrary.Create(lv);
            PieceFactory.RegisterAll(pool, mats);
            systems.AddComponent<PowerUpManager>();
            systems.AddComponent<CoinManager>();
            ParticleManager.Create(mats);
            GameManager gm = systems.AddComponent<GameManager>();

            CameraController cc = CameraController.Create(mats);
            LevelManager.ApplyAtmosphere(lv, cc.Cam);
            ParticleManager.CreateAmbient(cc.transform, lv, mats);

            PlayerController pc = PlayerController.Create(ConfigDatabase.GetSkin(SaveSystem.SelectedSkin), mats);
            cc.Init(pc.transform);
            EnvironmentSpawner.Create(lv, pool, pc.transform, false);

            UIManager ui = UIManager.Create();
            PauseManager.Create(ui.Canvas);
            GameOverManager.Create(ui.Canvas);

            gm.Begin(lv, idx);
            am.StartLevelAudio(lv.night, idx);
        }
    }
}
