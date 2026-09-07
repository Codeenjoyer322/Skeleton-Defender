"""Read-only pixel QC; writes coordinate metadata, never changes a raster."""
import json
from pathlib import Path
from PIL import Image
from collections import deque
ROOT=Path(r'C:/Users/Life/Desktop/Testnew2/NeonGothic')
data={}
for direction in ['south','south-east','north-east','north','north-west','west','south-west']:
    results=[]
    for index in range(1,9):
        path=ROOT/'raw/actors'/f'circe_attack_{direction.replace("-","_")}_v3/job_00/frames/{index:03}.png'
        im=Image.open(path).convert('RGBA');px=im.load()
        selected={(x,y)for y in range(im.height)for x in range(im.width)if px[x,y][3]>200 and ((px[x,y][1]>130 and px[x,y][2]>150 and px[x,y][2]>px[x,y][0]+8)or min(px[x,y][:3])>220)}
        components=[]
        while selected:
            seed=selected.pop();q=deque([seed]);component=[seed]
            while q:
                x,y=q.popleft()
                for dx in [-1,0,1]:
                    for dy in [-1,0,1]:
                        p=x+dx,y+dy
                        if p in selected:selected.remove(p);q.append(p);component.append(p)
            cyan=[p for p in component if px[p][2]>px[p][0]+8 and px[p][1]>130]
            if cyan:components.append((len(cyan),component))
        if not components:results.append(None);continue
        _,component=max(components)
        core=[p for p in component if min(px[p][:3])>220] or component
        cx=round(sum(p[0]for p in core)/len(core));cy=round(sum(p[1]for p in core)/len(core))
        bounds=[min(p[0]for p in component),min(p[1]for p in component),max(p[0]for p in component),max(p[1]for p in component)]
        results.append({'center':[cx,cy],'bounds':bounds})
    data[direction]=results
print(json.dumps(data,indent=2))
(ROOT/'review/circe-attack-crystal-qc.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
