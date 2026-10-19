"""Check the directory that Steam will publish, not just dist's main manifest."""
import argparse,json,hashlib
from pathlib import Path

def validate(root,branch):
    root=Path(root)
    manifests=list(root.rglob("*.json"))
    assert len(manifests)==1 and manifests[0].name=="mod_manifest.json", "Game scans every JSON as a mod manifest; auxiliary JSON must stay outside content."
    manifest=json.loads(manifests[0].read_text(encoding="utf-8-sig"))
    game={"public":"0.107.1","public-beta":"0.111.0"}[branch]
    assert manifest["id"]=="Frostsworn" and manifest["min_game_version"]==game
    for name in ("Frostsworn.dll","Frostsworn.pck"):
        assert (root/name).is_file() and (root/name).stat().st_size>0
    print(json.dumps({"branch":branch,"version":manifest["version"],"game":game,"hashes":{n:hashlib.sha256((root/n).read_bytes()).hexdigest() for n in ("Frostsworn.dll","Frostsworn.pck","mod_manifest.json")}},ensure_ascii=False))

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("content");p.add_argument("--branch",choices=("public","public-beta"),required=True);a=p.parse_args();validate(a.content,a.branch)
