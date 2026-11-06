import os
from common import *

def C(r, g, b, a=1.0): return (r, g, b, a)

LEVELS = [
 dict(levelIndex=0, levelName="Bamboo Entrance", description="A gentle path into the grove. Basic obstacles and a slow pace.",
  difficulty=1, distanceToUnlockNext=800, baseSpeed=15.0, maxSpeed=26.0, acceleration=0.10,
  obstacleChance=0.42, coinChance=0.80, wLog=3.0, wRock=2.5, wBarricade=2.0, wHanging=0.8, wPit=0.0, wRiver=0.0, wSpike=0.0, wBoulder=0.0, wTempleWall=0.0, wTempleGate=0.0,
  bambooPerSide=9, templeChance=0.04, vineArchChance=0.30, hasWaterfall=False, night=False, glowPlants=False,
  skyTop=C(0.25,0.55,0.92), skyHorizon=C(0.80,0.92,0.86), fogColor=C(0.74,0.88,0.82), fogDensity=0.011,
  ambientColor=C(0.52,0.62,0.56), lightColor=C(1.0,0.96,0.85), lightIntensity=1.15, sunPitch=42.0, sunYaw=150.0,
  pathColor=C(0.64,0.52,0.35), grassColor=C(0.30,0.55,0.22), bambooColor=C(0.44,0.72,0.25), leafColor=C(0.34,0.65,0.22),
  stoneColor=C(0.55,0.55,0.52), waterColor=C(0.2,0.55,0.75,0.75), glowColor=C(0.5,1.0,0.7)),
 dict(levelIndex=1, levelName="Deep Bamboo Forest", description="Dense, misty bamboo. More obstacles, higher speed and richer coin trails.",
  difficulty=2, distanceToUnlockNext=1200, baseSpeed=17.0, maxSpeed=30.0, acceleration=0.12,
  obstacleChance=0.52, coinChance=0.85, wLog=3.0, wRock=2.5, wBarricade=2.5, wHanging=2.0, wPit=1.5, wRiver=0.0, wSpike=1.0, wBoulder=0.0, wTempleWall=0.0, wTempleGate=0.0,
  bambooPerSide=13, templeChance=0.06, vineArchChance=0.45, hasWaterfall=False, night=False, glowPlants=False,
  skyTop=C(0.18,0.42,0.52), skyHorizon=C(0.55,0.74,0.66), fogColor=C(0.45,0.64,0.56), fogDensity=0.022,
  ambientColor=C(0.32,0.46,0.40), lightColor=C(0.88,1.0,0.80), lightIntensity=0.95, sunPitch=55.0, sunYaw=160.0,
  pathColor=C(0.46,0.39,0.28), grassColor=C(0.16,0.40,0.15), bambooColor=C(0.30,0.62,0.20), leafColor=C(0.20,0.52,0.18),
  stoneColor=C(0.42,0.46,0.42), waterColor=C(0.15,0.45,0.55,0.75), glowColor=C(0.5,1.0,0.6)),
 dict(levelIndex=2, levelName="River Crossing", description="Golden-hour rivers and wooden bridges. Pick the right lane or jump the water.",
  difficulty=3, distanceToUnlockNext=1600, baseSpeed=19.0, maxSpeed=33.0, acceleration=0.14,
  obstacleChance=0.55, coinChance=0.85, wLog=2.0, wRock=2.0, wBarricade=2.0, wHanging=1.5, wPit=1.0, wRiver=3.5, wSpike=1.0, wBoulder=0.0, wTempleWall=0.0, wTempleGate=0.0,
  bambooPerSide=10, templeChance=0.05, vineArchChance=0.35, hasWaterfall=True, night=False, glowPlants=False,
  skyTop=C(0.33,0.33,0.62), skyHorizon=C(1.0,0.64,0.36), fogColor=C(0.95,0.66,0.46), fogDensity=0.014,
  ambientColor=C(0.56,0.42,0.42), lightColor=C(1.0,0.62,0.36), lightIntensity=1.2, sunPitch=14.0, sunYaw=170.0,
  pathColor=C(0.62,0.46,0.31), grassColor=C(0.36,0.46,0.20), bambooColor=C(0.56,0.70,0.25), leafColor=C(0.50,0.62,0.20),
  stoneColor=C(0.55,0.48,0.44), waterColor=C(0.25,0.50,0.70,0.78), glowColor=C(1.0,0.8,0.4)),
 dict(levelIndex=3, levelName="Ancient Forest Temple", description="Misty ruins with stone barriers, spike traps and rolling boulders.",
  difficulty=4, distanceToUnlockNext=2000, baseSpeed=21.0, maxSpeed=36.0, acceleration=0.16,
  obstacleChance=0.60, coinChance=0.85, wLog=1.5, wRock=1.5, wBarricade=1.5, wHanging=1.5, wPit=1.5, wRiver=0.6, wSpike=2.0, wBoulder=1.6, wTempleWall=2.6, wTempleGate=2.0,
  bambooPerSide=9, templeChance=0.40, vineArchChance=0.40, hasWaterfall=False, night=False, glowPlants=False,
  skyTop=C(0.30,0.24,0.46), skyHorizon=C(0.86,0.56,0.52), fogColor=C(0.64,0.50,0.55), fogDensity=0.019,
  ambientColor=C(0.42,0.36,0.46), lightColor=C(1.0,0.78,0.60), lightIntensity=0.95, sunPitch=24.0, sunYaw=200.0,
  pathColor=C(0.52,0.48,0.44), grassColor=C(0.22,0.36,0.18), bambooColor=C(0.36,0.56,0.26), leafColor=C(0.28,0.46,0.22),
  stoneColor=C(0.52,0.50,0.50), waterColor=C(0.22,0.40,0.55,0.78), glowColor=C(1.0,0.7,0.3)),
 dict(levelIndex=4, levelName="Mystic Bamboo Forest", description="Moonlit night with glowing plants. Maximum difficulty.",
  difficulty=5, distanceToUnlockNext=0, baseSpeed=23.0, maxSpeed=40.0, acceleration=0.20,
  obstacleChance=0.68, coinChance=0.90, wLog=2.0, wRock=2.0, wBarricade=2.0, wHanging=2.0, wPit=2.0, wRiver=1.6, wSpike=2.0, wBoulder=1.6, wTempleWall=1.8, wTempleGate=1.6,
  bambooPerSide=11, templeChance=0.15, vineArchChance=0.45, hasWaterfall=True, night=True, glowPlants=True,
  skyTop=C(0.015,0.025,0.10), skyHorizon=C(0.10,0.15,0.34), fogColor=C(0.07,0.11,0.24), fogDensity=0.020,
  ambientColor=C(0.14,0.18,0.34), lightColor=C(0.48,0.58,1.0), lightIntensity=0.55, sunPitch=38.0, sunYaw=165.0,
  pathColor=C(0.24,0.24,0.34), grassColor=C(0.07,0.20,0.22), bambooColor=C(0.17,0.42,0.38), leafColor=C(0.12,0.46,0.42),
  stoneColor=C(0.30,0.32,0.42), waterColor=C(0.10,0.25,0.50,0.80), glowColor=C(0.30,1.0,0.85)),
]

