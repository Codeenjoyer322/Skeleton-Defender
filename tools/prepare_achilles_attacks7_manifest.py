"""Final real-view attack metadata; native Aseprite owns all image edits."""
import json
from pathlib import Path
from PIL import Image
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
HANDS={
'south':[(33,62),(33,34),(43,23),(43,23),(43,23),(43,23),(53,70),(52,70),(50,70),(50,70),(51,69),(33,62)],
'south-east':[(55,65),(62,57),(61,36),(48,33),(47,33),(71,57),(50,70),(44,64),(43,64),(43,64),(43,64),(55,65)],
'north':[(53,51),(53,47),(53,43),(53,43),(53,43),(53,43),(60,65),(63,66),(62,66),(62,66),(62,66),(53,51)],
'west':[(42,65),(46,58),(44,48),(47,40),(44,43),(44,43),(43,46),(46,66),(47,70),(48,69),(48,69),(42,65)],
'south-west':[(42,64),(43,55),(44,39),(54,32),(53,37),(53,37),(52,37),(32,65),(45,69),(44,67),(44,67),(42,64)],
'north-west':[(32,52),(38,44),(40,39),(42,34),(39,36),(38,38),(39,39),(37,44),(34,49),(34,49),(32,51),(32,52)]}
TIPS={'south':(52,90),'south-east':(62,89),'north':(82,71),'west':(19,54),'south-west':(15,82),'north-west':(22,31)}
def pixel_near(image,point,kind,radius):
    cx,cy=point;candidates=[]
    for y in range(max(0,cy-radius),min(image.height,cy+radius+1)):
        for x in range(max(0,cx-radius),min(image.width,cx+radius+1)):
            r,g,b,a=image.getpixel((x,y))
            good=a>200 and ((r>150 and g>80 and r>b+35)if kind=='gold'else(min(r,g,b)>95 and max(r,g,b)-min(r,g,b)<45))
            if good:candidates.append((x,y))
    assert candidates,('No actual '+kind+' pixel near authored point',point)
    return min(candidates,key=lambda p:(p[0]-cx)**2+(p[1]-cy)**2)
clips=[]
for direction in ['south','south-east','north-east','north','north-west','west','south-west']:
    folder=ROOT/'source/actors'/f'achilles_attack_{direction}_clean'
    meta=json.loads((folder/'native-cleanup.json').read_text(encoding='utf-8'))
    paths=sorted((folder/'frames').glob('*.png'));times=meta['durationsMs'];contact=meta['contactFrameIndex'];size=meta['size']
    assert len(paths)==12 and sum(times)==900 and sum(times[:contact])==500
    with Image.open(paths[0])as reference:
        ox=(128-size)//2;oy=100-(reference.convert('RGBA').getbbox()[3]-1)
    points=meta.get('weaponHands',HANDS.get(direction));sockets=[]
    for index,(path,point)in enumerate(zip(paths,points)):
        with Image.open(path)as source:
            image=source.convert('RGBA');assert image.size==(size,size)
            # The north-facing grip is in front of the chest and genuinely occluded by the back.
            # Keep its anatomical location; snapping to a visible shoulder would invent a false socket.
            x,y=point if direction=='north' and index in [0,1,2,3,4,5,11] else pixel_near(image,point,'gold',7)
            sockets.append({'name':'weapon_hand','frameIndex':index,'position':{'x':x+ox,'y':y+oy}})
    with Image.open(paths[contact])as source:
        tip=meta['bladeTips'][contact]if 'bladeTips'in meta else TIPS[direction]
        x,y=pixel_near(source.convert('RGBA'),tip,'steel',14)
    blade={'x':x+ox,'y':y+oy};sockets.append({'name':'blade_tip','frameIndex':contact,'position':blade})
    clips.append({'approved':True,'role':'achilles','action':'attack','direction':direction,'authoredDirection':direction,
        'sourceMode':'aseprite-cleanup','category':'heroes','width':128,'height':128,'loop':False,'flipX':False,'displayScale':1,
        'groundPivot':{'x':64,'y':100},'hitPoint':{'x':64,'y':72},
        'inputs':[{'file':str(p),'offsetX':ox,'offsetY':oy,'durationMs':t}for p,t in zip(paths,times)],
        'events':[{'name':'sword_contact','frameIndex':contact,'position':blade,'socketName':'blade_tip'}],
        'sockets':sockets,'originalSource':meta['originalSource'],
        'reviewNotes':'True independently authored direction; native own-ready recovery frame, actual sword contact retimed to500ms without changing900ms action duration. Visible hand sockets verified on final gold pixels, contact on visible steel. North front-of-chest grip is anatomically occluded during windup and recorded behind the back, never snapped to a shoulder. '+('NE locked view preserves original body/arms while native Aseprite repairs the full-length sword swing; rejected v3 frontal morph not used.'if direction=='north-east'else'NW locked view used; no body mirror/rotation.'if direction=='north-west'else'SE duplicate blade removed.'if direction=='south-east'else'Natural body twist retained during strike.')})
out=ROOT/'review/manifests/achilles-attacks-seven-qa.json';out.write_text(json.dumps({'root':str(ROOT),'schemaVersion':1,'clips':clips},indent=2),encoding='utf-8')
print('READY7 Achilles attacks:900ms duration, actual contact500ms, own ready recovery, real per-frame gold-hand/steel-contact sockets.')
for clip in clips:print(clip['direction'],clip['events'][0], 'padding',clip['inputs'][0]['offsetY'])
