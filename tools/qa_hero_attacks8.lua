local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local dirs={'south','south-east','north-east','north','north-west','west','south-west'}
for _,role in ipairs({'achilles','circe'})do
 local count=role=='achilles' and 12 or 8
 for _,direction in ipairs(dirs)do
  local folder=root..'raw/actors/'..role..'_attack_'..direction:gsub('-','_')..'_v3/job_00/frames/'
  local contact=Sprite(832,math.ceil(count/4)*208,ColorMode.RGB);local image=contact.cels[1].image
  for y=0,image.height-1 do for x=0,image.width-1 do image:drawPixel(x,y,Color{r=13,g=23,b=37,a=255})end end
  for index=1,count do
   local source=app.open(folder..string.format('%03d.png',index))
   local flat=Image(source.width,source.height,ColorMode.RGB);flat:drawSprite(source,1);source:close()
   local ox=((index-1)%4)*208;local oy=math.floor((index-1)/4)*208
   for y=0,flat.height-1 do for x=0,flat.width-1 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
  end
  contact:saveCopyAs(root..'review/qa_'..role..'_attack_'..direction..'_2x.png');contact:close()
 end
end
