"""Metadata for native pirate melee and own-reference idles. No raster writes."""
import json
from pathlib import Path
from PIL import Image
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
DIRS=['south','south-east','east','north-east','north','north-west','west','south-west']
def base(role,action,direction,paths,times,note,source):
    with Image.open(ROOT/'source/actors'/f'{role}_views_clean/{direction}.png')as image:
        box=image.convert('RGBA').getbbox();oy=128-(box[3]-1)
    for p in paths:
        with Image.open(p)as image:assert image.size==(128,128)and image.convert('RGBA').getbbox()
    return dict(approved=True,role=role,action=action,direction=direction,authoredDirection=direction,
        sourceMode='aseprite-cleanup',category='enemies',width=160,height=160,loop=action in ['idle','walk'],flipX=False,
        displayScale=1,groundPivot={'x':80,'y':128},hitPoint={'x':80,'y':93},
        inputs=[dict(file=str(p),offsetX=16,offsetY=oy,durationMs=t)for p,t in zip(paths,times)],
        sockets=[],events=[],originalSource=str(source),reviewNotes=note)
parts={}
for role in ['pirate','warlock']:
    clips=[]
    for direction in DIRS:
        reference=ROOT/'source/actors'/f'{role}_views_clean/{direction}.png'
        path=reference
        note='Actual independently authored directional reference, native canvas retained and padded. No mirror/rotation of the body.'
        if role=='warlock'and direction=='east':
            path=ROOT/'source/actors/enemy_weapon_qa/warlock/lightning_cast/east/frames/frame_000.png'
            note+=' Staff-less E reference repaired with the same native single staff from approved E casting first pose; body unchanged.'
        clips.append(base(role,'idle',direction,[path],[1000],note,reference))
    parts[f'{role}-idle-eight-qa']=clips
melee=[]
for direction in DIRS:
    folder=ROOT/'source/actors/enemy_weapon_qa/pirate/attack'/direction
    meta=json.loads((folder/'native-cleanup.json').read_text(encoding='utf-8'))
    paths=sorted((folder/'frames').glob('*.png'));times=meta['durationsMs'];idx=meta['contactFrameIndex']
    assert len(paths)==8 and sum(times)==850 and sum(times[:idx])==300
    p=meta['sockets'][idx];tx,ty=p['tip'];hx,hy=p['hand']
    with Image.open(paths[idx])as image:
        image=image.convert('RGBA')
        assert image.getpixel((tx,ty))[3]>200,('Missing blade tip',direction,(tx,ty))
        assert image.getpixel((hx,hy))[3]>200,('Missing grip',direction,(hx,hy))
    clip=base('pirate','attack',direction,paths,times,
        'Own authored body view. Native short steel contact blade replaces inconsistent bright generated weapon shapes; baked loops and disconnected outlines removed. West/SW reorder their own windup/contact poses. North occluded forearm rebuilt, duplicated lowered arm removed. Bodies are not mirrored or rotated. Legacy850ms/contact300ms preserved.',meta['originalSource'])
    oy=clip['inputs'][0]['offsetY']
    hand=dict(x=hx+16,y=hy+oy);tip=dict(x=tx+16,y=ty+oy)
    clip['sockets']=[dict(name='weapon_hand',frameIndex=idx,position=hand),dict(name='blade_tip',frameIndex=idx,position=tip)]
    clip['events']=[dict(name='melee_contact',frameIndex=idx,position=tip,socketName='blade_tip')]
    melee.append(clip)
parts['pirate-attack-eight-qa']=melee
for name,clips in parts.items():
    (ROOT/'review/manifests'/f'{name}.json').write_text(json.dumps(dict(root=str(ROOT),schemaVersion=1,clips=clips),indent=2),encoding='utf-8')
print('READY: 16 owned idle views + 8 pirate contact attacks; exact opaque grip/tip and850ms/300ms timing verified.')