POWERUPS = [
 dict(type=0, displayName="Gold Magnet", shortLabel="M", description="Attracts nearby coins automatically.", baseDuration=10.0, durationPerUpgrade=2.0, maxUpgradeLevel=5, basePrice=100, spawnWeight=1.0, color=C(1.0,0.25,0.25)),
 dict(type=1, displayName="Shield", shortLabel="S", description="Absorbs one collision.", baseDuration=30.0, durationPerUpgrade=5.0, maxUpgradeLevel=5, basePrice=120, spawnWeight=1.2, color=C(0.30,0.60,1.0)),
 dict(type=2, displayName="Speed Boost", shortLabel="B", description="Run faster and smash through obstacles.", baseDuration=6.0, durationPerUpgrade=1.5, maxUpgradeLevel=5, basePrice=150, spawnWeight=0.9, color=C(1.0,0.60,0.10)),
 dict(type=3, displayName="Double Coins", shortLabel="x2", description="Every coin counts double.", baseDuration=15.0, durationPerUpgrade=3.0, maxUpgradeLevel=5, basePrice=100, spawnWeight=1.0, color=C(1.0,0.85,0.20)),
 dict(type=4, displayName="Hover", shortLabel="H", description="Fly over most obstacles, pits and rivers.", baseDuration=7.0, durationPerUpgrade=1.5, maxUpgradeLevel=5, basePrice=150, spawnWeight=0.8, color=C(0.40,0.95,1.0)),
]

