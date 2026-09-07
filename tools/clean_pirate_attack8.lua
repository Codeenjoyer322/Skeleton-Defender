-- Native blade/contact cleanup over independently authored directional bodies.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local dirs={'south','south-east','east','north-east','north','north-west','west','south-west'}
local poses={
 south={{64,81,64,97},{58,74,44,54},{63,75,66,54},{64,79,65,103},{64,79,65,100},{80,81,90,104},{80,83,94,97},{80,83,94,97}},
 ['south-east']={{85,80,104,91},{86,72,106,63},{77,68,90,48},{62,69,86,85},{62,69,83,82},{77,71,94,80},{83,80,102,90},{85,80,104,91}},
 east={{68,78,78,94},{73,75,88,90},{88,72,107,60},{88,68,99,45},{85,77,111,79},{82,77,103,86},{75,77,89,95},{68,78,78,94}},
 ['north-east']={{81,81,94,99},{84,79,104,81},{88,69,93,47},{87,51,99,29},{86,61,104,46},{86,69,95,48},{84,79,101,81},{81,81,94,99}},
 north={{85,81,94,99},{81,66,97,56},{79,47,92,32},{73,43,73,18},{68,46,60,24},{66,54,53,39},{81,74,91,87},{85,81,94,99}},
 ['north-west']={{83,78,94,99},{85,74,96,92},{71,49,88,36},{52,63,35,43},{42,68,33,43},{58,69,45,51},{80,76,93,93},{83,78,94,99}},
 west={{49,81,36,96},{51,72,26,69},{47,57,35,37},{48,65,23,62},{48,65,25,65},{53,75,41,52},{52,81,37,95},{49,81,36,96}},
 ['south-west']={{76,81,87,98},{51,59,49,35},{51,60,49,35},{42,68,22,84},{42,68,24,83},{51,71,34,54},{42,80,27,91},{76,81,87,98}}
}
local order={west={1,2,3,3,3,6,7,8},['south-west']={1,3,4,2,2,6,7,8}}
local function C(r,g,b)return Color{r=r,g=g,b=b,a=255}end
local function line(im,x1,y1,x2,y2,c,width)
 local n=math.max(math.abs(x2-x1),math.abs(y2-y1));if n<1 then n=1 end
 for i=0,n do local x=math.floor(x1+(x2-x1)*i/n+.5);local y=math.floor(y1+(y2-y1)*i/n+.5)
  for dy=-width,width do for dx=-width,width do if x+dx>=0 and x+dx<128 and y+dy>=0 and y+dy<128 then im:drawPixel(x+dx,y+dy,c)end end end
 end
end
local function blade(im,p)
 local x,y,tx,ty=table.unpack(p);local dx,dy=tx-x,ty-y;local l=math.sqrt(dx*dx+dy*dy);dx=dx/l;dy=dy/l
 line(im,x+dx*3,y+dy*3,tx,ty,C(39,44,47),1)
 line(im,x+dx*4,y+dy*4,tx,ty,C(142,157,161),0)
 line(im,x+dx*5-dy,y+dy*5+dx,tx-dx*2,ty-dy*2,C(197,210,204),0)
 line(im,x-dy*3,y+dx*3,x+dy*3,y-dx*3,C(128,96,41),0)
 im:drawPixel(math.floor(x),math.floor(y),C(209,190,142))
