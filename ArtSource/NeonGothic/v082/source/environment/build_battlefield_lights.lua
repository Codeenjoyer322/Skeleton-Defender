-- Native Aseprite extraction of actual emissive pixels; the map stays unchanged.
local project='C:/Users/Life/Documents/ChatGPT/Skeleton Defender/'
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/v082/'
app.fs.makeAllDirectories(root..'source/environment')
app.fs.makeAllDirectories(root..'exports/environment')
app.fs.makeAllDirectories(root..'review')
local input=Sprite{fromFile=project..'Assets/Resources/NeonGothic/cemetery_battlefield.png'}
local original=Image(input.spec)
for _,cel in ipairs(input.cels)do if cel.frame.frameNumber==1 then original:drawImage(cel.image,cel.position)end end
input:close()
local regions={
 {name='entry',x=25,y=21,w=7,h=7,cx=28,cy=24},
 {name='north_lamp',x=349,y=34,w=10,h=12,cx=354,cy=40},
 {name='castle_lamp',x=448,y=66,w=6,h=6,cx=451,cy=69},
 {name='east_edge',x=520,y=124,w=8,h=12,cx=525,cy=130},
 {name='west_edge',x=0,y=147,w=5,h=12,cx=1,cy=153},
 {name='southwest_lamp',x=31,y=223,w=11,h=11,cx=37,cy=229},
 {name='southeast_lamp',x=390,y=270,w=10,h=12,cx=395,cy=276},
 {name='castle_stained_glass',x=500,y=28,w=18,h=38,cx=509,cy=47,pink=true}
}
local sp=Sprite(528,320,ColorMode.RGB);sp.layers[1].name='Original map reference - hidden in emission PNG';sp.cels[1].image=Image(original);sp.layers[1].isVisible=false
local emission=Image(528,320,ColorMode.RGB)
local counts={}
for _,region in ipairs(regions)do
 local mask=Image(528,320,ColorMode.RGB);local count=0
 for y=region.y,region.y+region.h-1 do for x=region.x,region.x+region.w-1 do
  local p=original:getPixel(x,y);local r=app.pixelColor.rgbaR(p);local g=app.pixelColor.rgbaG(p);local b=app.pixelColor.rgbaB(p)
  local chosen=region.pink and r>120 and b>85 and r>g*1.45 and b>g*1.15
   or not region.pink and r>205 and g>105 and r>b+20
  if chosen then
   local c
   if region.pink then c=app.pixelColor.rgba(math.min(255,r+45),math.min(180,g+58),math.min(238,b+70),255)
   else c=app.pixelColor.rgba(255,math.min(252,g+65),math.min(214,b+45),255)end
   mask:drawPixel(x,y,c);emission:drawPixel(x,y,c);count=count+1
  end
 end end
 assert(count>0,'An emission region must contain visible source pixels: '..region.name)
 local layer=sp:newLayer();layer.name=region.name..' - exact luminous shape';sp:newCel(layer,1,mask,Point(0,0))
 counts[#counts+1]={name=region.name,x=region.x,y=region.y,width=region.w,height=region.h,center={x=region.cx,y=region.cy},pixelCount=count}
end
sp:saveAs(root..'source/environment/battlefield_emission.aseprite')
emission:saveAs(root..'exports/environment/battlefield_emission.png')
if not app.params['reviewOnly'] then emission:saveAs(project..'Assets/Resources/NeonGothic/battlefield_emission.png')end
local f=assert(io.open(root..'source/environment/battlefield_lights.json','w'));f:write(json.encode({width=528,height=320,regions=counts}));f:close()
-- Make a native enlarged inspection of the source and extraction, without editing source.
local contact=Image(144,64*4,ColorMode.RGB)
for i,region in ipairs(regions)do
 local row=math.floor((i-1)/2);local col=(i-1)%2
 local sourcePatch=Image(36,64,ColorMode.RGB);sourcePatch:drawImage(original,Point(18-region.cx,24-region.cy))
 local maskPatch=Image(36,64,ColorMode.RGB);maskPatch:drawImage(emission,Point(18-region.cx,24-region.cy))
 contact:drawImage(sourcePatch,Point(col*72,row*64));contact:drawImage(maskPatch,Point(col*72+36,row*64))
end
contact:resize{width=576,height=1024};contact:saveAs(root..'review/battlefield-lights-mask-4x.png')
sp:close()

local lightSprite=Sprite{fromFile=project..'Assets/Resources/NeonGothic/pixel_light.png'}
local light=Image(lightSprite.spec)
for _,c in ipairs(lightSprite.cels)do if c.frame.frameNumber==1 then light:drawImage(c.image,c.position)end end
lightSprite:close()
local function glow(target,cx,cy,rx,ry,r,g,b,opacity)
 local im=Image(rx*2,ry*2,ColorMode.RGB)
 for y=0,ry*2-1 do for x=0,rx*2-1 do
  local sx=math.min(63,math.floor((x+.5)/(rx*2)*64));local sy=math.min(63,math.floor((y+.5)/(ry*2)*64))
  local a=math.floor(app.pixelColor.rgbaA(light:getPixel(sx,sy))*opacity+.5)
  im:drawPixel(x,y,app.pixelColor.rgba(r,g,b,a))
 end end
 target:drawImage(im,Point(cx-rx,cy-ry))
end
local function core(target,region,opacity)
 local im=Image(region.w,region.h,ColorMode.RGB)
 for y=0,region.h-1 do for x=0,region.w-1 do
  local p=emission:getPixel(region.x+x,region.y+y)
  if app.pixelColor.rgbaA(p)>0 then im:drawPixel(x,y,app.pixelColor.rgba(app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p),math.floor(255*opacity+.5)))end
 end end
 target:drawImage(im,Point(region.x,region.y))
end
local preview=Sprite(528,320,ColorMode.RGB);preview.layers[1].name='Preview only - runtime uses game.Elapsed and pauses correctly'
for fi=0,23 do
 local t=fi/10;local frame=fi==0 and preview.frames[1]or preview:newEmptyFrame();frame.duration=.1
 local im=Image(original)
 for i,region in ipairs(regions)do
  if region.pink then
   local p=.84+.12*math.sin(t*1.25+.6)+.04*math.sin(t*2.7+1.4)
   glow(im,region.cx,region.cy,21,34,226,94,175,.38*p);glow(im,region.cx,region.cy,10,22,252,139,206,.19*p);core(im,region,.35+.22*p)
  else
   local phase=(i-1)*1.73;local p=.83+.11*math.sin(t*4.2+phase)+.06*math.sin(t*7.7+phase*.61)
   glow(im,region.cx,region.cy,20,24,255,182,99,.48*p);glow(im,region.cx,region.cy,7,10,255,210,141,.23*p);core(im,region,.36+.20*p)
  end
 end
 if fi==0 then preview.cels[1].image=im;im:saveAs(root..'review/battlefield-lights-preview.png')else preview:newCel(preview.layers[1],fi+1,im,Point(0,0))end
end
preview:saveAs(root..'review/battlefield-lights-preview.aseprite');preview:saveAs(root..'review/battlefield-lights-preview.gif');preview:close()
