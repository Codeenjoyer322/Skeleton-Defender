"""Copy approved editable art and previews into the private source repository.

Only delivered exports and sources referenced by the imported legacy pack are
included. Account settings, raw API requests and rejected generations stay local.
"""
from pathlib import Path
import json, shutil, hashlib

PROJECT=Path(__file__).resolve().parents[1]
DESKTOP=Path('C:/Users/Life/Desktop/Testnew2')
DEST=PROJECT/'ArtSource'
records={}
missing=[]

def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def copy(source, purpose):
    source=Path(source).resolve()
    try: relative=source.relative_to(DESKTOP.resolve())
    except ValueError: return
    if source.suffix.lower() not in ('.aseprite','.ase','.png','.gif','.json','.lua','.md','.html'):
        return
    if not source.is_file():
        missing.append(str(relative)); return
    if source.stat().st_size>90_000_000:
        raise RuntimeError('Art file requires separate storage: '+str(relative))
    dest=DEST/relative
    dest.parent.mkdir(parents=True,exist_ok=True)
    if not dest.exists() or source.read_bytes()!=dest.read_bytes(): shutil.copy2(source,dest)
    records[relative.as_posix()]=dict(path=relative.as_posix(),purpose=purpose,
        bytes=dest.stat().st_size,sha256=hashlib.sha256(dest.read_bytes()).hexdigest())

legacy=read(PROJECT/'Assets/Resources/AnimationPack/provenance.json')
for entry in legacy['sources']:
    for key in ('sourceSheet','sourceMetadata'):
        if entry.get(key): copy(DESKTOP/entry[key],'legacy accepted animation')
    metadata=DESKTOP/entry.get('sourceMetadata','missing')
    if metadata.is_file():
        data=read(metadata)
        files=data.get('meta',{}).get('files',{})
        for key in ('native','gif','sheet','metadata'):
            if isinstance(files.get(key),str):
                src=Path(files[key]);copy(src if src.is_absolute() else metadata.parent/src,'legacy editable source')

for version in (DESKTOP/'NeonGothic',DESKTOP/'NeonGothic/v081'):
    for category in ('exports','source/environment','source/icons','source/towers'):
        directory=version/category
        if directory.is_dir():
            for file in directory.rglob('*'):
                if file.is_file(): copy(file,'Neon Gothic delivered art')
    for name in ('review/index.html','review/gallery-data.json'):
        if (version/name).exists(): copy(version/name,'offline review gallery')

DEST.mkdir(parents=True,exist_ok=True)
summary=dict(files=len(records),bytes=sum(x['bytes'] for x in records.values()),
    missingReferencedFiles=sorted(set(missing)),records=list(records.values()))
(DEST/'source-index.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in summary.items() if k!='records'},ensure_ascii=False))
