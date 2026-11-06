import os, textwrap
from common import *

def w(rel, text):
    p = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, "w").write(text)

# ---------- placeholder READMEs for asset folders ----------
notes = {
 "Assets/Audio/README.txt": "AUDIO\nAll sounds are synthesised at start-up by Scripts/Audio/AudioManager.cs, so no audio files are required.\nTo replace a sound with a real recording, place an AudioClip in Assets/Resources/Audio/ using one of these names:\n  click, coin, jump, slide, swish, land, powerup, shield_break, hit, gameover, splash, unlock, buy, denied,\n  footsteps (loop), bird0..bird3, ambience_day (loop), ambience_night (loop), music_day (loop), music_night (loop)\nA clip with a matching name overrides the generated one automatically.\n",
 "Assets/Resources/Audio/README.txt": "Drop replacement AudioClips here (see Assets/Audio/README.txt for the list of names).\n",
 "Assets/Models/README.txt": "MODELS\nNo external models are used. Every mesh (cube, cylinder, sphere, capsule, bamboo, rocks, temple, bridge ...) is generated in code\nby Scripts/Utility/ProcMesh.cs, MeshKit.cs and Scripts/World/PieceFactory.cs.\nMenu: Forest Runner > Bake Procedural Pieces To Prefabs writes generated meshes to Assets/Models/Generated.\n",
 "Assets/Animations/README.txt": "ANIMATIONS\nThe runner is animated procedurally in Scripts/Player/CharacterVisual.cs (run cycle, jump, slide, hover glide, death tumble).\nNo AnimationClips / AnimatorControllers are required.\n",
 "Assets/Particles/README.txt": "PARTICLES\nAll particle systems are created in code by Scripts/FX/ParticleManager.cs (coin sparkle, power-up burst, collision, shield break,\nsplash, dust, smash, speed lines, ambient pollen / fireflies, boost trail) and PieceFactory.cs (waterfall mist).\n",
 "Assets/UI/README.txt": "UI\nAll screens (main menu, level select, shop, settings, HUD, pause, game over) are built in code by Scripts/UI/*.cs using uGUI.\nThe rounded-rectangle sprite is generated at runtime (UIKit.Round).\n",
 "Assets/Textures/README.txt": "TEXTURES\nTextures (ground noise, stone, soft particle circle, light beam) are generated at runtime by Scripts/Utility/ProcTex.cs.\nMenu: Forest Runner > Bake Procedural Pieces To Prefabs also saves them to Assets/Textures/Generated.\n",
 "Assets/Prefabs/README.txt": "PREFABS\nWorld pieces are built procedurally and pooled at runtime (Scripts/World/PieceFactory.cs).\nRun the menu item  Forest Runner > Bake Procedural Pieces To Prefabs  to write reference prefabs for every piece\n(obstacles, coins, power-ups, bamboo, temple ...) into this folder.\n",
 "Assets/Materials/README.txt": "MATERIALS\nMaterials are created at runtime by Scripts/Utility/MaterialLibrary.cs from the active LevelConfig colours (built-in shaders only).\nBaked copies appear in Assets/Materials/Generated after running  Forest Runner > Bake Procedural Pieces To Prefabs.\n",
 "Assets/ScriptableObjects/README.txt": "SCRIPTABLE OBJECTS\nLevels/    Level1..Level5 (LevelConfig)\nPowerUps/  Magnet, Shield, Speed Boost, Double Coins, Hover (PowerUpConfig)\nSkins/     5 character skins (SkinConfig)\nAssets/Resources/GameConfig.asset references all of them. Edit the assets in the Inspector to tune the game.\n",
}
for k, v in notes.items(): w(k, v)

