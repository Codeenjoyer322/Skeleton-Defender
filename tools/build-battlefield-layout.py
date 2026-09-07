"""Generate shared geometry only; Aseprite draws all raster artwork."""
import json, math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
control = [(50,120),(150,154),(150,330),(370,330),(370,130),
           (585,130),(585,450),(800,450),(800,280),(930,280),(1000,234)]
sites = [(66,258),(257,224),(253,438),(469,270),(471,422),
         (698,242),(489,559),(717,559),(912,407),(890,210)]

def lerp(a,b,t): return tuple(x+(y-x)*t for x,y in zip(a,b))
def distance(a,b): return math.hypot(a[0]-b[0],a[1]-b[1])
path=[control[0]]
def line(end):
    start=path[-1]
    for i in range(1,math.ceil(distance(start,end)/8)+1):
        path.append(lerp(start,end,i/math.ceil(distance(start,end)/8)))
for i in range(1,len(control)-1):
    a,b,c=control[i-1:i+2]
    cut=min(50,distance(a,b)*.3,distance(b,c)*.3)
    entry=lerp(b,a,cut/distance(a,b)); leave=lerp(b,c,cut/distance(b,c))
    line(entry)
    for j in range(1,13):
        t=j/12
        path.append(lerp(lerp(entry,b,t),lerp(b,leave,t),t))
line(control[-1])
def xy(p):return dict(x=round(p[0],4),y=round(p[1],4))
data=dict(schemaVersion=1,width=1056,height=640,artWidth=528,artHeight=320,
          roadWidth=52,controlPoints=list(map(xy,control)),
          path=list(map(xy,path)),sites=list(map(xy,sites)),
          enemyGate=xy(control[0]),castleGate=xy(control[-1]))
dest=ROOT/'Assets/Resources/NeonGothic/battlefield-layout.json'
dest.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
length=sum(distance(a,b) for a,b in zip(path,path[1:]))
print(json.dumps(dict(points=len(path),sites=len(sites),length=round(length,2))))
