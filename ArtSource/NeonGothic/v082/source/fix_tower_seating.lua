-- Native Aseprite repair: open Archer II platform, common foundation centre.
-- Existing buildings and elves stay at their native resolution. No generated art.
local ROOT='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local OLD=ROOT..'v081/';local OUT=ROOT..'v082/'
for _,p in ipairs({'source/towers','exports/environment','review/towers'})do app.fs.makeAllDirectories(OUT..p)end
local function read(path)local f=assert(io.open(path,'r'));local v=json.decode(f:read('*a'));f:close();return v end
local function write(path,value)local f=assert(io.open(path,'w'));f:write(json.encode(value));f:close()end
local function load(path)
 local sp=Sprite{fromFile=path};local im=Image(sp.spec)
 for _,c in ipairs(sp.cels)do if c.frame.frameNumber==1 then im:drawImage(c.image,c.position)end end
 sp:close();return im
end
local rgba=app.pixelColor.rgba
local cyan=rgba(123,229,242,255);local gold=rgba(227,187,94,255)
local function line(im,x0,y0,x1,y1,color)
 local steps=math.max(math.abs(x1-x0),math.abs(y1-y0));if steps==0 then im:drawPixel(x0,y0,color);return end
 for i=0,steps do im:drawPixel(math.floor(x0+(x1-x0)*i/steps+.5),math.floor(y0+(y1-y0)*i/steps+.5),color)end
end
local function footprint(im,cx,cy,color)
 line(im,cx,cy-20,cx+40,cy,color);line(im,cx+40,cy,cx,cy+20,color)
 line(im,cx,cy+20,cx-40,cy,color);line(im,cx-40,cy,cx,cy-20,color)
 line(im,cx-3,cy,cx+3,cy,color);line(im,cx,cy-3,cx,cy+3,color)
end
local catalog=read(OLD..'exports/environment/tower-sockets.json')
catalog.coordinateSystem='native top-left pixels; groundPivot is foundation footprint centre; archer anchors are feet on the open platform'
catalog.revision='0.8.2'
local elf=load(OLD..'source/tower_elf/idle/south/frames/frame_000.png')
local openPlatform=load(OLD..'exports/environment/tower_0_1.png')
local family=Image(180*3,232*3,ColorMode.RGB)
local guideFamily=Image(180*3,232*3,ColorMode.RGB)
local verification={towers={},changedArt={'tower_0_2'},groundPivot={72,161},archerCounts={1,1,2}}
for _,t in ipairs(catalog.towers)do
 local name=string.format('tower_%d_%d',t.kind,t.level)
 local original=load(OLD..'exports/environment/'..name..'.png')
 local sp=Sprite(144,192,ColorMode.RGB);sp.layers[1].name='Original accepted masonry';sp.cels[1].image=Image(original)
 local architecture=Image(original)
 if t.kind==0 and t.level==2 then
  -- Remove canopy AND its supporting posts, replace only the upper platform.
  -- The matching native open platform from tier I shares the same masonry seam.
  for y=0,79 do for x=0,143 do architecture:drawPixel(x,y,openPlatform:getPixel(x,y))end end
  -- Small brass coping / studs distinguishes the second tier without a roof.
  for _,p in ipairs({{48,70},{49,71},{50,71},{51,72},{52,72},{91,70},{90,71},{89,71},{88,72},{87,72}})do
   if app.pixelColor.rgbaA(architecture:getPixel(p[1],p[2]))>0 then architecture:drawPixel(p[1],p[2],gold)end
  end
  sp.layers[1].isVisible=false
  local repaired=sp:newLayer();repaired.name='Archer II - open deck, low parapet, canopy and posts removed';sp:newCel(repaired,1,architecture)
 end
 t.groundPivotX=72;t.groundPivotY=161
 t.footprintX=32;t.footprintY=141;t.footprintWidth=80;t.footprintHeight=40
 -- All actor anchors and socket positions remain in this same native canvas.
 local x0,y0,x1,y1=144,192,-1,-1
 for y=0,191 do for x=0,143 do if app.pixelColor.rgbaA(architecture:getPixel(x,y))>0 then
  x0=math.min(x0,x);x1=math.max(x1,x);y0=math.min(y0,y);y1=math.max(y1,y)
 end end end
 t.opaqueX=x0;t.opaqueY=y0;t.opaqueWidth=x1-x0+1;t.opaqueHeight=y1-y0+1
 architecture:saveAs(OUT..'exports/environment/'..name..'.png')
 local preview=Image(architecture)
 for _,a in ipairs(t.archers)do
  local im=Image(elf);im:resize{width=math.floor(elf.width*a.scale+.5),height=math.floor(elf.height*a.scale+.5)}
  local x=math.floor(a.pixelX-22*a.scale+.5);local y=math.floor(a.pixelY-52*a.scale+.5)
  local layer=sp:newLayer();layer.name='Preview elf '..a.index..' - separately animated at runtime';sp:newCel(layer,1,im,Point(x,y));layer.isVisible=false
  preview:drawImage(im,Point(x,y))
 end
 local guides=Image(144,192,ColorMode.RGB);footprint(guides,72,161,cyan)
 for _,s in ipairs(t.sockets)do local x,y=math.floor(s.pixelX+.5),math.floor(s.pixelY+.5);line(guides,x-2,y,x+2,y,gold);line(guides,x,y-2,x,y+2,gold)end
 local guideLayer=sp:newLayer();guideLayer.name='Registration guides - centre72,161, footprint and emitters';sp:newCel(guideLayer,1,guides);guideLayer.isVisible=false
 sp:saveAs(OUT..'source/towers/'..name..'.aseprite');sp:close()
 preview:saveAs(OUT..'review/towers/'..name..'-preview.png')
 local dx,dy=(t.level-1)*180+18,t.kind*232+16
 family:drawImage(preview,Point(dx,dy));guideFamily:drawImage(preview,Point(dx,dy));guideFamily:drawImage(guides,Point(dx,dy))
 table.insert(verification.towers,{kind=t.kind,level=t.level,groundPivot={72,161},footprint={32,141,80,40},opaque={x0,y0,x1-x0+1,y1-y0+1},original='v081/exports/environment/'..name..'.png',native='v082/source/towers/'..name..'.aseprite'})
end
family:saveAs(OUT..'review/towers/tower-family-seated.png');family:resize{width=1080,height=1392};family:saveAs(OUT..'review/towers/tower-family-seated-2x.png')
guideFamily:resize{width=1080,height=1392};guideFamily:saveAs(OUT..'review/towers/tower-foundation-and-sockets-2x.png')
write(OUT..'exports/environment/tower-sockets.json',catalog)
write(OUT..'source/towers/registration.json',catalog)
write(OUT..'review/tower-registration-native.json',verification)
