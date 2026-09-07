local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local raw=root..'raw/actors/achilles_8views_reference_v2/'
local clean=root..'source/actors/achilles_walk8_clean/'
local dirs={'south','south-east','east','north-east','north','north-west','west','south-west'}
for part=0,1 do
 local sprite=Sprite(920,736,ColorMode.RGB);local image=sprite.cels[1].image
 for y=0,735 do for x=0,919 do image:drawPixel(x,y,Color{r=13,g=23,b=37,a=255})end end
 for row=0,3 do local dir=dirs[part*4+row+1]
  for col=0,4 do
   local file=col==0 and(raw..'rotations/'..dir..'.png')or(clean..dir..'/frames/'..string.format('frame_%03d.png',col-1))
   local source=app.open(file);local frame=Image(92,92,ColorMode.RGB);frame:drawSprite(source,1);source:close()
   for y=0,91 do for x=0,91 do local c=Color(frame:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(col*184+x*2+dx,row*184+y*2+dy,c)end end end end end
  end
 end
 sprite:saveCopyAs(root..'review/qa_achilles_walk8_clean_part'..(part+1)..'.png');sprite:close()
end
