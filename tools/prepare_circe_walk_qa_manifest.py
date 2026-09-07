"""Read-only pixel inspection and metadata. All corrections come from native Aseprite."""
import json
from pathlib import Path
from collections import deque
from PIL import Image

ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
RAW=ROOT/'raw/actors/circe_8views_reference_v1'
DIRS=['south','south-east','east','north-east','north','north-west','west','south-west']

def staff_pixel(path):
    with Image.open(path) as image:
        im=image.convert('RGBA')
        px=im.load()
        selected=set()
        for y in range(15,45):
            for x in range(im.width):
                r,g,b,a=px[x,y]
                if a>200 and ((g>140 and b>160 and b>r+8)or min(r,g,b)>230):selected.add((x,y))
        components=[]
        while selected:
            first=selected.pop();q=deque([first]);component=[first]
            while q:
                x,y=q.popleft()
                for dx in [-1,0,1]:
                    for dy in [-1,0,1]:
                        p=(x+dx,y+dy)
                        if p in selected:selected.remove(p);q.append(p);component.append(p)
            cyan=[p for p in component if px[p][2]>px[p][0]+8 and px[p][1]>140]
            if cyan:components.append((len(cyan),component))
        assert components,('No visible cyan staff crystal',path)
        _,component=max(components,key=lambda c:c[0])
        whites=[p for p in component if min(px[p][:3])>220]
        candidates=whites or component
        cx=sum(x for x,y in candidates)/len(candidates);cy=sum(y for x,y in candidates)/len(candidates)
        point=min(candidates,key=lambda p:(p[0]-cx)**2+(p[1]-cy)**2)
        return point

clips=[]
for direction in DIRS:
    reference=RAW/'rotations'/f'{direction}.png'
    with Image.open(reference) as im:
        assert im.size==(92,92)
        bottom=im.convert('RGBA').getbbox()[3]-1
    ox,oy=18,100-bottom
    for action in ['idle','walk']:
        mode='pixellab-v3' if action=='idle' else 'pixellab-template'
        if action=='idle':paths=[reference]
        elif direction in ['south','north-east','south-west']:
            clean={'south':'circe_south_walk_clean','north-east':'circe_ne_walk_clean','south-west':'circe_sw_walk_clean'}[direction]
            paths=sorted((ROOT/'source/actors'/clean/'frames').glob('*.png'))
            mode='aseprite-cleanup'
        else:
            candidates=list((RAW/'animations').glob('animating*/'+direction))
            paths=sorted(candidates[0].glob('*.png')) if candidates else []
        if not paths:continue
        assert len(paths)==(1 if action=='idle' else 4)
        sockets=[]
        for i,path in enumerate(paths):
            x,y=staff_pixel(path)
            sockets.append({'name':'staffTip','frameIndex':i,'position':{'x':x+ox,'y':y+oy}})
        clips.append({'approved':True,'role':'circe','action':action,'direction':direction,'authoredDirection':direction,
            'sourceMode':mode,'category':'heroes','width':128,'height':128,'loop':True,'flipX':False,'displayScale':1,
            'groundPivot':{'x':64,'y':100},'hitPoint':{'x':64,'y':73},
            'inputs':[{'file':str(p),'offsetX':ox,'offsetY':oy,'durationMs':1000 if action=='idle' else 150}for p in paths],
            'events':[],'sockets':sockets,
            'reviewNotes':'True authored direction with fixed padding and natural motion. Staff socket is a verified visible white/cyan crystal pixel each frame. South terminal back-view replaced by frontal passing pose; NE stray club removed and upright far-hand staff restored behind original NE body; SW lost crystal restored with its own staff behind unchanged body.'})
out=ROOT/'review/manifests/circe-idle-walk-qa.json';out.parent.mkdir(exist_ok=True)
out.write_text(json.dumps({'root':str(ROOT),'schemaVersion':1,'clips':clips},indent=2),encoding='utf-8')
print('READY Circe',len(clips),'clips; walking dirs:',[c['direction']for c in clips if c['action']=='walk'])
for c in clips:
    print(c['action'],c['direction'],[s['position']for s in c['sockets']])
