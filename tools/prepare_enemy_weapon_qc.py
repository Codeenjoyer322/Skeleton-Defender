"""Read-only discovery of completed owned enemy jobs, plus a native Aseprite input plan."""
import json
from pathlib import Path
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
ledger=json.loads((ROOT/'actors-ledger.json').read_text(encoding='utf-8'))
entries={e['id']:e for e in ledger['entries']}
clips=[]
for role in ['pirate','warlock']:
    for action in ['walk','attack']+(['pistol_shot']if role=='pirate'else['lightning_cast']):
        for direction in ['south','south-east','east','north-east','north','north-west','west','south-west']:
            job=f'{role}_{action}_{direction.replace("-","_")}_direct_v1'
            if entries.get(job,{}).get('status')!='completed':continue
            folder=ROOT/'raw/actors'/job/'job_00/frames'
            files=sorted(folder.glob('*.png'))
            if len(files)==9:files=files[1:]
            if len(files)!=8:continue
            clips.append({'role':role,'action':action,'direction':direction,'jobId':job,'files':[str(p)for p in files]})
for direction in ['south','south-east']:
    job=f'pirate_pistol_shot_{direction.replace("-","_")}_aimed_v2'
    if entries.get(job,{}).get('status')=='completed':
        files=sorted((ROOT/'raw/actors'/job/'job_00/frames').glob('*.png'))
        if len(files)==9:files=files[1:]
        if len(files)==8:clips.append({'role':'pirate','action':'pistol_shot_aimed','direction':direction,'jobId':job,'files':[str(p)for p in files]})
out=ROOT/'source/enemy-weapon-qc-inputs.json';out.write_text(json.dumps(clips,indent=2),encoding='utf-8')
print('Owned ready weapon clips:',len(clips))
for clip in clips:print(clip['role'],clip['action'],clip['direction'])
