"""Metadata only. Native Aseprite cleanup scripts own all pixel modifications."""
import json
from pathlib import Path
from PIL import Image

ROOT = Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
RAW = ROOT/'raw/actors/achilles_8views_reference_v2'
DIRS = ['south','south-east','east','north-east','north','north-west','west','south-west']
output = ROOT/'review/manifests'
output.mkdir(exist_ok=True)
character = json.loads((RAW/'character.json').read_text(encoding='utf-8'))
clips = []
for direction in DIRS:
    reference = RAW/'rotations'/f'{direction}.png'
    with Image.open(reference) as im:
        assert im.size == (92,92)
        bottom = im.convert('RGBA').getbbox()[3]-1
    offset_x,offset_y = 18,100-bottom
    keypoints = character['skeletons']['2d_references'][direction]['keypoints']
    hit_x = round(keypoints['NECK']['x']*92)+offset_x
    hit_y = round((keypoints['NECK']['y']+(keypoints['LEFT HIP']['y']+keypoints['RIGHT HIP']['y'])/2)*46)+offset_y
    palm = {'x':round(keypoints['RIGHT ARM']['x']*92)+offset_x,'y':round(keypoints['RIGHT ARM']['y']*92)+offset_y}
    for action in ['idle','walk']:
        paths = [reference] if action=='idle' else sorted((ROOT/'source/actors/achilles_walk8_clean'/direction/'frames').glob('*.png'))
        assert len(paths)==(1 if action=='idle' else 4)
        clips.append({'approved':True,'role':'achilles','action':action,'direction':direction,'authoredDirection':direction,
            'sourceMode':'pixellab-v3' if action=='idle' else 'aseprite-cleanup','category':'heroes',
            'width':128,'height':128,'loop':True,'flipX':False,'displayScale':1,
            'groundPivot':{'x':64,'y':100},'hitPoint':{'x':hit_x,'y':hit_y},
            'inputs':[{'file':str(path),'offsetX':offset_x,'offsetY':offset_y,'durationMs':1000 if action=='idle' else 150}for path in paths],
            'events':[],'sockets':[{'name':'weapon_hand','frameIndex':0,'position':palm}] if action=='idle' else [],
            'reviewNotes':'Eight independently authored views. No reflection or body rotation. Fixed padding per direction preserves natural walk bob; no resizing. South helmet/held sword, north crest and east greaves cleaned in native Aseprite.',
            'originalSource':str(RAW/'rotations'/f'{direction}.png') if action=='idle' else str(RAW/'animations/animating'/direction)})
(output/'achilles-idle-walk-qa.json').write_text(json.dumps({'root':str(ROOT),'schemaVersion':1,'clips':clips},indent=2),encoding='utf-8')

centers=[(65,36),(70,35),(80,52),(80,62),(79,62),(80,61),(80,58),(75,39),(65,36)]
durations=[120,160,270,90,100,150,140,50,80]
assert sum(durations)==1160 and sum(durations[:3])==550
paths=sorted((ROOT/'source/actors/circe_staff_attack_east_clean/frames').glob('*.png'))
assert len(paths)==9
for path,(cx,cy) in zip(paths,centers):
    with Image.open(path) as im:
        assert im.size==(104,104)
        r,g,b,a=im.convert('RGBA').getpixel((cx,cy))
        assert a>200 and min(r,g,b)>230,('Socket is not on white crystal core',path)
clip={'approved':True,'role':'circe','action':'staff_attack','direction':'east','authoredDirection':'east',
    'sourceMode':'aseprite-cleanup','category':'heroes','width':128,'height':128,'loop':False,'flipX':False,'displayScale':1,
    'groundPivot':{'x':64,'y':100},'hitPoint':{'x':65,'y':73},
    'inputs':[{'file':str(p),'offsetX':12,'offsetY':12,'durationMs':t}for p,t in zip(paths,durations)],
    'events':[{'name':'release_fireball','frameIndex':3,'position':{'x':92,'y':74},'socketName':'staffTip'}],
    'sockets':[{'name':'staffTip','frameIndex':i,'position':{'x':cx+12,'y':cy+12}}for i,(cx,cy) in enumerate(centers)],
    'reviewNotes':'Stable compact cyan crystal with attached brown stem; separated wisps removed. Extra final ready frame settles recovery. Original body pixels and 1.16s/.55s duration/release preserved.',
    'originalSource':str(ROOT/'raw/actors/circe_8views_reference_v1/animations/staff_attack/east')}
(output/'circe-staff-east-qa.json').write_text(json.dumps({'root':str(ROOT),'schemaVersion':1,'clips':[clip]},indent=2),encoding='utf-8')
print('READY: 16 Achilles idle/walk clips; 1 Circe staff clip. All inputs fit 128px with padding only. Crystal sockets on verified white pixels.')

attack_paths=sorted((RAW/'animations/overhead_cut/east').glob('*.png'))
assert len(attack_paths)==12
attack_times=[100,70,80,80,80,90,60,70,70,60,60,80]
hands=[(62,61),(62,42),(47,34),(47,37),(47,37),(47,34),(50,65),(43,66),(39,67),(37,66),(44,65),(57,62)]
assert sum(attack_times)==900 and sum(attack_times[:6])==500
attack={'approved':True,'role':'achilles','action':'attack','direction':'east','authoredDirection':'east',
    'sourceMode':'pixellab-v3','category':'heroes','width':128,'height':128,'loop':False,'flipX':False,'displayScale':1,
    'groundPivot':{'x':64,'y':100},'hitPoint':{'x':65,'y':71},
    'inputs':[{'file':str(p),'offsetX':12,'offsetY':12,'durationMs':t}for p,t in zip(attack_paths,attack_times)],
    'events':[{'name':'sword_contact','frameIndex':6,'position':{'x':92,'y':83},'socketName':'blade_tip'}],
    'sockets':[{'name':'weapon_hand','frameIndex':i,'position':{'x':x+12,'y':y+12}}for i,(x,y) in enumerate(hands)] +
        [{'name':'blade_tip','frameIndex':6,'position':{'x':92,'y':83}}],
    'reviewNotes':'Visible overhead windup frames1-5, fast front/down stroke at6 and clear follow-through7-10. First attack pilot rejected; this is overhead_cut. Duration900/contact500ms unchanged. Hand socket sampled on gold pixels per frame.',
    'originalSource':str(RAW/'animations/overhead_cut/east')}
for p,(x,y) in zip(attack_paths,hands):
    with Image.open(p) as image:
        r,g,b,a=image.convert('RGBA').getpixel((x,y))
        assert a>200 and r>150 and g>90 and r>b+35,('Hand socket not on visible hand',p)
(output/'achilles-attack-east-qa.json').write_text(json.dumps({'root':str(ROOT),'schemaVersion':1,'clips':[attack]},indent=2),encoding='utf-8')
print('READY: Achilles overhead attack east, 12frames/.9s/.5s contact, 12 independently checked gold-hand sockets.')
