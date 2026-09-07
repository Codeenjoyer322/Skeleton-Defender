local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local out='C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/'
local atlas=app.open(root..'raw/environment/gothic_tower_upgrade_atlas/000.png')
atlas.layers[1].name='PixelLab - nine distinct Gothic tower upgrades'
atlas:saveAs(root..'source/environment/gothic_tower_upgrade_atlas.aseprite')
local img=atlas.cels[1].image
local sockets={}
for row=0,2 do for column=0,2 do
 local pixels,visited,components={},{},{}
 for y=0,127 do for x=0,127 do
  local c=img:getPixel(column*128+x,row*128+y)
  if app.pixelColor.rgbaA(c)>30 then pixels[y*128+x]=c end
 end end
 -- Separate the actual building from stray pixels crossing atlas cell boundaries.
 for y=0,127 do for x=0,127 do
  local key=y*128+x
  if pixels[key] and not visited[key] then
   local comp={keys={},x0=x,x1=x,y0=y,y1=y};local queue={key};visited[key]=true;local at=1
   while at<=#queue do
    local k=queue[at];at=at+1;local xx=k%128;local yy=math.floor(k/128)
    comp.keys[#comp.keys+1]=k;comp.x0=math.min(comp.x0,xx);comp.x1=math.max(comp.x1,xx);comp.y0=math.min(comp.y0,yy);comp.y1=math.max(comp.y1,yy)
    for dy=-1,1 do for dx=-1,1 do
     local nx,ny=xx+dx,yy+dy;local n=ny*128+nx
     if nx>=0 and nx<128 and ny>=0 and ny<128 and pixels[n] and not visited[n] then visited[n]=true;queue[#queue+1]=n end
    end end
   end
   components[#components+1]=comp
  end
 end end
 table.sort(components,function(a,b) return #a.keys>#b.keys end)
 local body=components[1];local cx=(body.x0+body.x1)*.5;local foot=body.y1
 local clean={}
 for i,c in ipairs(components) do
  -- Keep intentional flags/flames and floating ice crystals near their own tower.
  local keep=i==1 or (#c.keys>=8 and c.y1<foot and c.y0>=body.y0-15 and math.abs((c.x0+c.x1)*.5-cx)<48)
  if keep then for _,k in ipairs(c.keys) do clean[k]=pixels[k] end end
 end
 local s=Sprite(96,128,ColorMode.RGB);s.layers[1].name='Registered tower - ground at 48,90'
 local dst=s.cels[1].image
 -- Fixed scale across all upgrades; no distortion or auto fit by individual level.
 local scale=.69
 for y=0,89 do for x=0,95 do
  local sx=math.floor(cx+(x-48)/scale+.5);local sy=math.floor(foot+(y-89)/scale+.5)
  if sx>=0 and sy>=0 and sx<128 and sy<128 and clean[sy*128+sx] then dst:drawPixel(x,y,clean[sy*128+sx]) end
 end end
 -- Author the projectile origin in native pixels: firing parapet, fire bowl, ice tip.
 local socketX,socketY=48,89-(foot-body.y0)*scale*.68
 if column>0 then
  local sumX,sumY,n=0,0,0;local top=128
  for y=0,89 do for x=0,95 do
   local c=dst:getPixel(x,y);local r,g,b=app.pixelColor.rgbaR(c),app.pixelColor.rgbaG(c),app.pixelColor.rgbaB(c)
   if app.pixelColor.rgbaA(c)>30 and y<socketY and (column==1 and r>180 and g>95 and r>b*1.5 or column==2 and g>175 and b>180 and r<g*1.08) then
    if y<top then top=y end
   end
  end end
  for y=math.max(0,top),math.min(89,top+7) do for x=0,95 do
   local c=dst:getPixel(x,y);local r,g,b=app.pixelColor.rgbaR(c),app.pixelColor.rgbaG(c),app.pixelColor.rgbaB(c)
   if app.pixelColor.rgbaA(c)>30 and (column==1 and r>180 and g>95 and r>b*1.5 or column==2 and g>175 and b>180 and r<g*1.08) then sumX=sumX+x;sumY=sumY+y;n=n+1 end
  end end
  if n>0 then socketX=math.floor(sumX/n+.5);socketY=math.floor(sumY/n+.5) end
 end
 socketY=math.floor(socketY+.5)
 if app.pixelColor.rgbaA(dst:getPixel(socketX,socketY))<128 then
  local best=10000;local bx,by=socketX,socketY
  for y=math.max(0,socketY-8),math.min(89,socketY+8) do for x=math.max(0,socketX-8),math.min(95,socketX+8) do
   local c=dst:getPixel(x,y);local r,g,b=app.pixelColor.rgbaR(c),app.pixelColor.rgbaG(c),app.pixelColor.rgbaB(c)
   local qualified=column==0 or column==1 and r>180 and g>95 and r>b*1.5 or column==2 and g>175 and b>180 and r<g*1.08
   local d=(x-socketX)^2+(y-socketY)^2
   if qualified and app.pixelColor.rgbaA(c)>127 and d<best then best=d;bx=x;by=y end
  end end
  socketX=bx;socketY=by
 end
 local marker=s:newLayer();marker.name='Projectile socket - enable to edit';marker.isVisible=false
 local point=Image(96,128,ColorMode.RGB);point:drawPixel(socketX,socketY,Color{r=255,g=0,b=255,a=255});s:newCel(marker,s.frames[1],point,Point(0,0))
 sockets[#sockets+1]=string.format('{"kind":%d,"level":%d,"pixelX":%d,"pixelY":%d,"offsetX":%.3f,"offsetY":%.3f}',column,row+1,socketX,socketY,socketX*.75-36,socketY*.75-67)
 local name='tower_'..column..'_'..(row+1)
 s:saveAs(root..'source/environment/'..name..'.aseprite')
 s:saveCopyAs(root..'exports/environment/'..name..'.png')
 s:saveCopyAs(out..name..'.png');s:close()
end end
atlas:close()
local json='{"schemaVersion":1,"canvasWidth":96,"canvasHeight":128,"drawScale":0.75,"sockets":['..table.concat(sockets,',')..']}\n'
for _,path in ipairs({out..'tower-sockets.json',root..'exports/environment/tower-sockets.json'}) do local f=assert(io.open(path,'w'));f:write(json);f:close() end
print('Nine tower upgrades cleaned, grounded and annotated in native Aseprite.')
