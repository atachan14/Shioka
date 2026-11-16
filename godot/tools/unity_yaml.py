"""Unity YAML (asset / prefab / meta) reader used by export_unity_data.py.

One-time migration helper: reads the Unity project under ../Assets and resolves
GUID references. Godot never runs this; the exported JSON under godot/data is
the source of truth after the migration.
"""
import os, re, yaml

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ASSETS = os.path.join(ROOT, 'Assets')

class _Loader(yaml.SafeLoader):
    pass

def _any(loader, suffix, node):
    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node, deep=True)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node, deep=True)
    return loader.construct_scalar(node)
_Loader.add_multi_constructor('', _any)

_guid_map = None
def guid_map():
    global _guid_map
    if _guid_map is None:
        _guid_map = {}
        for d, _, files in os.walk(ASSETS):
            for f in files:
                if f.endswith('.meta'):
                    p = os.path.join(d, f)
                    with open(p, encoding='utf-8') as h:
                        for line in h:
                            if line.startswith('guid:'):
                                _guid_map[line.split()[1]] = p[:-5]
                                break
    return _guid_map

def path_of(ref):
    if not isinstance(ref, dict) or not ref.get('guid'):
        return None
    return guid_map().get(ref['guid'])

def load_docs(path):
    text = open(path, encoding='utf-8').read()
    # Serialized int arrays are hex strings; keep them as strings (digit-only ones would parse as ints).
    text = re.sub(r'^(\s*(?:chains|modifier\dActions): )([0-9a-fA-F]+)\s*$', r"\1'\2'", text, flags=re.M)
    docs = {}
    parts = re.split(r'^--- !u!(\d+) &(-?\d+)( stripped)?\s*$', text, flags=re.M)
    # parts: [header, cls, id, stripped, body, cls, id, stripped, body...]
    for i in range(1, len(parts), 4):
        body = parts[i + 3]
        data = yaml.load(body, Loader=_Loader) or {}
        docs[int(parts[i + 1])] = data
    return docs

def load_asset(path):
    """Main MonoBehaviour object of a ScriptableObject asset."""
    docs = load_docs(path)
    for v in docs.values():
        if 'MonoBehaviour' in v:
            return v['MonoBehaviour']
    return next(iter(docs.values()))

def load_ref(ref):
    p = path_of(ref)
    return None if p is None else load_asset(p)

def meta(path):
    return yaml.load(open(path + '.meta', encoding='utf-8').read(), Loader=_Loader)

def hex_ints(s):
    if not s:
        return []
    s = str(s)
    return [int.from_bytes(bytes.fromhex(s[i:i + 8]), 'little', signed=True) for i in range(0, len(s), 8)]
