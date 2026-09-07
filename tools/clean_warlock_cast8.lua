-- Eight real directional bodies. Remove baked lightning; maintain one actual staff crystal.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local settings={
 south={indices={0,1,5,5,5,6,7,7},cores={{87,31},{80,23},{79,24},{79,24},{79,24},{82,26},{87,31},{87,31}}},
 ['south-east']={indices={0,1,2,3,4,5,6,7},cores={{89,34},{111,54},{101,89},{104,90},{104,90},{112,76},{90,39},{85,31}}},
 east={indices={0,0,0,0,0,0,0,0},cores={{78,34},{81,30},{91,34},{94,35},{92,34},{86,30},{80,32},{78,34}}},
 ['north-east']={indices={0,1,4,4,4,5,6,7},cores={{47,27},{58,19},{75,21},{75,21},{75,21},{69,19},{51,23},{45,30}}},
 north={indices={0,1,2,2,2,5,6,7},cores={{44,32},{50,26},{64,17},{64,17},{64,17},{65,21},{49,29},{42,31}}},
 ['north-west']={indices={0,6,3,3,4,5,6,7},cores={{50,31},{42,31},{30,27},{30,27},{33,28},{38,28},{42,31},{46,31}}},
 west={indices={0,1,3,3,4,5,6,7},cores={{69,31},{49,30},{29,58},{29,58},{28,58},{49,30},{66,31},{69,31}}},
 ['south-west']={indices={0,1,3,3,4,5,6,7},cores={{84,31},{71,17},{28,53},{28,53},{28,54},{51,30},{78,27},{83,31}}}}
local times={130,160,110,70,70,70,70,80}
local function color(r,g,b)return Color{r=r,g=g,b=b,a=255}end
local function line(im,x1,y1,x2,y2,c)
 local steps=math.max(math.abs(x2-x1),math.abs(y2-y1));if steps==0 then im:drawPixel(x1,y1,c);return end
 for n=0,steps do im:drawPixel(math.floor(x1+(x2-x1)*n/steps+.5),math.floor(y1+(y2-y1)*n/steps+.5),c)end
