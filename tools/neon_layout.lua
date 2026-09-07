-- Native Aseprite: battlefield composition guide and reusable UI metalwork.
local base = 'C:/Users/Life/Desktop/Testnew2/NeonGothic/'
app.fs.makeAllDirectories(base..'source/environment')
app.fs.makeAllDirectories(base..'exports/environment')
local function color(h,a) return Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=a or 255} end
local sp,img
local function canvas(w,h,name)
 sp=Sprite(w,h,ColorMode.RGB); sp.layers[1].name=name; img=sp.cels[1].image
end
local function rect(x,y,w,h,c)
 for yy=math.max(0,y),math.min(img.height-1,y+h-1) do for xx=math.max(0,x),math.min(img.width-1,x+w-1) do img:drawPixel(xx,yy,color(c)) end end
end
local function line(x,y,xx,yy,width,c)
 local n=math.max(math.abs(xx-x),math.abs(yy-y),1)
 for i=0,n do rect(math.floor(x+(xx-x)*i/n-width/2),math.floor(y+(yy-y)*i/n-width/2),width,width,c) end
end
local function diamond(x,y,w,h,c)
 for yy=-h,h do local hw=math.floor(w*(1-math.abs(yy)/h)); rect(x-hw,y+yy,hw*2+1,1,c) end
end
local function save(name)
 sp:saveAs(base..'source/environment/'..name..'.aseprite')
 sp:saveCopyAs(base..'exports/environment/'..name..'.png'); sp:close()
end
canvas(528,320,'Battlefield spatial guide - preserve route')
rect(0,0,528,320,'111725')
for y=-10,340,18 do for x=-36,560,36 do diamond(x+(math.floor(y/18)%2)*18,y,17,8,'1c3045') end end
local path={{-16,77},{75,77},{75,165},{185,165},{185,65},{292.5,65},{292.5,225},{400,225},{400,140},{495.5,140}}
for i=1,#path-1 do line(path[i][1],path[i][2],path[i+1][1],path[i+1][2],29,'0b1121') end
for i=1,#path-1 do line(path[i][1],path[i][2],path[i+1][1],path[i+1][2],24,'3c566a') end
for i=1,#path-1 do line(path[i][1],path[i][2],path[i+1][1],path[i+1][2],17,'587184') end
local sites={{37,122},{126,109.5},{132,207.5},{235.5,122.5},{237.5,27},{345,108.5},{249.5,267.5},{355,267.5},{449.5,186},{440.5,90.5}}
for _,p in ipairs(sites) do diamond(math.floor(p[1]),math.floor(p[2]),16,9,'294153'); diamond(math.floor(p[1]),math.floor(p[2]),11,5,'426578') end
-- Landmarks: enemy portal left and defended gate right. Keep ground anchors clear.
rect(0,31,19,39,'0c1224'); line(4,35,4,65,3,'bd427e'); line(0,35,16,35,3,'bd427e')
rect(477,32,46,91,'23354d'); rect(482,47,6,39,'1dd9dc'); rect(512,44,5,42,'ed418f'); rect(490,84,17,53,'08101c'); line(490,85,498,75,3,'57728a'); line(498,75,507,85,3,'57728a')
save('battlefield_layout')

canvas(192,240,'Obsidian arch')
rect(0,0,192,240,'111525'); rect(3,3,186,234,'293346'); rect(6,6,180,228,'141c30')
for y=10,225,18 do for x=9,177,32 do rect(x+(math.floor(y/18)%2)*9,y,25,1,'233349') end end
-- Eight-sided vaulted portrait alcove.
line(24,227,24,55,3,'41516b'); line(24,55,96,15,3,'597088'); line(96,15,168,55,3,'41516b'); line(168,55,168,227,3,'41516b')
line(29,219,29,57,1,'47becb'); line(29,57,96,21,1,'47becb'); line(96,21,163,57,1,'ab428b'); line(163,57,163,219,1,'ab428b')
for x=0,7 do diamond(96,216,52-x*5,14-x, x%2==0 and '304c65' or '18283c') end
line(47,216,96,203,1,'38d9d4');line(96,203,145,216,1,'b858ae');line(47,216,96,229,1,'38a0ae');line(96,229,145,216,1,'625084')
save('portrait_arch')

-- Separate transparent ornament is tiled by the interface without scaling pixels.
canvas(96,24,'Runic metal ornament')
line(1,12,33,12,1,'41667e'); line(63,12,94,12,1,'41667e')
diamond(48,12,11,9,'253a50');diamond(48,12,8,6,'408fa2');diamond(48,12,5,3,'63fff1')
diamond(31,12,3,3,'c257a0');diamond(65,12,3,3,'c257a0');save('rune_ornament')
print('Native Aseprite layout and UI source files saved.')
