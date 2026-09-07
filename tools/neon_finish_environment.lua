-- PixelLab originals remain untouched; all pixel finishing is native Aseprite.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local dest='C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/'
app.fs.makeAllDirectories(dest)
local function col(h,a) return Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=a or 255} end
local function save(sp,name)
 sp:saveAs(root..'source/environment/'..name..'.aseprite')
 sp:saveCopyAs(root..'exports/environment/'..name..'.png')
 sp:saveCopyAs(dest..name..'.png');sp:close()
end
local sp=app.open(root..'raw/environment/moonlit_courtyard_menu_v2/000.png')
sp.layers[1].name='PixelLab original - cathedral light and architecture'
save(sp,'moonlit_courtyard_menu')
sp=app.open(root..'raw/environment/cemetery_battlefield/000.png')
sp.layers[1].name='PixelLab original - Gothic cemetery'
local src=sp.cels[1].image; local out=Image(sp.width,sp.height,ColorMode.RGB)
local path={{-16,77},{75,77},{75,165},{185,165},{185,65},{292.5,65},{292.5,225},{400,225},{400,140},{495.5,140}}
local function distance(x,y)
 local best=10000
 for i=1,#path-1 do
  local a,b=path[i],path[i+1]; local dx,dy=b[1]-a[1],b[2]-a[2]
  local t=math.max(0,math.min(1,((x-a[1])*dx+(y-a[2])*dy)/(dx*dx+dy*dy)))
  best=math.min(best,math.sqrt((x-a[1]-dx*t)^2+(y-a[2]-dy*t)^2))
 end
 return best
end
-- Remove the flat cyan guide picked up by generation, including its outer rim.
local mask={}
for y=0,319 do for x=0,527 do
 local c=src:getPixel(x,y); local r,g,b=app.pixelColor.rgbaR(c),app.pixelColor.rgbaG(c),app.pixelColor.rgbaB(c)
 if distance(x,y)<33 and r>86 and r<183 and g>123 and g<206 and b>141 and b<219 and g-r>13 and g-r<55 and b-g<34 then
  for yy=math.max(0,y-3),math.min(319,y+3) do for xx=math.max(0,x-3),math.min(527,x+3) do mask[yy*528+xx]=true end end
 end
end end
local ground={'24394b','283e51','2d4355','213548','31475b'}
local road={'4c6378','536d80','455d73','5c7487','4a6579','5b7185'}
for y=0,319 do for x=0,527 do
 local d=distance(x,y)
 if mask[y*528+x] and d>14 then
  local u=math.floor((x+2*y)/26);local v=math.floor((x-2*y)/26)
  local edge=(x+2*y)%26<2 or (x-2*y)%26<2
  out:drawPixel(x,y,col(edge and '172839' or ground[(u*7+v*13)%#ground+1]))
 end
 if d<=16 then
  local c='0b1527'
  if d<14 then
   if d>11.5 then c=(x+2*y)%12<2 and '263f55' or '3f596f'
   else
    local row=math.floor(y/5);local gx=(x+(row%2)*6)%12;local gy=y%5
    local idx=(math.floor((x+(row%2)*6)/12)*17+row*23)%#road+1
    c=(gx==0 or gy==0) and '293d53' or road[idx]
    if gy==1 and gx>1 then c='688296' end
    if (x*31+y*17)%181==0 then c='243b51' end
   end
  end
  out:drawPixel(x,y,col(c))
 end
end end
local layer=sp:newLayer();layer.name='Aseprite - exact playable route and individual stone blocks'
sp:newCel(layer,sp.frames[1],out,Point(0,0))
local light=Image(528,320,ColorMode.RGB)
local lamps={{58,244,38,22,'ffaa56',65},{446,226,34,22,'ffad58',72},{379,78,28,22,'ffc075',60},{465,55,58,30,'39dfe8',50},{516,12,45,32,'ef4596',42},{296,281,37,22,'1ae5dc',36},{356,280,28,20,'f04eb0',37}}
for _,p in ipairs(lamps) do
 for y=math.max(0,p[2]-p[4]),math.min(319,p[2]+p[4]) do for x=math.max(0,p[1]-p[3]),math.min(527,p[1]+p[3]) do
  local d=((x-p[1])/p[3])^2+((y-p[2])/p[4])^2
  if d<1 then local a=math.floor((1-d)^2*p[6]/4)*4; if a>0 then light:drawPixel(x,y,col(p[5],a)) end end
 end end
end
local lighting=sp:newLayer(); lighting.name='Aseprite - cyan magenta and lantern bounce'; lighting.blendMode=BlendMode.SCREEN
sp:newCel(lighting,sp.frames[1],light,Point(0,0))
save(sp,'cemetery_battlefield')
for _,name in ipairs({'portrait_arch','rune_ornament'}) do
 local s=app.open(root..'source/environment/'..name..'.aseprite');s:saveCopyAs(dest..name..'.png');s:close()
end
local pad=Sprite(68,48,ColorMode.RGB);pad.layers[1].name='Blue slate platform with cyan inlay'
local pi=pad.cels[1].image
for y=0,47 do for x=0,67 do
 local d=math.abs(x-34)/31+math.abs(y-21)/13
 local lower=math.abs(x-34)/31+math.abs(y-26)/13
 if lower<=1 then pi:drawPixel(x,y,col('122139')) end
 if d<=1 then pi:drawPixel(x,y,col(d>.88 and '3a627a' or d>.72 and '203951' or '182b43')) end
 if d>.74 and d<.79 and (x<22 or x>46) then pi:drawPixel(x,y,col(x<34 and '46c7ce' or '946199')) end
end end
save(pad,'building_pad')
print('Environment exported with exact route on its own editable layer.')