# ---------- scenes ----------
def scene(mode):
    sg = script_guid("SceneBootstrap")
    return textwrap.dedent("""\
    %YAML 1.1
    %TAG !u! tag:unity3d.com,2011:
    --- !u!1 &1000
    GameObject:
      m_ObjectHideFlags: 0
      m_CorrespondingSourceObject: {{fileID: 0}}
      m_PrefabInstance: {{fileID: 0}}
      m_PrefabAsset: {{fileID: 0}}
      serializedVersion: 6
      m_Component:
      - component: {{fileID: 1001}}
      - component: {{fileID: 1002}}
      m_Layer: 0
      m_Name: Bootstrap
      m_TagString: Untagged
      m_Icon: {{fileID: 0}}
      m_NavMeshLayer: 0
      m_StaticEditorFlags: 0
      m_IsActive: 1
    --- !u!4 &1001
    Transform:
      m_ObjectHideFlags: 0
      m_CorrespondingSourceObject: {{fileID: 0}}
      m_PrefabInstance: {{fileID: 0}}
      m_PrefabAsset: {{fileID: 0}}
      m_GameObject: {{fileID: 1000}}
      m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
      m_LocalPosition: {{x: 0, y: 0, z: 0}}
      m_LocalScale: {{x: 1, y: 1, z: 1}}
      m_Children: []
      m_Father: {{fileID: 0}}
      m_RootOrder: 0
      m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
    --- !u!114 &1002
    MonoBehaviour:
      m_ObjectHideFlags: 0
      m_CorrespondingSourceObject: {{fileID: 0}}
      m_PrefabInstance: {{fileID: 0}}
      m_PrefabAsset: {{fileID: 0}}
      m_GameObject: {{fileID: 1000}}
      m_Enabled: 1
      m_EditorHideFlags: 0
      m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
      m_Name: 
      m_EditorClassIdentifier: 
      mode: {mode}
    """).format(guid=sg, mode=mode)
w("Assets/Scenes/MainMenu.unity", scene(0))
w("Assets/Scenes/Gameplay.unity", scene(1))

# ---------- project settings ----------
w("ProjectSettings/ProjectVersion.txt", "m_EditorVersion: 2022.3.20f1\n")
w("Packages/manifest.json", """{
  "dependencies": {
    "com.unity.ugui": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0"
  }
}
""")
layers = ["Default","TransparentFX","Ignore Raycast","","Water","UI","","","Player","Obstacle","Collectible","Ground"] + [""]*20
w("ProjectSettings/TagManager.asset", "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!78 &1\nTagManager:\n  serializedVersion: 2\n  tags: []\n  layers:\n" +
  "".join("  - %s\n" % l if l else "  - \n" for l in layers) +
  "  m_SortingLayers:\n  - name: Default\n    uniqueID: 0\n    locked: 0\n")
ebs = ("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!1045 &1\nEditorBuildSettings:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Scenes:\n"
  "  - enabled: 1\n    path: Assets/Scenes/MainMenu.unity\n    guid: GUID_A\n  - enabled: 1\n    path: Assets/Scenes/Gameplay.unity\n    guid: GUID_B\n  m_configObjects: {}\n")
ebs = ebs.replace("GUID_A", guid_for("Assets/Scenes/MainMenu.unity")).replace("GUID_B", guid_for("Assets/Scenes/Gameplay.unity"))
w("ProjectSettings/EditorBuildSettings.asset", ebs)
w("ProjectSettings/ProjectSettings.asset", """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!129 &1
PlayerSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 26
  productGUID: 5f0b6c2a7d3e4a19b8c4d1e2f3a40516
  companyName: ForestRunnerStudio
  productName: Forest Runner - Bamboo Grove
  defaultScreenWidth: 1920
  defaultScreenHeight: 1080
  fullscreenMode: 1
  runInBackground: 1
  colorSpace: 1
  activeInputHandler: 0
  allowFullscreenSwitch: 1
  resizableWindow: 1
  useFlipModelSwapchain: 1
  bundleVersion: 1.0.0
""")

w(".gitignore", "[Ll]ibrary/\n[Tt]emp/\n[Oo]bj/\n[Bb]uild/\n[Bb]uilds/\n[Ll]ogs/\n[Uu]ser[Ss]ettings/\n*.csproj\n*.sln\n*.suo\n*.user\n.vs/\n.idea/\n")

# ---------- .meta files ----------
def meta_for(rel, is_dir):
    g = guid_for(rel)
    if is_dir:
        body = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif rel.endswith(".cs"):
        body = "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif rel.endswith(".shader"):
        body = "ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif rel.endswith(".asset"):
        body = "NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif rel.endswith(".unity"):
        body = "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        body = "TextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    return "fileFormatVersion: 2\nguid: %s\n%s" % (g, body)

count = 0
assets = os.path.join(ROOT, "Assets")
for dp, dn, fn in os.walk(assets):
    for d in dn:
        rel = os.path.relpath(os.path.join(dp, d), ROOT).replace(os.sep, "/")
        open(os.path.join(ROOT, rel + ".meta"), "w").write(meta_for(rel, True)); count += 1
    for f in fn:
        if f.endswith(".meta"): continue
        rel = os.path.relpath(os.path.join(dp, f), ROOT).replace(os.sep, "/")
        open(os.path.join(ROOT, rel + ".meta"), "w").write(meta_for(rel, False)); count += 1
print("meta files:", count)
