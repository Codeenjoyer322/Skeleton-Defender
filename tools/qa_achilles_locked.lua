local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
for _,direction in ipairs({'north-east','north-west'})do
 local folder=root..'raw/actors/achilles_attack_'..direction:gsub('-','_')..'_locked_v4/job_00/frames/'
 local sprite=Sprite(736,552,ColorMode.RGB);local image=sprite.cels[1].image
 for y=0,551 do for x=0,735 do image:drawPixel(x,y,Color{r=13,g=23,b=37,a=255})end end
 for i=1,12 do local source=app.open(folder..string.format('%03d.png',i));local frame=Image(92,92,ColorMode.RGB);frame:drawSprite(source,1);source:close()
  local ox=((i-1)%4)*184;local oy=math.floor((i-1)/4)*184
  for y=0,91 do for x=0,91 do local c=Color(frame:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
 end
 sprite:saveCopyAs(root..'review/qa_achilles_attack_'..direction..'_locked_2x.png');sprite:close()
end
