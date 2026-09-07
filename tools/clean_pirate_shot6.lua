-- Six accepted actual firing views. Projectile/muzzle FX remain separate runtime objects.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local directions={'east','north-east','north','north-west','west','south-west'}
local release={east={107,51},['north-east']={103,46},north={85,34},['north-west']={33,37},west={23,47},['south-west']={24,45}}
local times={120,150,170,60,80,130,100,120}
local function col(r,g,b)return Color{r=r,g=g,b=b,a=255}end
local function line(im,x1,y1,x2,y2,c)
 local n=math.max(math.abs(x2-x1),math.abs(y2-y1));for i=0,n do im:drawPixel(math.floor(x1+(x2-x1)*i/n+.5),math.floor(y1+(y2-y1)*i/n+.5),c)end
end
local function barrel(im,x1,y1,x2,y2)
 line(im,x1,y1+1,x2,y2+1,col(46,43,35));line(im,x1,y1,x2,y2,col(119,116,94));line(im,x1,y1-1,x2,y2-1,col(87,87,74))
 im:drawPixel(x2,y2,col(44,42,34))
end
local function removeFlecks(im)
 local seen={}
 for y=0,127 do for x=0,127 do local key=y*128+x
  if not seen[key]and app.pixelColor.rgbaA(im:getPixel(x,y))>0 then
   local q={{x,y}};local head=1;seen[key]=true
   while head<=#q do local p=q[head];head=head+1
    for dy=-1,1 do for dx=-1,1 do local xx,yy=p[1]+dx,p[2]+dy;local k=yy*128+xx
     if xx>=0 and yy>=0 and xx<128 and yy<128 and not seen[k]and app.pixelColor.rgbaA(im:getPixel(xx,yy))>0 then seen[k]=true;q[#q+1]={xx,yy}end
    end end
   end
   if #q<=12 then for _,p in ipairs(q)do im:drawPixel(p[1],p[2],0)end end
  end
 end end
end
for _,direction in ipairs(directions)do
 local folder=root..'raw/actors/pirate_pistol_shot_'..direction:gsub('-','_')..'_direct_v1/job_00/frames/'
 local output=root..'source/actors/enemy_weapon_qa/pirate/pistol_shot/'..direction..'/';app.fs.makeAllDirectories(output..'frames')
 local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='Actual '..direction..' firing - baked flashes removed'
 for i=1,8 do
  local frame=i==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=times[i]/1000
  local source=app.open(folder..string.format('%03d.png',i));local im=Image(128,128,ColorMode.RGB);im:drawSprite(source,1);source:close()
  for y=0,127 do for x=0,127 do local c=Color(im:getPixel(x,y))
   local white=c.red>200 and c.green>200 and c.blue>200 and math.max(c.red,c.green,c.blue)-math.min(c.red,c.green,c.blue)<30
   local red=c.red>160 and c.green<105 and c.blue<100
   local yellow=((direction=='north-west'and x<50 and y<55)or(direction=='north'and x>73 and y<47))and c.red>195 and c.green>180 and c.blue<160
   if c.alpha>0 and(white or red or yellow)then im:drawPixel(x,y,0)end
  end end
  if direction=='north-west'and i==4 then for y=20,33 do for x=23,34 do im:drawPixel(x,y,0)end end end
  if direction=='north-west'and i==5 then for y=15,41 do for x=20,43 do im:drawPixel(x,y,0)end end end
  if direction=='north-west'and i==5 then for y=15,27 do for x=44,47 do im:drawPixel(x,y,0)end end end
  if direction=='north'and i==5 then for y=26,42 do for x=78,91 do im:drawPixel(x,y,0)end end end
  if direction=='west'and i==4 then for y=43,50 do for x=18,22 do im:drawPixel(x,y,0)end end end
  if i==4 then
   if direction=='east'then barrel(im,86,56,107,51)
   elseif direction=='north-east'then barrel(im,95,47,103,46)
   elseif direction=='north'then barrel(im,81,43,85,34)
   elseif direction=='north-west'then barrel(im,44,43,33,37)
   elseif direction=='west'then barrel(im,38,48,23,47)
   elseif direction=='south-west'then barrel(im,35,49,24,45)end
  end
  if i==5 and direction=='north-west'then barrel(im,43,43,30,37)end
  if i==5 and direction=='north'then barrel(im,78,46,82,35)end
  removeFlecks(im)
  if i==1 then sprite.cels[1].image=im else sprite:newCel(sprite.layers[1],frame,im)end
  local flat=Sprite(128,128,ColorMode.RGB);flat.cels[1].image=Image(im);flat:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));flat:close()
 end
 sprite:newTag(1,8).name='pistol_shot';sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(output..'animation.gif')
 local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode({durationsMs=times,releaseFrameIndex=3,muzzle=release[direction],originalSource=folder}));mf:close()
 local contact=Sprite(1024,512,ColorMode.RGB);local image=contact.cels[1].image
 for y=0,511 do for x=0,1023 do image:drawPixel(x,y,col(13,23,37))end end
 for i=1,8 do local flat=Image(128,128,ColorMode.RGB);flat:drawSprite(sprite,i);local ox=((i-1)%4)*256;local oy=math.floor((i-1)/4)*256
  for y=0,127 do for x=0,127 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 contact:saveCopyAs(root..'review/enemy-weapons/pirate_pistol_shot_'..direction..'_clean_2x.png');contact:close();sprite:close()
end