end
local function clearDetachedFlecks(body,staff,grip)
 local flat=Image(128,128,ColorMode.RGB);flat:drawImage(staff);flat:drawImage(body);flat:drawImage(grip)
 local seen={}
 for y=0,127 do for x=0,127 do local key=y*128+x
  if not seen[key] and app.pixelColor.rgbaA(flat:getPixel(x,y))>0 then
   local q={{x,y}};local head=1;seen[key]=true
   while head<=#q do local p=q[head];head=head+1
    for dy=-1,1 do for dx=-1,1 do local xx,yy=p[1]+dx,p[2]+dy;local k=yy*128+xx
     if xx>=0 and yy>=0 and xx<128 and yy<128 and not seen[k]and app.pixelColor.rgbaA(flat:getPixel(xx,yy))>0 then seen[k]=true;q[#q+1]={xx,yy}end
    end end
   end
   if #q<=12 then for _,p in ipairs(q)do body:drawPixel(p[1],p[2],0);staff:drawPixel(p[1],p[2],0);grip:drawPixel(p[1],p[2],0)end end
  end
 end end
end
for _,direction in ipairs({'south','south-east','east','north-east','north','north-west','west','south-west'})do
 local config=settings[direction];local folder=root..'raw/actors/warlock_lightning_cast_'..direction:gsub('-','_')..'_direct_v1/job_00/frames/'
 local output=root..'source/actors/enemy_weapon_qa/warlock/lightning_cast/'..direction..'/';app.fs.makeAllDirectories(output..'frames')
 local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='One attached purple crystal and staff'
 local bodyLayer=sprite:newLayer();bodyLayer.name='Actual '..direction..' body - occluding lightning poses rejected'
 local gripLayer=sprite:newLayer();gripLayer.name='Native E grip repair'
 for i,index in ipairs(config.indices)do
  local frame=i==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=times[i]/1000
  local file=direction=='east'and(root..'source/actors/warlock_views_clean/east.png')or(folder..string.format('%03d.png',index+1))
  local source=app.open(file);local body=Image(128,128,ColorMode.RGB);body:drawSprite(source,1);source:close()
  local cx,cy=config.cores[i][1],config.cores[i][2]
  -- Snap approximate crystal coordinates to the actual bright purple pixels before replacing the glow.
  if direction~='east'then local best=nil;local distance=9999
   for y=math.max(0,cy-8),math.min(127,cy+8)do for x=math.max(0,cx-8),math.min(127,cx+8)do local c=Color(body:getPixel(x,y))
    if c.alpha>200 and c.red>125 and c.blue>c.red+15 and c.blue>c.green+25 then local d=(x-cx)^2+(y-cy)^2;if d<distance then distance=d;best={x,y}end end
   end end
   if best then cx,cy=best[1],best[2]end
   config.cores[i]={cx,cy}
  end
  for y=0,127 do for x=0,127 do local c=Color(body:getPixel(x,y))
   local white=c.red>140 and c.green>140 and c.blue>140 and math.max(c.red,c.green,c.blue)-math.min(c.red,c.green,c.blue)<40
   local purple=c.red>90 and c.green>40 and c.blue>c.red+15 and c.blue>c.green+25
   local blue=c.green<110 and c.blue>c.red+35 and c.blue>c.green+35
   if c.alpha>0 and(white or purple or blue)then body:drawPixel(x,y,0)end
  end end
  local staff=Image(128,128,ColorMode.RGB);local grip=Image(128,128,ColorMode.RGB)
  if direction=='east'then
   local tx=math.floor(75+(75-cx)*.65+.5);local ty=math.floor(82+(82-cy)*.65+.5)
   line(staff,tx+1,ty,cx+1,cy+6,color(59,48,33));line(staff,tx,ty,cx,cy+6,color(142,128,82))
   line(grip,67,82,74,81,color(70,61,42));line(grip,68,81,75,80,color(211,196,132))
   for y=80,83 do grip:drawPixel(75,y,color(223,208,144))end
  else
   local nearest=nil;local distance=99999
   for y=math.max(0,cy-16),math.min(127,cy+16)do for x=math.max(0,cx-16),math.min(127,cx+16)do local c=Color(body:getPixel(x,y))
    if c.alpha>200 and c.red>65 and c.red<190 and c.green>50 and c.blue<c.green*.9 and c.red>c.green then local d=(x-cx)^2+(y-cy)^2;if d<distance then distance=d;nearest={x,y}end end
   end end
   if nearest then line(staff,nearest[1]+1,nearest[2],cx+1,cy,color(59,48,33));line(staff,nearest[1],nearest[2],cx,cy,color(142,128,82))end
  end
  local power=(i>=3 and i<=5)and 1 or 0
  for dy=-7,6 do for dx=-4,4 do
   if math.abs(dx)/4+math.abs(dy)/7<=1 then staff:drawPixel(cx+dx,cy+dy,color(151+power*17,100+power*15,205+power*15))end
   if math.abs(dx)/2+math.abs(dy)/5<=1 then staff:drawPixel(cx+dx,cy+dy,color(226,191,246))end
  end end
  staff:drawPixel(cx,cy,color(243,229,254))
  clearDetachedFlecks(body,staff,grip)
  if i==1 then sprite.cels[1].image=staff else sprite:newCel(sprite.layers[1],frame,staff)end
  sprite:newCel(bodyLayer,frame,body);sprite:newCel(gripLayer,frame,grip)
  local flat=Sprite(128,128,ColorMode.RGB);local image=Image(128,128,ColorMode.RGB);image:drawImage(staff);image:drawImage(body);image:drawImage(grip);flat.cels[1].image=image
  flat:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));flat:close()
 end
 sprite:newTag(1,8).name='lightning_cast';sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(output..'animation.gif')
 local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode({indices=config.indices,cores=config.cores,durationsMs=times,releaseFrameIndex=2,originalSource=folder,eastNativeStaff=direction=='east'}));mf:close()
 local contact=Sprite(1024,512,ColorMode.RGB);local image=contact.cels[1].image
 for y=0,511 do for x=0,1023 do image:drawPixel(x,y,color(13,23,37))end end
 for i=1,8 do local flat=Image(128,128,ColorMode.RGB);flat:drawSprite(sprite,i);local ox=((i-1)%4)*256;local oy=math.floor((i-1)/4)*256
  for y=0,127 do for x=0,127 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 contact:saveCopyAs(root..'review/enemy-weapons/warlock_lightning_cast_'..direction..'_clean_2x.png');contact:close();sprite:close()
end
