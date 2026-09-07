"""Metadata and read-only pixel verification for native Aseprite hero cleanup."""
import json
from pathlib import Path
from PIL import Image
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
clips=[]
for direction in ['south','south-east','north-east','north','north-west','west','south-west']:
    folder=ROOT/'source/actors'/f'circe_staff_attack_{direction}_clean'
    meta=json.loads((folder/'native-cleanup.json').read_text(encoding='utf-8'))
    times=meta['durationsMs'];release=meta['releaseFrameIndex'];centers=meta['centers']
    assert sum(times)==1160 and sum(times[:release])==550
    paths=sorted((folder/'frames').glob('*.png'));assert len(paths)==len(times)
    sockets=[]
    for index,(path,(cx,cy)) in enumerate(zip(paths,centers)):
        with Image.open(path) as image:
            im=image.convert('RGBA');assert im.size==(104,104)
            choices=[]
            for y in range(max(0,cy-4),min(im.height,cy+5)):
                for x in range(max(0,cx-4),min(im.width,cx+5)):
                    r,g,b,a=im.getpixel((x,y))
                    if a>200 and ((b>r+8 and g>130)or min(r,g,b)>230):choices.append((x,y,min(r,g,b)>230))
            assert choices,('No actual staff crystal',path,(cx,cy))
            x,y,_=min(choices,key=lambda p:(0 if p[2]else 100)+(p[0]-cx)**2+(p[1]-cy)**2)
            sockets.append({'name':'staffTip','frameIndex':index,'position':{'x':x+12,'y':y+12}})
    clips.append({'approved':True,'role':'circe','action':'staff_attack','direction':direction,'authoredDirection':direction,
        'sourceMode':'aseprite-cleanup','category':'heroes','width':128,'height':128,'loop':False,'flipX':False,'displayScale':1,
        'groundPivot':{'x':64,'y':100},'hitPoint':{'x':65,'y':73},
        'inputs':[{'file':str(p),'offsetX':12,'offsetY':12,'durationMs':t}for p,t in zip(paths,times)],
        'events':[{'name':'release_fireball','frameIndex':release,'position':sockets[release]['position'],'socketName':'staffTip'}],
        'sockets':sockets,'originalSource':str(ROOT/'raw/actors'/f'circe_attack_{direction.replace("-","_")}_v3/job_00/frames'),
        'reviewNotes':'Independent authored direction, raw body retained. Native Aseprite removes detached cyan overshoot, attaches a compact crystal to actual staff and adds ready-pose recovery. All frame sockets sampled from the final visible crystal; duration1.16s/release.55s unchanged. '+('West rejects generated right-turn frames3-7; uses actual west source0,1,2,2,1,0.'if direction=='west'else'No body mirroring or rotation.')})
out=ROOT/'review/manifests/circe-attacks-seven-qa.json'
out.write_text(json.dumps({'root':str(ROOT),'schemaVersion':1,'clips':clips},indent=2),encoding='utf-8')
print('READY:7 independently authored Circe attacks; every frame crystal is a verified actual pixel; all release times550ms.')
