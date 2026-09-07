-- Native staff cleanup. Eight real directional bodies; no mirrored or rotated character views.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local dirs={'south','south-east','east','north-east','north','north-west','west','south-west'}
local cores={
 south={{89,31},{90,23},{107,77},{109,77},{105,79},{96,39},{90,25},{89,31}},
 ['south-east']={{84,31},{45,32},{28,55},{26,57},{21,54},{104,57},{97,38},{84,31}},
 east={{86,34},{86,30},{96,93},{105,93},{101,77},{99,80},{86,47},{86,34}},
 ['north-east']={{49,25},{58,15},{111,78},{109,84},{106,81},{105,79},{45,30},{45,30}},
 north={{48,23},{59,17},{97,51},{98,73},{96,62},{77,25},{46,27},{43,32}},
 ['north-west']={{48,32},{51,21},{101,70},{100,72},{101,69},{96,34},{45,30},{43,32}},
 west={{69,31},{72,33},{23,69},{18,70},{20,75},{28,57},{90,40},{69,32}},
 ['south-west']={{96,41},{82,98},{108,77},{106,82},{102,81},{107,78},{96,40},{83,32}}
}
local times={100,110,110,100,100,140,150,150}
local function C(r,g,b)return Color{r=r,g=g,b=b,a=255}end
local function line(im,x1,y1,x2,y2,c)
 local n=math.max(math.abs(x2-x1),math.abs(y2-y1),1)
 for i=0,n do local x=math.floor(x1+(x2-x1)*i/n+.5);local y=math.floor(y1+(y2-y1)*i/n+.5)
  if x>=0 and x<128 and y>=0 and y<128 then im:drawPixel(x,y,c)end
 end
end
local function load(file)local src=app.open(file);local im=Image(128,128,ColorMode.RGB);im:drawSprite(src,1);src:close();return im end
local function clearFx(im,dir)
 for y=0,127 do for x=0,127 do local c=Color(im:getPixel(x,y))
  local white=c.red>140 and c.green>140 and c.blue>140 and math.max(c.red,c.green,c.blue)-math.min(c.red,c.green,c.blue)<40
  local purple=c.blue>c.red+14 and c.blue>c.green+22 and c.blue>40
  local blue=c.green<110 and c.blue>c.red+35 and c.blue>c.green+35
  local pink=(c.red>180 and c.blue>200 and c.green<225)or(c.red>c.green+25 and c.blue>c.green+25 and c.blue>c.red-10)or(math.min(c.red,c.blue)>c.green+45)
  local yellow=(c.red>245 and c.green>235 and c.blue<205)or(dir=='north-west'and c.red>=238 and c.red<=242 and c.green>=228 and c.green<=232 and c.blue>=175 and c.blue<=179)
  if c.alpha>0 and(white or purple or blue or pink or yellow)then im:drawPixel(x,y,0)end
 end end
