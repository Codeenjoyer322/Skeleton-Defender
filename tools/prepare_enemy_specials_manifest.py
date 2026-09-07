"""Approved weapon metadata only; all raster cleanup is native Aseprite."""
import json
from pathlib import Path
from PIL import Image
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
DIRS=['south','south-east','east','north-east','north','north-west','west','south-west']
def base(role,action,direction,paths,times,notes,original):
    reference=ROOT/'source/actors'/f'{role}_views_clean/{direction}.png'
    with Image.open(reference)as src:oy=128-(src.convert('RGBA').getbbox()[3]-1)
    for path in paths:
        with Image.open(path)as src:assert src.size==(128,128)and src.convert('RGBA').getbbox()
    return {'approved':True,'role':role,'action':action,'direction':direction,'authoredDirection':direction,
        'sourceMode':'aseprite-cleanup','category':'enemies','width':160,'height':160,'loop':False,'flipX':False,'displayScale':1,
        'groundPivot':{'x':80,'y':128},'hitPoint':{'x':80,'y':93},
        'inputs':[{'file':str(p),'offsetX':16,'offsetY':oy,'durationMs':t}for p,t in zip(paths,times)],
        'events':[],'sockets':[],'reviewNotes':notes,'originalSource':original}
warlock=[]
for direction in DIRS:
    folder=ROOT/'source/actors/enemy_weapon_qa/warlock/lightning_cast'/direction
    meta=json.loads((folder/'native-cleanup.json').read_text(encoding='utf-8'));paths=sorted((folder/'frames').glob('*.png'));assert len(paths)==8
    crystals=[]
    for path,(cx,cy)in zip(paths,meta['cores']):
        with Image.open(path)as src:
            im=src.convert('RGBA');candidates=[]
            for y in range(max(0,cy-7),min(128,cy+8)):
                for x in range(max(0,cx-5),min(128,cx+6)):
                    r,g,b,a=im.getpixel((x,y))
                    if a>200 and b>r+8 and b>g+15 and r>110:candidates.append((x,y))
            assert candidates,('No visible actual purple crystal',path,(cx,cy))
            crystals.append(min(candidates,key=lambda p:(p[0]-cx)**2+(p[1]-cy)**2))
    for action in ['lightning_cast','cast_fail']:
        times=meta['durationsMs']if action=='lightning_cast'else[200,240,120,100,100,100,80,100]
        cue='lightning_release'if action=='lightning_cast'else'fizzle_release'
        assert sum(times)==(760 if action=='lightning_cast' else 1040)and sum(times[:2])==(290 if action=='lightning_cast' else 440)
        clip=base('warlock',action,direction,paths,times,
            'Own authored view. Baked lightning and duplicate crystals removed; occluding-flash poses excluded in favor of clean poses of the same view. One visible native purple crystal, actual pixel socket each frame. '+
            ('East uses the true east reference body and a native single staff raised/tilted around its fixed grip; no mirrored or rotated body.'if direction=='east'else'Actual generated body and cloak motion retained.')+
            (' Fizzle deliberately shares the casting art with independent1.04s/.44s timing; smoke is a separate runtime effect.'if action=='cast_fail'else''),meta['originalSource'])
        oy=clip['inputs'][0]['offsetY']
        clip['sockets']=[{'name':'staff_head','frameIndex':i,'position':{'x':x+16,'y':y+oy}}for i,(x,y)in enumerate(crystals)]
        clip['events']=[{'name':cue,'frameIndex':2,'position':clip['sockets'][2]['position'],'socketName':'staff_head'}]
        warlock.append(clip)
pirate=[]
for direction in ['east','north-east','north','north-west','west','south-west']:
    folder=ROOT/'source/actors/enemy_weapon_qa/pirate/pistol_shot'/direction
    meta=json.loads((folder/'native-cleanup.json').read_text(encoding='utf-8'));paths=sorted((folder/'frames').glob('*.png'));times=meta['durationsMs'];assert len(paths)==8
    assert sum(times)==930 and sum(times[:3])==440
    x,y=meta['muzzle']
    with Image.open(paths[3])as src:assert src.convert('RGBA').getpixel((x,y))[3]>200,('Native muzzle lost',direction,(x,y))
    clip=base('pirate','pistol_shot',direction,paths,times,
        'Own true firing view. Baked muzzle flash/bullet/casing removed and obscured barrel end restored in native Aseprite. One exact muzzle socket on the actual release frame; other frames do not emit projectiles. Duration.93s/release.44s preserved.',meta['originalSource'])
    point={'x':x+16,'y':y+clip['inputs'][0]['offsetY']}
    clip['sockets']=[{'name':'pistol_muzzle','frameIndex':3,'position':point}]
    clip['events']=[{'name':'release_projectile','frameIndex':3,'position':point,'socketName':'pistol_muzzle'}]
    pirate.append(clip)
for name,clips in [('warlock-specials-qa',warlock),('pirate-specials-six-qa',pirate)]:
    (ROOT/'review/manifests'/f'{name}.json').write_text(json.dumps({'root':str(ROOT),'schemaVersion':1,'clips':clips},indent=2),encoding='utf-8')
print('READY:16 warlock special clips +6 pirate pistol shots. True-view native PNG pixel anchors and legacy timing checks PASS.')
