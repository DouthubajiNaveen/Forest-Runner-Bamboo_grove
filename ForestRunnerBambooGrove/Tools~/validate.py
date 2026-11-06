import os, re, sys
from common import ROOT
cs = {}
for dp, dn, fn in os.walk(os.path.join(ROOT, "Assets")):
    for f in fn:
        if f.endswith(".cs"):
            cs[os.path.join(dp, f)] = open(os.path.join(dp, f)).read()

def strip(src):
    out = []; i = 0; n = len(src)
    while i < n:
        c = src[i]
        if src.startswith("//", i):
            while i < n and src[i] != "\n": i += 1
        elif src.startswith("/*", i):
            j = src.find("*/", i + 2); i = n if j < 0 else j + 2
        elif c == '"':
            i += 1
            while i < n and src[i] != '"':
                if src[i] == "\\": i += 1
                i += 1
            i += 1; out.append('""')
        elif c == "'":
            i += 1
            while i < n and src[i] != "'":
                if src[i] == "\\": i += 1
                i += 1
            i += 1; out.append("' '")
        else:
            out.append(c); i += 1
    return "".join(out)

errors = []
stripped = {p: strip(s) for p, s in cs.items()}
# 1 balance
for p, s in stripped.items():
    for a, b in ("{}", "()", "[]"):
        if s.count(a) != s.count(b):
            errors.append("%s: unbalanced %s%s (%d vs %d)" % (os.path.basename(p), a, b, s.count(a), s.count(b)))

# 2 type table
types = {}
for p, s in stripped.items():
    for m in re.finditer(r"\b(?:class|struct|enum|interface)\s+([A-Z]\w*)", s):
        types.setdefault(m.group(1), []).append(p)
def members_of(name):
    ms = set()
    for p in types.get(name, []):
        s = stripped[p]
        i = s.find(name)
        # whole-file identifier set is a superset, good enough for existence check
        ms |= set(re.findall(r"\b[A-Za-z_]\w*\b", s))
    return ms

enum_members = {}
for p, s in stripped.items():
    for m in re.finditer(r"enum\s+(\w+)\s*\{([^}]*)\}", s):
        enum_members[m.group(1)] = set(re.findall(r"\b([A-Za-z_]\w*)\b(?=\s*(?:=|,|$))", m.group(2).strip() + ","))

unity_types = set("""Mathf Vector2 Vector3 Vector4 Quaternion Color Color32 Time Input KeyCode Physics Random Resources Screen Application QualitySettings
RenderSettings GameObject Transform Camera Light Mesh Material Shader Texture2D Texture Sprite Font Rect Matrix4x4 AnimationCurve Keyframe Gradient
SceneManager AudioClip AudioSource AudioListener ParticleSystem Object Debug PlayerPrefs System Array String Math UnityEngine List Dictionary Func Action
Space Collider BoxCollider SphereCollider Rigidbody MeshFilter MeshRenderer Canvas CanvasScaler Image Text Button Slider EventSystem Screen""".split())
mychk = 0
for p, s in stripped.items():
    for m in re.finditer(r"\b([A-Z]\w+)\.([A-Za-z_]\w*)", s):
        t, mem = m.group(1), m.group(2)
        if t in unity_types: continue
        if t in types:
            mychk += 1
            if t in enum_members:
                if mem not in enum_members[t]: errors.append("%s: enum %s has no member %s" % (os.path.basename(p), t, mem))
            elif mem not in members_of(t):
                errors.append("%s: %s.%s not found in %s" % (os.path.basename(p), t, mem, t))
    # Instance chains: X.Instance.Member
    for m in re.finditer(r"\b([A-Z]\w+)\.Instance\.([A-Za-z_]\w*)", s):
        t, mem = m.group(1), m.group(2)
        if t in types and mem not in members_of(t):
            errors.append("%s: %s.Instance.%s not found" % (os.path.basename(p), t, mem))
print("cross refs checked:", mychk)

# 3 guid integrity
metaguids = {}
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.endswith(".meta"):
            g = re.search(r"guid: (\w+)", open(os.path.join(dp, f)).read()).group(1)
            metaguids[g] = os.path.join(dp, f)
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.endswith((".asset", ".unity")):
            for g in re.findall(r"guid: ([0-9a-f]{32})", open(os.path.join(dp, f)).read()):
                if g not in metaguids and g != "0000000000000000f000000000000000":
                    errors.append("%s references unknown guid %s" % (f, g))
# 4 every asset has meta
for dp, dn, fn in os.walk(os.path.join(ROOT, "Assets")):
    for x in dn + fn:
        if not x.endswith(".meta") and not os.path.exists(os.path.join(dp, x) + ".meta"):
            errors.append("missing meta for " + os.path.join(dp, x))
# 5 asset field names vs class fields
for dp, dn, fn in os.walk(os.path.join(ROOT, "Assets/ScriptableObjects")):
    for f in fn:
        if f.endswith(".asset"):
            cls = {"Levels": "LevelConfig", "PowerUps": "PowerUpConfig", "Skins": "SkinConfig"}[os.path.basename(dp)]
            src = stripped[[p for p in cs if p.endswith(cls + ".cs")][0]]
            for k in re.findall(r"^  (\w+):", open(os.path.join(dp, f)).read(), re.M):
                if k.startswith("m_"): continue
                if not re.search(r"\b" + k + r"\b", src): errors.append("%s field %s missing in %s" % (f, k, cls))
print("scripts:", len(cs), "lines:", sum(s.count("\n") for s in cs.values()))
print("\n".join(errors) if errors else "NO STATIC ERRORS FOUND")
