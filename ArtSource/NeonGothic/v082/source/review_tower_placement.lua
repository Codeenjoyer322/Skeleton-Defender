local ROOT='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local function load(path)local sp=Sprite{fromFile=path};local im=Image(sp.spec);for _,c in ipairs(sp.cels)do if c.frame.frameNumber==1 then im:drawImage(c.image,c.position)end end;sp:close();return im end
local function pixel(im,x,y,c)if x>=0 and y>=0 and x<im.width and y<im.height then im:drawPixel(x,y,c)end end
local function line(im,x0,y0,x1,y1,c)local s=math.max(math.abs(x1-x0),math.abs(y1-y0));for i=0,s do pixel(im,math.floor(x0+(x1-x0)*i/s+.5),math.floor(y0+(y1-y0)*i/s+.5),c)end end
local map=load(ROOT..'v081/exports/environment/cemetery_battlefield.png');map:resize{width=1056,height=640}
local gold=app.pixelColor.rgba(230,195,94,255)
local output=Image(600,330,ColorMode.RGB)
for panel=0,1 do
 local scene=Image(map);local old=panel==0;local version=old and 'v081' or 'v082';local pivot=old and 182 or 161
 -- Same exact world/site position on both panels; only registered tower differs.
 local x,y=469,270
 if old then
  line(scene,x-54,y-16,x+54,y-16,gold);line(scene,x+54,y-16,x+54,y+8,gold)
  line(scene,x+54,y+8,x-54,y+8,gold);line(scene,x-54,y+8,x-54,y-16,gold)
 else
  line(scene,x,y-22,x+43,y,gold);line(scene,x+43,y,x,y+22,gold)
  line(scene,x,y+22,x-43,y,gold);line(scene,x-43,y,x,y-22,gold)
 end
 scene:drawImage(load(ROOT..version..'/review/towers/tower_0_2-preview.png'),Point(x-72,y-pivot))
 for yy=0,329 do for xx=0,299 do pixel(output,panel*300+xx,yy,scene:getPixel(x-150+xx,y-200+yy))end end
end
output:saveAs(ROOT..'v082/review/towers/archer-II-placement-before-after.png');output:resize{width=1200,height=660};output:saveAs(ROOT..'v082/review/towers/archer-II-placement-before-after-2x.png')
