"""Metadata only: native Aseprite owns all image pixels."""
import json
from pathlib import Path
from PIL import Image
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
DIRS=['south','south-east','east','north-east','north','north-west','west','south-west']
def clip_base(action,direction,paths,times,notes,source):
    with Image.open(ROOT/'source/actors/pirate_views_clean'/f'{direction}.png')as image:
        oy=128-(image.convert('RGBA').getbbox()[3]-1)
    for p in paths:
        with Image.open(p)as image:assert image.size==(128,128)and image.convert('RGBA').getbbox()
    return dict(approved=True,role='pirate',action=action,direction=direction,authoredDirection=direction,
        sourceMode='aseprite-cleanup'if action=='pistol_shot'else'pixellab-v3',category='enemies',width=160,height=160,
        loop=action=='walk',flipX=False,displayScale=1,groundPivot={'x':80,'y':128},hitPoint={'x':80,'y':93},
        inputs=[dict(file=str(p),offsetX=16,offsetY=oy,durationMs=t)for p,t in zip(paths,times)],
        events=[],sockets=[],originalSource=str(source),reviewNotes=notes)
shots=[]
for direction in ['south','south-east']:
    folder=ROOT/'source/actors/enemy_weapon_qa/pirate/pistol_shot'/direction
    meta=json.loads((folder/'native-cleanup.json').read_text(encoding='utf-8'))
    paths=sorted((folder/'frames').glob('*.png'));times=meta['durationsMs'];assert len(paths)==8 and sum(times)==930 and sum(times[:3])==440
    x,y=meta['muzzle']
    with Image.open(paths[3])as image:assert image.convert('RGBA').getpixel((x,y))[3]>200
    clip=clip_base('pistol_shot',direction,paths,times,
        'Own aimed V2 view. Baked flash removed. South retains a foreshortened muzzle toward the viewer; covered flash pose replaced by previous clean aim. South-east forearm and firearm alone tilted toward lower-right around its real elbow; body not rotated or mirrored. Exact native barrel socket on release.',meta['originalSource'])
    point={'x':x+16,'y':y+clip['inputs'][0]['offsetY']}
    clip['sockets']=[dict(name='pistol_muzzle',frameIndex=3,position=point)]
    clip['events']=[dict(name='release_projectile',frameIndex=3,position=point,socketName='pistol_muzzle')]
    shots.append(clip)
walk=[]
for direction in DIRS:
    folder=ROOT/'raw/actors'/f'pirate_walk_{direction.replace("-","_")}_direct_v1/job_00/frames'
    paths=[folder/f'{i:03}.png'for i in range(1,9)]
    walk.append(clip_base('walk',direction,paths,[100]*8,
        'Eight actual generated views independently inspected. Hat, face/back, coat and both held weapons retain their own direction; alternating foot contacts. Native128 source padded to160 without resampling.',folder))
for name,clips in [('pirate-specials-two-qa',shots),('pirate-walk-eight-qa',walk)]:
    (ROOT/'review/manifests'/f'{name}.json').write_text(json.dumps(dict(root=str(ROOT),schemaVersion=1,clips=clips),indent=2),encoding='utf-8')
print('READY: pirate 2 shots and 8 walks. Exact release pixels, 930ms/440ms timing, native canvas verified.')
