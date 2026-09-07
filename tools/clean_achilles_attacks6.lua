-- Retain actual directional bodies. Remove one erroneous duplicate blade; settle to own ready pose.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
for _,direction in ipairs({'south','south-east','north','north-west','west','south-west'})do
 local locked=direction=='north-west';local size=locked and 92 or 104
 local folder=root..'raw/actors/achilles_attack_'..direction:gsub('-','_')..(locked and '_locked_v4' or '_v3')..'/job_00/frames/'
 local output=root..'source/actors/achilles_attack_'..direction..'_clean/';app.fs.makeAllDirectories(output..'frames')
 local times={100,70,80,80,80,90,60,70,70,60,60,80};local contactFrame=6
 if direction=='west' or direction=='south-west' then times={70,60,70,70,70,80,80,80,80,80,80,80};contactFrame=7 end
 if locked then times={60,60,60,60,60,60,70,70,100,100,100,100};contactFrame=8 end
 local sprite=Sprite(size,size,ColorMode.RGB);sprite.layers[1].name='Original '..direction..' body and actual sword stroke'
 for i=1,12 do
  local frame=i==1 and sprite.frames[1] or sprite:newEmptyFrame();frame.duration=times[i]/1000
  local input=app.open(folder..string.format('%03d.png',i==12 and 1 or i));local body=Image(size,size,ColorMode.RGB);body:drawSprite(input,1);input:close()
  if direction=='south-east' and i==2 then
   for y=20,49 do for x=64,math.floor(80-(y-20)*.25)do
    body:drawPixel(x,y,0)
   end end
   -- Remove the last detached two-pixel outline fragment from the rejected second blade.
   for y=20,37 do for x=75,81 do body:drawPixel(x,y,0)end end
  end
  if i==1 then sprite.cels[1].image=body else sprite:newCel(sprite.layers[1],frame,body)end
  local flat=Sprite(size,size,ColorMode.RGB);flat.cels[1].image=Image(body);flat:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));flat:close()
 end
 sprite:newTag(1,12).name='attack';sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(root..'review/achilles_attack_'..direction..'_clean.gif')
 local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode({durationsMs=times,contactFrameIndex=contactFrame,size=size,originalSource=folder}));mf:close()
 local contact=Sprite(size*8,size*6,ColorMode.RGB);local image=contact.cels[1].image
 for y=0,image.height-1 do for x=0,image.width-1 do image:drawPixel(x,y,Color{r=13,g=23,b=37,a=255})end end
 for i=1,12 do local flat=Image(size,size,ColorMode.RGB);flat:drawSprite(sprite,i);local ox=((i-1)%4)*size*2;local oy=math.floor((i-1)/4)*size*2
  for y=0,size-1 do for x=0,size-1 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 contact:saveCopyAs(root..'review/qa_achilles_attack_'..direction..'_clean_2x.png');contact:close();sprite:close()
end