end
for _,dir in ipairs(dirs)do
 local input=root..'raw/actors/pirate_attack_'..dir:gsub('-','_')..'_direct_v1/job_00/frames/'
 local output=root..'source/actors/enemy_weapon_qa/pirate/attack/'..dir..'/';app.fs.makeAllDirectories(output..'frames')
 local times=dir=='east'and{80,80,70,70,90,150,150,160}or{100,100,100,90,110,110,110,130}
 local cue=dir=='east'and 4 or 3
 local sp=Sprite(128,128,ColorMode.RGB);sp.layers[1].name='Actual directional body and native contact blade'
 for i=1,8 do
  local n=order[dir]and order[dir][i]or i
  local src=app.open(input..string.format('%03d.png',n));local im=Image(128,128,ColorMode.RGB);im:drawSprite(src,1);src:close()
  local mask={}
  for y=0,127 do for x=0,127 do local c=Color(im:getPixel(x,y))
   if c.alpha>0 and c.red>145 and c.green>155 and c.blue>145 and math.max(c.red,c.green,c.blue)-math.min(c.red,c.green,c.blue)<37 then
    for dy=-1,1 do for dx=-1,1 do if x+dx>=0 and x+dx<128 and y+dy>=0 and y+dy<128 then mask[(y+dy)*128+x+dx]=true end end end
   end
  end end
  local idle=nil
  if dir=='south'then local src=app.open(input..'001.png');idle=Image(128,128,ColorMode.RGB);idle:drawSprite(src,1);src:close()end
  for key,_ in pairs(mask)do local x,y=key%128,math.floor(key/128);im:drawPixel(x,y,0)
   if idle and x>=49 and x<=78 and y>=49 then im:drawPixel(x,y,idle:getPixel(x,y))end
  end
  local clear=Color{r=0,g=0,b=0,a=0}
  if dir=='south-east'and i==2 then line(im,90,72,108,71,clear,2)end
  if dir=='south-east'and i==6 then line(im,86,72,98,74,clear,2)end
  if dir=='south-east'and(i==4 or i==5)then
   line(im,57,67,27,62,clear,3)
   line(im,50,61,46,72,C(64,48,30),2);line(im,50,62,46,72,C(202,183,135),1)
  end
  if dir=='north-east'and i==4 then
   line(im,89,47,93,35,clear,2)
   for y=32,44 do for x=80,87 do im:drawPixel(x,y,0)end end
  end
  if dir=='west'and i==2 then line(im,47,72,27,68,clear,2)end
  if dir=='south-west'and(i==2 or i==3)then line(im,44,53,42,25,clear,2)end
  if dir=='south-west'and(i==4 or i==5)then for y=64,77 do for x=13,39 do im:drawPixel(x,y,0)end end end
  if dir=='north'then
   -- The generated blade floated across the neck without a grip: rebuild the occluded forearm.
   if i>=2 and i<=6 then
    for y=63,104 do for x=79,98 do im:drawPixel(x,y,0)end end
    if i==6 then for y=39,53 do for x=25,55 do im:drawPixel(x,y,0)end end end
    local p=poses[dir][i];line(im,76,54,p[1],p[2],C(72,56,34),2);line(im,76,54,p[1],p[2],C(196,178,130),1)
   end
  end
  if dir=='west'and(i==4 or i==5)then for y=38,55 do for x=33,46 do im:drawPixel(x,y,0)end end end
  if dir=='south'and i==2 then for y=71,78 do for x=31,51 do im:drawPixel(x,y,0)end end end
  -- Only the attack weapon is painted; unchanged source bodies establish all eight genuine views.
  if i>=2 and i<=7 and not(i==7 and(dir=='south'or dir=='south-east'or dir=='north-east'or dir=='south-west'))then blade(im,poses[dir][i])end
  local seen={}
  for y=0,127 do for x=0,127 do local key=y*128+x
   if not seen[key]and app.pixelColor.rgbaA(im:getPixel(x,y))>0 then
    local q={{x,y}};local h=1;seen[key]=true
    while h<=#q do local p=q[h];h=h+1
     for dy=-1,1 do for dx=-1,1 do local xx,yy=p[1]+dx,p[2]+dy;local k=yy*128+xx
      if xx>=0 and yy>=0 and xx<128 and yy<128 and not seen[k]and app.pixelColor.rgbaA(im:getPixel(xx,yy))>0 then seen[k]=true;q[#q+1]={xx,yy}end
     end end
    end
    if #q<=16 then for _,p in ipairs(q)do im:drawPixel(p[1],p[2],0)end end
   end
  end end
  local frame=i==1 and sp.frames[1]or sp:newEmptyFrame();frame.duration=times[i]/1000
  if i==1 then sp.cels[1].image=im else sp:newCel(sp.layers[1],frame,im)end
  local one=Sprite(128,128,ColorMode.RGB);one.cels[1].image=Image(im);one:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));one:close()
 end
 sp:newTag(1,8).name='attack';sp:saveAs(output..'animation.aseprite');sp:saveCopyAs(output..'animation.gif')
 local sockets={};for i=1,8 do local p=poses[dir][i];sockets[i]={hand={p[1],p[2]},tip={p[3],p[4]}}end
 local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode({durationsMs=times,contactFrameIndex=cue,sockets=sockets,sourceFrames=order[dir]or{1,2,3,4,5,6,7,8},originalSource=input}));mf:close()
 local sheet=Sprite(1024,512,ColorMode.RGB);local out=sheet.cels[1].image
 for y=0,511 do for x=0,1023 do out:drawPixel(x,y,C(13,23,37))end end
 for i=1,8 do local im=Image(128,128,ColorMode.RGB);im:drawSprite(sp,i);local ox=((i-1)%4)*256;local oy=math.floor((i-1)/4)*256
  for y=0,127 do for x=0,127 do local c=Color(im:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do out:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 sheet:saveCopyAs(root..'review/enemy-weapons/pirate_attack_'..dir..'_clean_2x.png');sheet:close();sp:close()
end