SKINS = [
 dict(skinIndex=0, skinName="Explorer", price=0, shirt=C(0.80,0.32,0.20), pants=C(0.25,0.30,0.45), hat=C(0.76,0.60,0.30), skinTone=C(0.95,0.76,0.60)),
 dict(skinIndex=1, skinName="Forest Ranger", price=150, shirt=C(0.25,0.55,0.25), pants=C(0.35,0.28,0.18), hat=C(0.20,0.40,0.18), skinTone=C(0.90,0.70,0.55)),
 dict(skinIndex=2, skinName="Bamboo Monk", price=300, shirt=C(0.95,0.60,0.12), pants=C(0.70,0.30,0.10), hat=C(0.85,0.75,0.45), skinTone=C(0.85,0.62,0.45)),
 dict(skinIndex=3, skinName="Night Ninja", price=500, shirt=C(0.12,0.12,0.18), pants=C(0.08,0.08,0.12), hat=C(0.55,0.10,0.15), skinTone=C(0.80,0.60,0.48)),
 dict(skinIndex=4, skinName="Golden Guardian", price=800, shirt=C(0.95,0.78,0.20), pants=C(0.75,0.55,0.12), hat=C(1.00,0.88,0.35), skinTone=C(0.92,0.72,0.55)),
]

def f(v): return "%g" % v
def yaml_value(v):
    if isinstance(v, bool): return "1" if v else "0"
    if isinstance(v, tuple): return "{r: %s, g: %s, b: %s, a: %s}" % tuple(f(x) for x in v)
    if isinstance(v, float): return f(v)
    if isinstance(v, int): return str(v)
    return "'" + str(v).replace("'", "''") + "'"

def write_asset(rel, cls, name, fields):
    sg = script_guid(cls)
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
        "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % sg, "  m_Name: " + name, "  m_EditorClassIdentifier: "]
    for k, v in fields:
        if isinstance(v, list):
            lines.append("  %s:" % k)
            for g in v: lines.append("  - {fileID: 11400000, guid: %s, type: 2}" % g)
        else:
            lines.append("  %s: %s" % (k, yaml_value(v)))
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "w").write("\n".join(lines) + "\n")

def cs_value(v):
    if isinstance(v, bool): return "true" if v else "false"
    if isinstance(v, tuple): return "new Color(%sf, %sf, %sf, %sf)" % tuple(f(x) for x in v)
    if isinstance(v, float): return f(v) + "f"
    if isinstance(v, int): return str(v)
    return '"' + str(v).replace('"', '\\"') + '"'

asset_guids = {"levels": [], "powerups": [], "skins": []}
for lv in LEVELS:
    name = "Level%d" % (lv["levelIndex"] + 1)
    rel = "Assets/ScriptableObjects/Levels/%s.asset" % name
    write_asset(rel, "LevelConfig", name, list(lv.items()))
    asset_guids["levels"].append(guid_for(rel))
for p in POWERUPS:
    name = "PowerUp_" + p["displayName"].replace(" ", "")
    rel = "Assets/ScriptableObjects/PowerUps/%s.asset" % name
    write_asset(rel, "PowerUpConfig", name, list(p.items()))
    asset_guids["powerups"].append(guid_for(rel))
for s in SKINS:
    name = "Skin_" + s["skinName"].replace(" ", "")
    rel = "Assets/ScriptableObjects/Skins/%s.asset" % name
    write_asset(rel, "SkinConfig", name, list(s.items()))
    asset_guids["skins"].append(guid_for(rel))

write_asset("Assets/Resources/GameConfig.asset", "GameConfig", "GameConfig",
    [("levels", asset_guids["levels"]), ("powerUps", asset_guids["powerups"]), ("skins", asset_guids["skins"])])

# ---- DefaultData.cs (fallback, generated from the same tables) ----
cs = ["using UnityEngine;", "", "namespace ForestRunner", "{",
      "    /// <summary>AUTO-GENERATED by build/gen_data.py. Fallback copy of the ScriptableObject data.</summary>",
      "    public static class DefaultData", "    {"]
def emit(fn, cls, items):
    cs.append("        public static %s[] %s()" % (cls, fn))
    cs.append("        {")
    cs.append("            %s[] a = new %s[%d];" % (cls, cls, len(items)))
    for i, it in enumerate(items):
        cs.append("            { %s o = ScriptableObject.CreateInstance<%s>();" % (cls, cls))
        for k, v in it.items():
            if k == "type": cs.append("              o.type = (PowerUpType)%d;" % v)
            else: cs.append("              o.%s = %s;" % (k, cs_value(v)))
        cs.append("              a[%d] = o; }" % i)
    cs.append("            return a;")
    cs.append("        }")
    cs.append("")
emit("CreateLevels", "LevelConfig", LEVELS)
emit("CreatePowerUps", "PowerUpConfig", POWERUPS)
emit("CreateSkins", "SkinConfig", SKINS)
cs.append("    }")
cs.append("}")
open(os.path.join(ROOT, "Assets/Scripts/Data/DefaultData.cs"), "w").write("\n".join(cs) + "\n")
print("data generated")
