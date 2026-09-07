local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local input=root..'raw/actors/circe_8views_reference_v1/animations/animating-be844854/south/'
local output=root..'source/actors/circe_south_walk_clean/'
app.fs.makeAllDirectories(output..'frames')
local sprite=Sprite(92,92,ColorMode.RGB);sprite.layers[1].name='Frontal step cycle - replace erroneous back-view terminal frame'
local order={0,1,2,1}
for index,originalIndex in ipairs(order)do
 local frame=index==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=.15
 local source=app.open(input..string.format('frame_%03d.png',originalIndex));local image=Image(92,92,ColorMode.RGB);image:drawSprite(source,1);source:close()
 if index==1 then sprite.cels[1].image=image else sprite:newCel(sprite.layers[1],frame,image)end
 local flat=Sprite(92,92,ColorMode.RGB);flat.cels[1].image=image;flat:saveAs(output..'frames/'..string.format('frame_%03d.png',index-1));flat:close()
end
app.activeSprite=sprite;sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(output..'animation.gif');sprite:close()
