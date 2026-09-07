local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local raw=root..'raw/actors/circe_8views_reference_v1/'
local sprite=Sprite(920,368,ColorMode.RGB);local image=sprite.cels[1].image
for y=0,367 do for x=0,919 do image:drawPixel(x,y,Color{r=13,g=23,b=37,a=255})end end
for row,direction in ipairs({'west','south-west'})do
 for col=0,4 do
  local action=direction=='west' and 'animating-e2892536' or 'animating'
  local file=col==0 and(raw..'rotations/'..direction..'.png')or(raw..'animations/'..action..'/'..direction..'/'..string.format('frame_%03d.png',col-1))
  if col>0 and direction=='south-west' then file=root..'source/actors/circe_sw_walk_clean/frames/'..string.format('frame_%03d.png',col-1)end
  local source=app.open(file);local frame=Image(92,92,ColorMode.RGB);frame:drawSprite(source,1);source:close()
  for y=0,91 do for x=0,91 do local c=Color(frame:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(col*184+x*2+dx,(row-1)*184+y*2+dy,c)end end end end end
 end
end
sprite:saveCopyAs(root..'review/qa_circe_walk_west_2x.png');sprite:close()
