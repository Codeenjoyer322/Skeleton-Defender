-- Native Aseprite precise small inpaint guide: arch bottom center (17,62).
local v='C:/Users/Life/Desktop/Testnew2/NeonGothic/v081/'
local base=app.open(v..'source/environment/cemetery_battlefield.aseprite')
local flat=Image(base.spec);flat:drawSprite(base,1)
local sp=Sprite(64,80,ColorMode.RGB);local im=sp.cels[1].image;im:drawImage(flat,Point(0,0))
sp.layers[1].name='Original cemetery scene - local gate crop'
local layer=sp:newLayer();layer.name='Exact gate opening guide - road begins at17,62'
im=sp:newCel(layer,1,Image(64,80,ColorMode.RGB),Point(0,0)).image
local function col(h) return Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=255} end
local function rect(x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and yy>=0 and xx<64 and yy<80 then im:drawPixel(xx,yy,col(c)) end end end end
rect(0,8,39,52,'2b3d4c');rect(0,7,39,3,'4a586b');rect(0,10,39,2,'1b293a')
for y=14,56,6 do for x=0,38 do if x%9==math.floor(y/6)%2*4 then rect(x,y,1,5,'1d2a38') end end;rect(0,y,39,1,'26313f') end
rect(8,32,19,30,'0b1421');rect(10,27,15,5,'0b1421');rect(13,24,9,4,'0b1421');rect(16,22,3,3,'0b1421')
rect(5,33,3,28,'465969');rect(27,33,4,28,'394d5e')
for _,r in ipairs({{6,29,4,5},{8,25,4,4},{11,22,4,4},{14,19,6,4},{20,22,4,4},{23,25,4,4},{26,29,4,5}}) do rect(r[1],r[2],r[3],r[4],'65727d') end
for y=35,59,6 do rect(5,y,3,1,'203143');rect(27,y,4,1,'1c2d3d') end
rect(2,59,7,3,'728087');rect(27,59,7,3,'546776');rect(8,62,19,2,'60717b')
rect(33,34,1,15,'14212c');rect(31,32,5,7,'83583f');rect(32,33,3,5,'e2a05c');rect(33,34,1,3,'ffe9aa')
sp:saveAs(v..'source/environment/entrance_gate_guide.aseprite');sp:saveCopyAs(v..'source/environment/entrance_gate_guide.png')
local mask=Sprite(64,80,ColorMode.RGB);im=mask.cels[1].image
for y=0,79 do for x=0,63 do local a=x<=39 and y>=6 and y<=64;im:drawPixel(x,y,Color{r=a and 255 or 0,g=a and 255 or 0,b=a and 255 or 0,a=255}) end end
mask:saveCopyAs(v..'source/environment/entrance_gate_mask.png')
sp:close();mask:close();base:close();print('Precise entrance gate guide and mask saved.')
