#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForestRunner.EditorTools
{
    /// <summary>
    /// Editor conveniences: registers the scenes in Build Settings, makes sure the shaders used at
    /// runtime are included in standalone builds, and offers menu items to bake the procedural
    /// pieces to prefab assets and regenerate the bootstrap scenes.
    /// </summary>
    [InitializeOnLoad]
    public static class ForestRunnerEditorSetup
    {
        const string MenuScene = "Assets/Scenes/MainMenu.unity";
        const string GameScene = "Assets/Scenes/Gameplay.unity";

        static ForestRunnerEditorSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            try
            {
                EnsureBuildSettings();
                EnsureShadersIncluded();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ForestRunner] Auto setup skipped: " + e.Message);
            }
        }

        [MenuItem("Forest Runner/Setup Project (Build Settings + Shaders)")]
        public static void SetupMenu()
        {
            EnsureBuildSettings();
            EnsureShadersIncluded();
            Debug.Log("[ForestRunner] Build settings and shader list are set up.");
        }

        static void EnsureBuildSettings()
        {
            string[] paths = new string[] { MenuScene, GameScene };
            EditorBuildSettingsScene[] cur = EditorBuildSettings.scenes;
            if (cur.Length >= 2 && cur[0].path == paths[0] && cur[1].path == paths[1]) return;
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
            for (int i = 0; i < paths.Length; i++)
                if (File.Exists(paths[i])) list.Add(new EditorBuildSettingsScene(paths[i], true));
            if (list.Count > 0) EditorBuildSettings.scenes = list.ToArray();
        }

        static void EnsureShadersIncluded()
        {
            UnityEngine.Object gs = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            string[] names = new string[]
            {
                "Standard", "Unlit/Color", "Legacy Shaders/Diffuse", "Legacy Shaders/Transparent/Diffuse",
                "Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended",
                "Sprites/Default", "UI/Default"
            };
            SerializedObject so = new SerializedObject(gs);
            SerializedProperty arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;
            bool changed = false;
            for (int n = 0; n < names.Length; n++)
            {
                Shader sh = Shader.Find(names[n]);
                if (sh == null) continue;
                bool found = false;
                for (int i = 0; i < arr.arraySize; i++)
                    if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { found = true; break; }
                if (found) continue;
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Forest Runner/Regenerate Scene Files")]
        public static void RegenerateScenes()
        {
            CreateScene(MenuScene, SceneBootstrap.Mode.MainMenu);
            CreateScene(GameScene, SceneBootstrap.Mode.Gameplay);
            EnsureBuildSettings();
            AssetDatabase.Refresh();
            Debug.Log("[ForestRunner] Scenes regenerated.");
        }

        static void CreateScene(string path, SceneBootstrap.Mode mode)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("Bootstrap");
            SceneBootstrap sb = go.AddComponent<SceneBootstrap>();
            sb.mode = mode;
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
        }

        [MenuItem("Forest Runner/Open Main Menu Scene")]
        public static void OpenMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(MenuScene);
        }

        [MenuItem("Forest Runner/Clear Saved Progress (PlayerPrefs)")]
        public static void ClearProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[ForestRunner] PlayerPrefs cleared.");
        }

        // ------------------------------------------------------------------ prefab baking

        [MenuItem("Forest Runner/Bake Procedural Pieces To Prefabs")]
        public static void BakePrefabs()
        {
            LevelConfig lv = ConfigDatabase.GetLevel(0);
            MaterialLibrary mats = MaterialLibrary.Create(lv);
            Dictionary<string, Func<GameObject>> factories = PieceFactory.BuildFactories(mats);

            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Materials/Generated");
            Directory.CreateDirectory("Assets/Models/Generated");
            Directory.CreateDirectory("Assets/Textures/Generated");

            Dictionary<UnityEngine.Object, bool> done = new Dictionary<UnityEngine.Object, bool>();
            int counter = 0;
            int count = 0;
            foreach (KeyValuePair<string, Func<GameObject>> kv in factories)
            {
                GameObject go = kv.Value();
                go.name = kv.Key;
                try
                {
                    MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>(true);
                    for (int i = 0; i < mfs.Length; i++)
                    {
                        Mesh m = mfs[i].sharedMesh;
                        if (m == null || AssetDatabase.Contains(m) || done.ContainsKey(m)) continue;
                        AssetDatabase.CreateAsset(m, "Assets/Models/Generated/" + Safe(m.name) + "_" + (counter++) + ".asset");
                        done[m] = true;
                    }
                    Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < rs.Length; i++)
                    {
                        Material[] ms = rs[i].sharedMaterials;
                        for (int k = 0; k < ms.Length; k++)
                        {
                            Material mat = ms[k];
                            if (mat == null || AssetDatabase.Contains(mat) || done.ContainsKey(mat)) continue;
                            Texture tex = mat.mainTexture;
                            if (tex != null && !AssetDatabase.Contains(tex) && !done.ContainsKey(tex))
                            {
                                AssetDatabase.CreateAsset(tex, "Assets/Textures/Generated/Tex_" + (counter++) + ".asset");
                                done[tex] = true;
                            }
                            AssetDatabase.CreateAsset(mat, "Assets/Materials/Generated/Mat_" + (counter++) + ".mat");
                            done[mat] = true;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/" + kv.Key + ".prefab");
                    count++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ForestRunner] Could not bake " + kv.Key + ": " + e.Message);
                }
                UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ForestRunner] Baked " + count + " prefabs into Assets/Prefabs (reference copies; the game builds pieces procedurally at runtime).");
        }

        static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "mesh";
            foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s;
        }
    }
}
#endif
