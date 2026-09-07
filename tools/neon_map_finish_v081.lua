-- Native Aseprite scene assembly. Geometry overlays are exported for review only.
local v='C:/Users/Life/Desktop/Testnew2/NeonGothic/v081/'
local sp=app.open(v..'raw/jobs/cemetery_grass_map_v1/job_00/frames/000.png')
sp.layers[1].name='PixelLab cemetery architecture and cobblestones'
local ps=app.open(v..'raw/jobs/cemetery_grass_masked_inpaint_v1/job_00/frames/000.png')
local ms=app.open(v..'source/environment/grass_inpaint_mask.png')
local patch=ps.cels[1].image;local mask=ms.cels[1].image
local layer=sp:newLayer();layer.name='Native masked autumn grass - architecture protected'
local out=sp:newCel(layer,1,Image(sp.width,sp.height,ColorMode.RGB),Point(0,0)).image
for y=0,mask.height-1 do for x=0,mask.width-1 do
 if app.pixelColor.rgbaR(mask:getPixel(x,y))>127 then out:drawPixel(x+8,y,patch:getPixel(x,y)) end
end end
local clean=app.open(v..'raw/jobs/foundation_cleanup_masked_inpaint_v1/job_00/frames/000.png')
local cleanMask=app.open(v..'source/environment/foundation_cleanup_mask.png')
local cleanLayer=sp:newLayer();cleanLayer.name='Grass beneath dynamic foundations - no baked duplicate pads'
local cleanOut=sp:newCel(cleanLayer,1,Image(sp.width,sp.height,ColorMode.RGB),Point(0,0)).image
for y=0,319 do for x=0,511 do
 if app.pixelColor.rgbaR(cleanMask.cels[1].image:getPixel(x,y))>127 then cleanOut:drawPixel(x,y,clean.cels[1].image:getPixel(x,y)) end
end end
clean:close();cleanMask:close()
local gate=app.open(v..'raw/jobs/entrance_gate_masked_inpaint_v1/job_00/frames/000.png')
local gateMask=app.open(v..'source/environment/entrance_gate_mask.png')
local gateLayer=sp:newLayer();gateLayer.name='Complete entrance arch - fitted to its measured threshold'
local gateOut=sp:newCel(gateLayer,1,Image(sp.width,sp.height,ColorMode.RGB),Point(0,0)).image
for y=0,79 do for x=0,63 do
 if app.pixelColor.rgbaR(gateMask.cels[1].image:getPixel(x,y))>127 then gateOut:drawPixel(x,y,gate.cels[1].image:getPixel(x,y)) end
end end
gate:close();gateMask:close()
sp:saveAs(v..'source/environment/cemetery_battlefield.aseprite')
sp:saveCopyAs(v..'exports/environment/cemetery_battlefield.png')
sp:saveCopyAs('C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/cemetery_battlefield.png')
local f=io.open('C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/battlefield-layout.json','r')
local data=json.decode(f:read('*a'));f:close()
local guide=sp:newLayer();guide.name='REVIEW ONLY - actual Unity route and foundations'
local im=sp:newCel(guide,1,Image(sp.width,sp.height,ColorMode.RGB),Point(0,0)).image
local function dot(x,y,r,c)
 for yy=math.floor(y-r),math.floor(y+r) do for xx=math.floor(x-r),math.floor(x+r) do
  if xx>=0 and yy>=0 and xx<sp.width and yy<sp.height and (xx-x)^2+(yy-y)^2<=r*r then im:drawPixel(xx,yy,c) end
 end end
end
for _,p in ipairs(data.path) do dot(p.x/2,p.y/2,1,Color{r=255,g=180,b=80,a=255}) end
for _,p in ipairs(data.sites) do dot(p.x/2,p.y/2,3,Color{r=255,g=50,b=160,a=255}) end
for _,p in ipairs({data.path[1],data.path[#data.path]}) do dot(p.x/2,p.y/2,3,Color{r=50,g=255,b=255,a=255}) end
sp:resize(1056,640)
sp:saveCopyAs(v..'review/battlefield-alignment-2x.png')
sp:close();ps:close();ms:close()
print('Native map assembly and alignment review saved.')
