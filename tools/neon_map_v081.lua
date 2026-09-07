-- Native Aseprite layout. Raster artwork is authored here, geometry shared with Unity.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/v081/'
app.fs.makeAllDirectories(root..'source/environment')
app.fs.makeAllDirectories(root..'exports/environment')
local f=io.open('C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/battlefield-layout.json','r')
local layout=json.decode(f:read('*a')); f:close()
local function C(h,a) return Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=a or 255} end
local sp=Sprite(528,320,ColorMode.RGB)
sp.layers[1].name='Muted autumn cemetery grass - composition guide'
local im=sp.cels[1].image
local function px(x,y,c) if x>=0 and y>=0 and x<528 and y<320 then im:drawPixel(math.floor(x),math.floor(y),C(c)) end end
local function rect(x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do px(xx,yy,c) end end end
local function line(a,b,w,c)
 local n=math.max(1,math.ceil(math.max(math.abs(b[1]-a[1]),math.abs(b[2]-a[2]))))
 for i=0,n do local t=i/n;rect(math.floor(a[1]+(b[1]-a[1])*t-w/2),math.floor(a[2]+(b[2]-a[2])*t-w/2),w,w,c) end
end
local function diamond(x,y,w,h,c)
 for yy=-h,h do local a=math.floor(w*(1-math.abs(yy)/h));rect(x-a,y+yy,a*2+1,1,c) end
end
for y=0,319 do for x=0,527 do
 local patch=math.sin(x*.049+y*.031)+math.sin(x*.012-y*.07)+math.sin(x*.12+y*.11)*.4
 local colors={'252b30','2a302f','30352f','393a31','424035'}
 px(x,y,colors[math.max(1,math.min(5,math.floor(patch+3)))])
end end
-- Directional, clustered dead grass and copper leaves without a tiled pavement.
for i=1,1800 do
 local x=(i*131+i*i*17)%528;local y=(i*89+i*i*11)%320
 local c=({'444734','56523b','353d35','514339','61503d'})[i%5+1]
 line({x,y},{x+(i%3)-1,y-2-(i%3)},1,c)
 if i%4==0 then rect(x+2,y,2,1,c) end
end
local road=Image(528,320,ColorMode.RGB);local dist={}
for _,p in ipairs(layout.path) do
 local x,y=p.x/2,p.y/2
 for yy=math.max(0,math.floor(y-16)),math.min(319,math.ceil(y+16)) do
  for xx=math.max(0,math.floor(x-16)),math.min(527,math.ceil(x+16)) do
   local d=math.sqrt((xx-x)^2+(yy-y)^2);local k=yy*528+xx
   if d<16 and (not dist[k] or d<dist[k]) then dist[k]=d end
  end
 end
end
for k,d in pairs(dist) do
 local x,y=k%528,math.floor(k/528);local c
 if d<12 then
  local row=math.floor(y/5);local gx=(x+row%2*5)%11
  c=(gx==0 or y%5==0) and '303b49' or ({'60717b','526471','68747e','56666f'})[(math.floor(x/11)*3+row*7)%4+1]
 elseif d<14 then c='3f4b54' elseif (x+y)%3==0 then c='424236' end
 if c then road:drawPixel(x,y,C(c)) end
end
local layer=sp:newLayer();layer.name='Exact rounded road - same coordinates as runtime';sp:newCel(layer,1,road,Point(0,0))
local buildings=sp:newLayer();buildings.name='Gate anchors and cemetery perimeter guide';im=sp:newCel(buildings,1,Image(528,320,ColorMode.RGB),Point(0,0)).image
local function crypt(x,y,w,h)
 diamond(x,y+2,w,h,'17232e');rect(x-w,y-h-18,w*2,h+18,'243244')
 diamond(x,y-h-18,w,h,'536174');diamond(x,y-h-20,w-2,h-1,'405065')
 line({x-w,y-h-18},{x,y-18},1,'657186');line({x,y-18},{x+w,y-h-18},1,'293d52')
 rect(x-5,y-15,10,17,'101925');rect(x-3,y-12,6,10,'192738')
 rect(x-w+3,y-13,2,9,'52606b');rect(x+w-4,y-13,2,9,'34485c')
end
crypt(41,286,26,10);crypt(98,315,23,9);crypt(153,305,22,8)
crypt(20,225,16,7);crypt(12,313,25,10);crypt(513,314,22,10)
crypt(93,29,24,10);crypt(177,30,22,9)
-- Enemy gate: opening foot is exactly (17,62) art pixels.
rect(0,17,39,47,'2c3e50');rect(0,12,40,8,'4d5c6e')
rect(7,29,21,34,'0b1220');line({7,29},{17,20},4,'455870');line({17,20},{28,29},4,'455870')
rect(5,36,2,17,'bd558b');rect(29,33,2,14,'b95c90');rect(2,17,5,42,'394c62')
-- Castle gate: opening foot exactly (487,122), road approaches from SW.
rect(445,12,81,109,'24364a');rect(445,7,82,7,'3b5068')
rect(474,92,26,32,'091322');line({474,92},{487,77},4,'4b6076');line({487,77},{500,92},4,'4b6076')
for _,x in ipairs({451,463,505,517}) do rect(x,26,5,42,'26bfc6');rect(x+1,29,2,35,'68e9ec') end
rect(474,19,7,37,'c5539b');rect(485,15,7,42,'e77bbb')
for _,p in ipairs({{436,89},{507,146},{51,237},{157,278},{385,281},{17,167}}) do
 rect(p[1],p[2]-15,2,15,'182430');rect(p[1]-2,p[2]-17,6,7,'ce854f');rect(p[1],p[2]-16,2,5,'ffd28a')
end
local sitesLayer=sp:newLayer();sitesLayer.name='Empty foundation locations - keep clear';im=sp:newCel(sitesLayer,1,Image(528,320,ColorMode.RGB),Point(0,0)).image
for _,p in ipairs(layout.sites) do diamond(math.floor(p.x/2),math.floor(p.y/2),23,11,'263743');diamond(math.floor(p.x/2),math.floor(p.y/2),19,9,'455158') end
sp:saveAs(root..'source/environment/battlefield_layout.aseprite')
sp:saveCopyAs(root..'exports/environment/battlefield_layout.png')
sp:close()
print('v081 Aseprite scene guide saved; rounded route, 10 foundations, exact gate feet.')
