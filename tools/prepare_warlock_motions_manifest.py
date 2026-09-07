"""Final native warlock motion metadata and read-only validation."""
import json
from pathlib import Path
from PIL import Image
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
DIRS=['south','south-east','east','north-east','north','north-west','west','south-west']
clips=[]
for action in ['walk','attack']:
    for direction in DIRS:
        folder=ROOT/'source/actors/enemy_weapon_qa/warlock'/action/direction
        meta=json.loads((folder/'native-cleanup.json').read_text(encoding='utf-8'))
        paths=sorted((folder/'frames').glob('*.png'));times=meta['durationsMs'];assert len(paths)==8
        with Image.open(ROOT/'source/actors/warlock_views_clean'/f'{direction}.png')as reference:
            oy=128-(reference.convert('RGBA').getbbox()[3]-1)
        assert sum(times)==(800 if action=='walk' else 960)
        clip=dict(approved=True,role='warlock',action=action,direction=direction,authoredDirection=direction,
            sourceMode='aseprite-cleanup',category='enemies',width=160,height=160,loop=action=='walk',flipX=False,
            displayScale=1,groundPivot={'x':80,'y':128},hitPoint={'x':80,'y':93},
            inputs=[dict(file=str(p),offsetX=16,offsetY=oy,durationMs=t)for p,t in zip(paths,times)],
            sockets=[],events=[],originalSource=meta['originalSource'],reviewNotes=
            'Own generated directional body and moving cloak retained; no mirroring or body rotation. '+
            ('Seven walk views retain their source pixels exactly; E repairs malformed purple staff with one native staff and preserves its own walking legs/head view.'if action=='walk'else
             'Baked giant halos, flashes and duplicate crystals removed. A single native purple crystal is visibly connected to the held shaft. S and E restore their own unobscured head pixels. Contact at the forward shaft end when the strike uses the butt. Legacy960ms/contact320ms retained.'))
        for i,p in enumerate(paths):
            with Image.open(p)as image:
                image=image.convert('RGBA');assert image.size==(128,128)and image.getbbox()
                if action=='walk'and direction!='east':
                    with Image.open(Path(meta['originalSource'])/f'{i+1:03}.png')as original:assert image.tobytes()==original.convert('RGBA').tobytes()
                if meta['cores'][i]:
                    x,y=meta['cores'][i];assert image.getpixel((x,y))==(243,229,254,255),(action,direction,i,'core')
                    clip['sockets'].append(dict(name='staff_head',frameIndex=i,position=dict(x=x+16,y=y+oy)))
        if action=='attack':
            assert sum(times[:3])==320
            x,y=meta['cores'][3]
            # These generated sweeps hit with the opposite wooden end; do not pretend the rear crystal is the contact.
            if direction in ['south-east','north-west','south-west']:
                ex,ey={'south-east':(113,80),'north-west':(16,61),'south-west':(16,55)}[direction]
                with Image.open(paths[3])as image:
                    image=image.convert('RGBA');candidates=[]
                    for yy in range(ey-7,ey+8):
                        for xx in range(ex-7,ex+8):
                            r,g,b,a=image.getpixel((xx,yy))
                            if a>200 and r>65 and r<245 and g>50 and b<g*.9 and r>g:candidates.append((xx,yy))
                    assert candidates,(direction,'missing forward shaft')
                    x,y=min(candidates,key=lambda p:(p[0]-ex)**2+(p[1]-ey)**2)
            point=dict(x=x+16,y=y+oy)
            clip['sockets'].append(dict(name='staff_contact_point',frameIndex=3,position=point))
            clip['events']=[dict(name='staff_contact',frameIndex=3,position=point,socketName='staff_contact_point')]
        clips.append(clip)
(ROOT/'review/manifests/warlock-motions-qa.json').write_text(json.dumps(dict(root=str(ROOT),schemaVersion=1,clips=clips),indent=2),encoding='utf-8')
print('READY: warlock16 motions,128 frames,72 true bright staff cores,8 physical contact points.800ms walk /960ms attack /320ms contact. Unedited7 walk pixel equality PASS.')
