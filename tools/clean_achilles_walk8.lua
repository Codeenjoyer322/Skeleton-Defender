-- Preserve the eight generated views; fix only inconsistent helmet frames and dark greaves.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local raw=root..'raw/actors/achilles_8views_reference_v2/'
local output=root..'source/actors/achilles_walk8_clean/'
local dirs={'south','south-east','east','north-east','north','north-west','west','south-west'}
local function read(file)local source=app.open(file);local im=Image(92,92,ColorMode.RGB);im:drawSprite(source,1);source:close();return im end
local function line(im,x1,y1,x2,y2,c,w)
 local steps=math.max(math.abs(x2-x1),math.abs(y2-y1));w=w or 1
 for i=0,steps do local x=math.floor(x1+(x2-x1)*i/steps+.5);local y=math.floor(y1+(y2-y1)*i/steps+.5)
  for yy=0,w-1 do for xx=0,w-1 do im:drawPixel(x+xx-math.floor(w/2),y+yy-math.floor(w/2),c)end end
 end
end
for _,direction in ipairs(dirs) do
 app.fs.makeAllDirectories(output..direction..'/frames')
 local sp=Sprite(92,92,ColorMode.RGB);sp.layers[1].name='Native directional walk - original pixels'
 local patches=sp:newLayer();patches.name='Helmet, held sword and gold far greave consistency'
 local head=read(raw..'rotations/'..direction..'.png')
 for index=0,3 do
  local frame=index==0 and sp.frames[1]or sp:newEmptyFrame();frame.duration=.15
  local original=read(raw..'animations/animating/'..direction..'/'..string.format('frame_%03d.png',index))
  local patch=Image(92,92,ColorMode.RGB)
  if (direction=='south'and index>=2)or(direction=='north'and index==1)then
   for y=0,38 do for x=0,91 do original:drawPixel(x,y,0)end end
   for y=0,37 do for x=0,91 do local c=head:getPixel(x,y);if Color(c).alpha>0 then patch:drawPixel(x,y+1,c)end end end
  end
  if direction=='east'and(index==1 or index==3)then
   local box=index==1 and{51,66,64,79}or{32,67,49,79}
   for y=box[2],box[4]do for x=box[1],box[3]do local c=Color(original:getPixel(x,y))
    if c.alpha>200 and math.max(c.red,c.green,c.blue)<90 then
     local interior=true;for _,d in ipairs({{-1,0},{1,0},{0,-1},{0,1}})do if Color(original:getPixel(x+d[1],y+d[2])).alpha<200 then interior=false end end
     if interior then
      local glint=(x+y)%4==0
      patch:drawPixel(x,y,glint and Color{r=231,g=182,b=86,a=255}or Color{r=174,g=123,b=49,a=255})
     end
    end
   end end
  end
  if direction=='south'and index~=1 then
   local hx=index==3 and 35 or 36;local hy=index==0 and 60 or 61
   line(patch,hx,hy,hx+15,hy+11,Color{r=15,g=13,b=15,a=255},4)
   line(patch,hx+3,hy+3,hx+14,hy+10,Color{r=171,g=169,b=172,a=255},2)
   line(patch,hx+3,hy+2,hx+13,hy+9,Color{r=246,g=243,b=240,a=255},1)
   line(patch,hx,hy,hx+2,hy+2,Color{r=106,g=65,b=21,a=255},2)
   line(patch,hx+1,hy+4,hx+4,hy+1,Color{r=183,g=139,b=49,a=255},1)
  end
  if index==0 then sp.cels[1].image=original else sp:newCel(sp.layers[1],frame,original)end
  sp:newCel(patches,frame,patch)
  local flat=Sprite(92,92,ColorMode.RGB);local image=Image(92,92,ColorMode.RGB);image:drawImage(original);image:drawImage(patch);flat.cels[1].image=image
  flat:saveAs(output..direction..'/frames/'..string.format('frame_%03d.png',index));flat:close()
 end
 app.activeSprite=sp;sp:saveAs(output..direction..'/animation.aseprite');sp:saveCopyAs(output..direction..'/animation.gif');sp:close()
end
