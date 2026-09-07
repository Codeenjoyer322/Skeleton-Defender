-- Editable Aseprite light mask and an animated review of the menu illumination.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local dest='C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/'
local glow=Sprite(64,64,ColorMode.RGB)
glow.layers[1].name='Stepped pixel light falloff'
for y=0,63 do for x=0,63 do
 local r=math.sqrt(((x-31.5)/31.5)^2+((y-31.5)/31.5)^2)
 if r<1 then
  local a=math.floor((1-r)^2*255/8)*8
  glow.cels[1].image:drawPixel(x,y,Color{r=255,g=255,b=255,a=a})
 end
end end
glow:saveAs(root..'source/environment/pixel_light.aseprite')
glow:saveCopyAs(root..'exports/environment/pixel_light.png')
glow:saveCopyAs(dest..'pixel_light.png');glow:close()
local ring=Sprite(256,256,ColorMode.RGB);ring.layers[1].name='Thin range guide - tint supplied by tower'
for y=0,255 do for x=0,255 do
 local d=math.sqrt((x-127.5)^2+(y-127.5)^2)
 if d<125 then ring.cels[1].image:drawPixel(x,y,Color{r=255,g=255,b=255,a=d>123.75 and 150 or 7}) end
end end
ring:saveAs(root..'source/environment/range_ring.aseprite')
ring:saveCopyAs(root..'exports/environment/range_ring.png')
ring:saveCopyAs(dest..'range_ring.png');ring:close()
local sp=app.open(root..'source/environment/moonlit_courtyard_menu.aseprite')
local layer=sp:newLayer();layer.name='Animated cyan, magenta and lantern light'
local lamps={{241,92,25,60,125,245,255,.20},{298,66,25,60,125,245,255,.16},{357,40,23,60,125,245,255,.20},{414,84,25,52,255,132,225,.18},{481,84,23,48,255,132,225,.18},{241,233,25,49,255,132,225,.20},{286,235,37,46,255,208,132,.36},{433,220,36,48,255,208,132,.36},{505,262,32,44,255,208,132,.38},{425,334,52,30,99,255,241,.32}}
for frame=1,16 do
 if frame>1 then sp:newFrame() end
 sp.frames[frame].duration=.2
 local img=Image(sp.width,sp.height,ColorMode.RGB)
 local t=(frame-1)*.2
 for i,p in ipairs(lamps) do
  local pulse=.74+.19*math.sin(t*math.pi/1.6+i*.7)+.06*math.sin(t*math.pi*2.5+i)
  for y=math.max(0,p[2]-p[4]),math.min(sp.height-1,p[2]+p[4]) do
   for x=math.max(0,p[1]-p[3]),math.min(sp.width-1,p[1]+p[3]) do
    local r=math.sqrt(((x-p[1])/p[3])^2+((y-p[2])/p[4])^2)
    if r<1 then local a=math.floor((1-r)^2*255/8)*8*p[8]*pulse
     if a>0 then img:drawPixel(x,y,Color{r=p[5],g=p[6],b=p[7],a=math.floor(a)}) end
    end
   end
  end
 end
 sp:newCel(layer,sp.frames[frame],img,Point(0,0))
end
local tag=sp:newTag(1,16);tag.name='Lanterns and runes - 3.2 second loop'
sp:saveAs(root..'source/environment/moonlit_courtyard_animated.aseprite')
sp:saveCopyAs(root..'review/moonlit_courtyard_animated.gif');sp:close()
print('Editable menu illumination and native GIF preview saved.')
