-- Review only: true generated frames, no reflections or speculative approval.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=io.open(root..'source/enemy-weapon-qc-inputs.json','r');local clips=json.decode(f:read('*a'));f:close()
for _,clip in ipairs(clips)do
 local name=clip.role..'_'..clip.action..'_'..clip.direction
 local output=root..'review/enemy-weapons/';app.fs.makeAllDirectories(output)
 if not app.fs.isFile(output..name..'_2x.png')then
  local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='Actual authored '..clip.direction..' raw frames'
  local contact=Sprite(1024,512,ColorMode.RGB);local image=contact.cels[1].image
  for y=0,511 do for x=0,1023 do image:drawPixel(x,y,Color{r=13,g=23,b=37,a=255})end end
  for i,path in ipairs(clip.files)do
   local source=app.open(path);local flat=Image(128,128,ColorMode.RGB);flat:drawSprite(source,1);source:close()
   local frame=i==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=.1
   if i==1 then sprite.cels[1].image=flat else sprite:newCel(sprite.layers[1],frame,flat)end
   local ox=((i-1)%4)*256;local oy=math.floor((i-1)/4)*256
   for y=0,127 do for x=0,127 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
  end
  sprite:saveAs(output..name..'.aseprite');sprite:saveCopyAs(output..name..'.gif');sprite:close()
  contact:saveCopyAs(output..name..'_2x.png');contact:close()
 end
end