end
local function flecks(im)
 local seen={}
 for y=0,127 do for x=0,127 do local key=y*128+x
  if not seen[key]and app.pixelColor.rgbaA(im:getPixel(x,y))>0 then
   local q={{x,y}};local h=1;seen[key]=true
   while h<=#q do local p=q[h];h=h+1
    for dy=-1,1 do for dx=-1,1 do local xx,yy=p[1]+dx,p[2]+dy;local k=yy*128+xx
     if xx>=0 and yy>=0 and xx<128 and yy<128 and not seen[k]and app.pixelColor.rgbaA(im:getPixel(xx,yy))>0 then seen[k]=true;q[#q+1]={xx,yy}end
    end end
   end
   if #q<=32 then for _,p in ipairs(q)do im:drawPixel(p[1],p[2],0)end end
  end
 end end
end
local function crystal(im,cx,cy)
 for dy=-6,6 do for dx=-3,3 do
  if math.abs(dx)/3+math.abs(dy)/6<=1 then im:drawPixel(cx+dx,cy+dy,C(159,106,211))end
  if math.abs(dx)/1.5+math.abs(dy)/4<=1 then im:drawPixel(cx+dx,cy+dy,C(226,191,246))end
 end end
 im:drawPixel(cx,cy,C(243,229,254))
end
for _,action in ipairs({'walk','attack'})do for _,dir in ipairs(dirs)do
 local input=root..'raw/actors/warlock_'..action..'_'..dir:gsub('-','_')..'_direct_v1/job_00/frames/'
 local output=root..'source/actors/enemy_weapon_qa/warlock/'..action..'/'..dir..'/';app.fs.makeAllDirectories(output..'frames')
 local reference=load(root..'source/actors/warlock_views_clean/'..dir..'.png')
 local sp=Sprite(128,128,ColorMode.RGB);sp.layers[1].name='True '..dir..' body - native single staff cleanup'
 local staffPoints={}
 for i=1,8 do
  local im=load(input..string.format('%03d.png',i));local bottom=Image(128,128,ColorMode.RGB);local grip=Image(128,128,ColorMode.RGB)
  if action=='attack'or dir=='east'then
   clearFx(im,dir)
   if dir=='east'or(action=='attack'and dir=='south'and i>=3 and i<=5)then
    for y=10,55 do for x=47,(dir=='south'and 80 or 84)do im:drawPixel(x,y,reference:getPixel(x,y))end end
    if dir=='east'then for y=0,55 do for x=85,94 do im:drawPixel(x,y,0)end end end
   end
   local cx,cy
   if action=='walk'then cx=({86,87,88,89,88,86,85,86})[i];cy=({34,32,31,32,35,36,35,34})[i]
   else cx,cy=cores[dir][i][1],cores[dir][i][2]end
   local full=(dir=='east'and(action=='walk'or i<=2 or i==8))or(dir=='south-east'and i==1)or(dir=='north-east'and i>=7)
   if full then
    local gx,gy=(dir=='north-east'and 47 or(dir=='east'and 86 or 78)),82
    if dir=='east'then
     for y=56,100 do for x=77,84 do local c=Color(im:getPixel(x,y));if c.red<65 and c.green<60 and c.blue<90 then im:drawPixel(x,y,0)end end end
    end
    local tx=math.floor(gx+(gx-cx)*.65+.5);local ty=math.min(120,math.floor(gy+(gy-cy)*.65+.5))
    line(bottom,tx+1,ty,cx+1,cy+5,C(59,48,33));line(bottom,tx,ty,cx,cy+5,C(142,128,82))
    if dir=='east'then line(grip,69,82,gx-1,81,C(70,61,42));line(grip,69,81,gx,80,C(211,196,132));for y=80,83 do grip:drawPixel(gx,y,C(223,208,144))end end
   else
    local near=nil;local distance=99999
    for y=math.max(0,cy-18),math.min(127,cy+18)do for x=math.max(0,cx-18),math.min(127,cx+18)do local c=Color(im:getPixel(x,y))
     if c.alpha>200 and c.red>65 and c.red<190 and c.green>50 and c.blue<c.green*.9 and c.red>c.green then local d=(x-cx)^2+(y-cy)^2;if d<distance then distance=d;near={x,y}end end
    end end
    if near then line(bottom,near[1]+1,near[2],cx+1,cy,C(59,48,33));line(bottom,near[1],near[2],cx,cy,C(142,128,82))end
   end
   crystal(bottom,cx,cy)
   local flat=Image(128,128,ColorMode.RGB);flat:drawImage(bottom);flat:drawImage(im);flat:drawImage(grip);im=flat
   -- Attach one visible crystal to the real held shaft; old translucent FX outlines cannot obscure it.
   flecks(im)
   local nearest=nil;local distance=99999
   for y=math.max(0,cy-32),math.min(127,cy+32)do for x=math.max(0,cx-32),math.min(127,cx+32)do local c=Color(im:getPixel(x,y))
    if c.alpha>200 and c.red>65 and c.red<245 and c.green>50 and c.blue<c.green*.9 and c.red>c.green then local d=(x-cx)^2+(y-cy)^2;if d<distance then distance=d;nearest={x,y}end end
   end end
   if nearest then line(im,nearest[1]+1,nearest[2],cx+1,cy,C(59,48,33));line(im,nearest[1],nearest[2],cx,cy,C(142,128,82))end
   crystal(im,cx,cy);staffPoints[i]={cx,cy}
   if action=='attack'and dir=='south'and i==3 then
    line(im,13,61,44,66,C(59,48,33));line(im,13,60,44,65,C(142,128,82))
   end
  else staffPoints[i]={}end
  local frame=i==1 and sp.frames[1]or sp:newEmptyFrame();frame.duration=(action=='walk'and 100 or times[i])/1000
  if i==1 then sp.cels[1].image=im else sp:newCel(sp.layers[1],frame,im)end
  local one=Sprite(128,128,ColorMode.RGB);one.cels[1].image=Image(im);one:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));one:close()
 end
 sp:newTag(1,8).name=action;sp:saveAs(output..'animation.aseprite');sp:saveCopyAs(output..'animation.gif')
 local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode({durationsMs=action=='walk'and{100,100,100,100,100,100,100,100}or times,cores=staffPoints,contactFrameIndex=3,originalSource=input}));mf:close()
 local sheet=Sprite(1024,512,ColorMode.RGB);local out=sheet.cels[1].image
 for y=0,511 do for x=0,1023 do out:drawPixel(x,y,C(13,23,37))end end
 for i=1,8 do local im=Image(128,128,ColorMode.RGB);im:drawSprite(sp,i);local ox=((i-1)%4)*256;local oy=math.floor((i-1)/4)*256
  for y=0,127 do for x=0,127 do local c=Color(im:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do out:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 sheet:saveCopyAs(root..'review/enemy-weapons/warlock_'..action..'_'..dir..'_clean_2x.png');sheet:close();sp:close()
end end
