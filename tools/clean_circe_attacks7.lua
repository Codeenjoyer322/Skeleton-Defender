-- Native Aseprite cleanup of seven actual authored views; raw outputs remain intact.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=io.open(root..'review/circe-attack-crystal-qc.json','r');local qc=json.decode(f:read('*a'));f:close()
local function col(r,g,b)return Color{r=r,g=g,b=b,a=255}end
local function line(im,x1,y1,x2,y2,c)
 local steps=math.max(math.abs(x2-x1),math.abs(y2-y1));if steps==0 then im:drawPixel(x1,y1,c);return end
 for n=0,steps do im:drawPixel(math.floor(x1+(x2-x1)*n/steps+.5),math.floor(y1+(y2-y1)*n/steps+.5),c)end
end
for _,direction in ipairs({'south','south-east','north-east','north','north-west','west','south-west'})do
 local folder=root..'raw/actors/circe_attack_'..direction:gsub('-','_')..'_v3/job_00/frames/'
 local output=root..'source/actors/circe_staff_attack_'..direction..'_clean/';app.fs.makeAllDirectories(output..'frames')
 local indices=direction=='west' and {1,2,3,3,2,1} or {1,2,3,4,5,6,7,8,1}
 local times=direction=='west' and {240,310,210,160,120,120} or {120,160,270,90,100,150,140,50,80}
 local sprite=Sprite(104,104,ColorMode.RGB);sprite.layers[1].name='Original authored '..direction..' body and staff'
 local crystalLayer=sprite:newLayer();crystalLayer.name='Compact cyan crystal and connected staff end'
 local outputMeta={indices=indices,durationsMs=times,centers={},releaseFrameIndex=direction=='west' and 2 or 3}
 for i,index in ipairs(indices)do
  local frame=i==1 and sprite.frames[1] or sprite:newEmptyFrame();frame.duration=times[i]/1000
  local input=app.open(folder..string.format('%03d.png',index));local body=Image(104,104,ColorMode.RGB);body:drawSprite(input,1);input:close()
  local data=qc[direction][index];local cx,cy=data.center[1],data.center[2];local box=data.bounds
  outputMeta.centers[#outputMeta.centers+1]={cx,cy}
  local crystal=Image(104,104,ColorMode.RGB)
  if (box[3]-box[1])*(box[4]-box[2])>20 then
   local nearest=nil;local distance=99999
   for y=math.max(0,cy-24),math.min(103,cy+24)do for x=math.max(0,cx-24),math.min(103,cx+24)do local c=Color(body:getPixel(x,y))
    if c.alpha>200 and c.red>60 and c.red<190 and c.red>c.green*1.16 and c.green>c.blue*1.1 and c.blue<100 then
     local d=(x-cx)^2+(y-cy)^2;if d<distance then distance=d;nearest={x,y}end
    end
   end end
   for y=math.max(0,box[2]-1),math.min(103,box[4]+1)do for x=math.max(0,box[1]-1),math.min(103,box[3]+1)do local c=Color(body:getPixel(x,y))
    if c.alpha>0 and ((c.green>125 and c.blue>145 and c.blue>c.red+8)or(c.red>215 and c.green>215 and c.blue>215))then body:drawPixel(x,y,0)end
   end end
   if nearest then line(crystal,nearest[1],nearest[2]+1,cx,cy+1,col(57,34,31));line(crystal,nearest[1],nearest[2],cx,cy,col(130,85,54))end
   local horizontal=index>=3 and index<=7
   for dy=-5,5 do for dx=-5,5 do
    local inside=horizontal and(math.abs(dx)*.75+math.abs(dy)<=4)or(math.abs(dx)+math.abs(dy)*.75<=4)
    if inside then crystal:drawPixel(cx+dx,cy+dy,col(88,195,236))end
    if math.abs(dx)+math.abs(dy)<=2 then crystal:drawPixel(cx+dx,cy+dy,col(220,250,253))end
   end end
   crystal:drawPixel(cx,cy,col(248,246,244))
  end
  if i==1 then sprite.cels[1].image=body else sprite:newCel(sprite.layers[1],frame,body)end
  sprite:newCel(crystalLayer,frame,crystal)
  local flat=Sprite(104,104,ColorMode.RGB);local image=Image(104,104,ColorMode.RGB);image:drawImage(body);image:drawImage(crystal);flat.cels[1].image=image
  flat:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));flat:close()
 end
 sprite:newTag(1,#indices).name='staff_attack';sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(root..'review/circe_staff_attack_'..direction..'_clean.gif')
 local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode(outputMeta));mf:close()
 local contact=Sprite(832,math.ceil(#indices/4)*208,ColorMode.RGB);local image=contact.cels[1].image
 for y=0,image.height-1 do for x=0,image.width-1 do image:drawPixel(x,y,col(13,23,37))end end
 for i=1,#indices do local flat=Image(104,104,ColorMode.RGB);flat:drawSprite(sprite,i);local ox=((i-1)%4)*208;local oy=math.floor((i-1)/4)*208
  for y=0,103 do for x=0,103 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 contact:saveCopyAs(root..'review/qa_circe_attack_'..direction..'_clean_2x.png');contact:close();sprite:close()
end
