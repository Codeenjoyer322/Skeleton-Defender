-- Restore the lost crystal/staff in the actual independently authored SW walk.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local input=root..'raw/actors/circe_8views_reference_v1/animations/animating/south-west/'
local output=root..'source/actors/circe_sw_walk_clean/';app.fs.makeAllDirectories(output..'frames')
local sprite=Sprite(92,92,ColorMode.RGB);sprite.layers[1].name='Original independent SW walk'
local staffLayer=sprite:newLayer();staffLayer.name='Restored near-hand staff, matching own SW reference'
for index=0,3 do
 local frame=index==0 and sprite.frames[1] or sprite:newEmptyFrame();frame.duration=.15
 local source=app.open(input..string.format('frame_%03d.png',index));local body=Image(92,92,ColorMode.RGB);body:drawSprite(source,1);source:close()
 local cx=54;local cy=(index==1 or index==3) and 33 or 32
 local staff=Image(92,92,ColorMode.RGB)
 for y=cy+3,78 do local x=cx-math.floor((y-cy-3)*14/(75-cy));staff:drawPixel(x+1,y,Color{r=35,g=21,b=21,a=255});staff:drawPixel(x,y,Color{r=106,g=59,b=49,a=255})end
 for y=-5,4 do for x=-4,4 do if x*x/16+y*y/25<=1 then staff:drawPixel(cx+x,cy+y,Color{r=88,g=195,b=236,a=255})end end end
 for y=-3,2 do for x=-2,2 do if x*x/4+y*y/9<=1 then staff:drawPixel(cx+x,cy+y,Color{r=224,g=249,b=253,a=255})end end end
 if index==0 then sprite.cels[1].image=body else sprite:newCel(sprite.layers[1],frame,body)end
 sprite:newCel(staffLayer,frame,staff)
 local flat=Sprite(92,92,ColorMode.RGB);local image=Image(92,92,ColorMode.RGB);image:drawImage(body);image:drawImage(staff);flat.cels[1].image=image
 flat:saveAs(output..'frames/'..string.format('frame_%03d.png',index));flat:close()
end
sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(output..'animation.gif');sprite:close()
