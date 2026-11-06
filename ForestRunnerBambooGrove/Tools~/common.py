import hashlib, os
ROOT = "/home/claude/ForestRunnerBambooGrove"

def guid_for(path):
    """Deterministic 32-hex GUID derived from the project-relative asset path."""
    return hashlib.md5(("ForestRunnerBambooGrove/" + path).encode("utf-8")).hexdigest()

def script_guid(name):
    for dp, dn, fn in os.walk(os.path.join(ROOT, "Assets")):
        for f in fn:
            if f == name + ".cs":
                rel = os.path.relpath(os.path.join(dp, f), ROOT).replace(os.sep, "/")
                return guid_for(rel)
    raise Exception("script not found: " + name)
