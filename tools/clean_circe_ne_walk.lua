-- Restore only the far-hand staff. Keep the actual north-east body and walk motion.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local input=root..'raw/actors/circe_8views_reference_v1/animations/animating-be844854/north-east/'
local output=root..'source/actors/circe_ne_walk_clean/'
app.fs.makeAllDirectories(output..'frames')
local sprite=Sprite(92,92,ColorMode.RGB);sprite.layers[1].name='Far-hand upright staff behind body'
local bodyLayer=sprite:newLayer();bodyLayer.name='Actual NE walk - stray near-hand club removed'
for index=0,3 do
 local frame=index==0 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=.15
 local original=app.open(input..string.format('frame_%03d.png',index));local body=Image(92,92,ColorMode.RGB);body:drawSprite(original,1);original:close()
 if index>=2 then
  local before=Image(body)
  for y=60,88 do for x=56,76 do
   local c=Color(before:getPixel(x,y))
   -- The blue-violet hem overlaps the unwanted club's rectangle; retain it.
   local cloth=c.alpha>0 and c.blue>c.red+15 and c.blue>c.green+25
   local border=false
   if c.alpha>0 and math.max(c.red,c.green,c.blue)<70 then
    for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}})do local n=Color(before:getPixel(x+d[1],y+d[2]));if n.alpha>0 and n.blue>n.red+15 and n.blue>n.green+25 then border=true end end
   end
   if not cloth and not border then body:drawPixel(x,y,0)end
  end end
 end
 local staff=Image(92,92,ColorMode.RGB)
 for y=35,77 do local x=37+math.floor((y-35)*5/42);staff:drawPixel(x+1,y,Color{r=35,g=21,b=21,a=255});staff:drawPixel(x,y,Color{r=106,g=59,b=49,a=255})end
 for y=-5,4 do for x=-4,4 do if x*x/16+y*y/25<=1 then staff:drawPixel(37+x,32+y,Color{r=88,g=195,b=236,a=255})end end end
 for y=-3,2 do for x=-2,2 do if x*x/4+y*y/9<=1 then staff:drawPixel(37+x,32+y,Color{r=224,g=249,b=253,a=255})end end end
 if index==0 then sprite.cels[1].image=staff else sprite:newCel(sprite.layers[1],frame,staff)end
 sprite:newCel(bodyLayer,frame,body)
 local flat=Sprite(92,92,ColorMode.RGB);local image=Image(92,92,ColorMode.RGB);image:drawImage(staff);image:drawImage(body);flat.cels[1].image=image
 flat:saveAs(output..'frames/'..string.format('frame_%03d.png',index));flat:close()
end
app.activeSprite=sprite;sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(output..'animation.gif');sprite:close()
