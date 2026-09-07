"""Read-only raster/metadata audit of the owned pirate and warlock accepted parts."""
import hashlib,json
from pathlib import Path
from PIL import Image
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
names=['pirate-idle-eight-qa','pirate-walk-eight-qa','pirate-attack-eight-qa','pirate-specials-six-qa','pirate-specials-two-qa','warlock-idle-eight-qa','warlock-specials-qa','warlock-motions-qa']
keys=set();frames=0;checked_events=0;roles={};checks=[]
for name in names:
    part=json.loads((ROOT/'review/manifests'/f'{name}.json').read_text(encoding='utf-8'))
    for clip in part['clips']:
        key=(clip['role'],clip['action'],clip['direction'])
        assert key not in keys,key
        keys.add(key);roles[key[0]]=roles.get(key[0],0)+1
        assert clip['approved']and not clip['flipX']and clip['authoredDirection']==clip['direction']
        inputs=clip['inputs'];duration=sum(x['durationMs']for x in inputs)
        for item in inputs:
            with Image.open(item['file'])as image:
                assert image.size==(128,128)
                assert 0<=item['offsetX']and item['offsetX']+128<=160
                assert 0<=item['offsetY']and item['offsetY']+128<=160
                assert image.convert('RGBA').getbbox()
            frames+=1
        for event in clip['events']:
            idx=event['frameIndex'];point=event['position'];item=inputs[idx]
            px=round(point['x']-item['offsetX']);py=round(point['y']-item['offsetY'])
            with Image.open(item['file'])as image:
                assert image.convert('RGBA').getpixel((px,py))[3]>200,(key,event,px,py)
            timing=sum(x['durationMs']for x in inputs[:idx])
            expected={'attack':((850,300)if clip['role']=='pirate'else(960,320)),'pistol_shot':(930,440),'lightning_cast':(760,290),'cast_fail':(1040,440)}[clip['action']]
            assert(duration,timing)==expected,(key,duration,timing)
            checked_events+=1
        checks.append(dict(role=key[0],action=key[1],direction=key[2],frames=len(inputs),durationMs=duration,
            sourceSha256=hashlib.sha256(Path(inputs[0]['file']).read_bytes()).hexdigest()))
assert roles=={'pirate':32,'warlock':40},roles
result=dict(status='PASS',scope='All owned accepted pirate32 and warlock40; complete idle/walk/melee/special8 views.',
    clips=len(keys),frames=frames,opaqueTimedEvents=checked_events,roles=roles,checks=checks)
(ROOT/'review/enemy-weapon-verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items()if k!='checks'}))
