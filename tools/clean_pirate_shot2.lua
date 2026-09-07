-- Approved south and south-east retrials. Native weapon-only cleanup; bodies keep their own views.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local times={120,150,170,60,80,130,100,120}
local function col(r,g,b)return Color{r=r,g=g,b=b,a=255}end
for _,direction in ipairs({'south','south-east'})do
 local sourceFolder=root..'raw/actors/pirate_pistol_shot_'..direction:gsub('-','_')..'_aimed_v2/job_00/frames/'
 local output=root..'source/actors/enemy_weapon_qa/pirate/pistol_shot/'..direction..'/'
 app.fs.makeAllDirectories(output..'frames')
 local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='Own firing view; flash separate; directional gun correction'
 for i=1,8 do
  local frame=i==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=times[i]/1000
  -- The south flash covers the held weapon: hold the immediately preceding clear aim pose.
  local n=(direction=='south'and i==5)and 4 or i
  local src=app.open(sourceFolder..string.format('%03d.png',n));local im=Image(128,128,ColorMode.RGB);im:drawSprite(src,1);src:close()
  if direction=='south-east'then
   for y=0,127 do for x=0,127 do local c=Color(im:getPixel(x,y))
    if c.alpha>0 and ((c.red>195 and c.green>190 and c.blue>175)or(c.red>195 and c.green>170 and c.blue<100))then im:drawPixel(x,y,0)end
   end end
   local angle=({0,25,35,35,35,12,0,0})[i]*math.pi/180
   if angle~=0 then
    local arm=Image(im)
    for y=39,75 do for x=82,124 do im:drawPixel(x,y,0)end end
    for y=30,99 do for x=77,127 do
     local dx,dy=x-82,y-58
     local sx=math.floor(82+dx*math.cos(angle)+dy*math.sin(angle)+.5)
     local sy=math.floor(58-dx*math.sin(angle)+dy*math.cos(angle)+.5)
     if sx>=82 and sx<=124 and sy>=39 and sy<=75 then local c=Color(arm:getPixel(sx,sy));if c.alpha>0 then im:drawPixel(x,y,c)end end
    end end
   end
   if i==4 then im:drawPixel(111,70,col(43,42,34))end
  elseif i==4 or i==5 then
   -- A compact foreshortened barrel faces the viewer; no baked screen-wide flash.
   for dy=-2,2 do for dx=-2,2 do if dx*dx+dy*dy<=5 then im:drawPixel(64+dx,64+dy,col(109,104,84))end end end
   im:drawPixel(64,64,col(38,35,29));im:drawPixel(64,65,col(38,35,29))
  end
  if direction=='south-east'then
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
  if i==1 then sprite.cels[1].image=im else sprite:newCel(sprite.layers[1],frame,im)end
  local flat=Sprite(128,128,ColorMode.RGB);flat.cels[1].image=Image(im);flat:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));flat:close()
 end
 sprite:newTag(1,8).name='pistol_shot';sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(output..'animation.gif')
 local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode({durationsMs=times,releaseFrameIndex=3,muzzle=direction=='south'and{64,64}or{111,70},originalSource=sourceFolder}));mf:close()
 local contact=Sprite(1024,512,ColorMode.RGB);local out=contact.cels[1].image
 for y=0,511 do for x=0,1023 do out:drawPixel(x,y,col(13,23,37))end end
 for i=1,8 do local im=Image(128,128,ColorMode.RGB);im:drawSprite(sprite,i);local ox=((i-1)%4)*256;local oy=math.floor((i-1)/4)*256
  for y=0,127 do for x=0,127 do local c=Color(im:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do out:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 contact:saveCopyAs(root..'review/enemy-weapons/pirate_pistol_shot_'..direction..'_clean_2x.png');contact:close();sprite:close()
end
