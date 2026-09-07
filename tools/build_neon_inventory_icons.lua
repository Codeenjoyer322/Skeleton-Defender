-- Original editable 48px inventory art, rendered and exported by native Aseprite.
local base='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local runtime='C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/Icons/'
for _,p in ipairs({'source/icons','exports/icons','review'}) do app.fs.makeAllDirectories(base..p) end
app.fs.makeAllDirectories(runtime)
local P={ink='101421',deep='1b2035',shadow='27354b',steel='435e75',mid='64849a',light='9fbcc8',white='dcf5ea',
 cyan='62e7e8',ice='a1fff0',blue='286f91',magenta='bd4e91',pink='f48dbb',purple='553762',gold='bf8d40',goldlight='f3d78a',
 brown='684735',leather='8d6347',leatherlight='b28d62',darkleather='382c31',silk='39456e',silklight='697fb1'}
local function color(c,a) c=P[c] or c; return Color{r=tonumber(c:sub(1,2),16),g=tonumber(c:sub(3,4),16),b=tonumber(c:sub(5,6),16),a=a or 255} end
local sp,img,cache={},{},{}
local function canvas(name)
 sp=Sprite(48,48,ColorMode.RGB); sp.layers[1].name='01 - outline and dark volume'; img=sp.cels[1].image
end
local function layer(name) local l=sp:newLayer();l.name=name; local cel=sp:newCel(l,1,Image(48,48,ColorMode.RGB)); img=cel.image end
local function pixel(x,y,c) x=math.floor(x);y=math.floor(y); if x>=0 and x<img.width and y>=0 and y<img.height then img:drawPixel(x,y,color(c)) end end
local function rect(x,y,w,h,c) for yy=y,y+h-1 do for xx=x,x+w-1 do pixel(xx,yy,c) end end end
local function line(x1,y1,x2,y2,c,width)
 width=width or 1;local n=math.max(math.abs(x2-x1),math.abs(y2-y1),1)
 for i=0,n do rect(math.floor(x1+(x2-x1)*i/n-(width-1)/2+.5),math.floor(y1+(y2-y1)*i/n-(width-1)/2+.5),width,width,c) end
end
local function poly(points,c)
 local miny,maxy=48,-1;for _,p in ipairs(points) do miny=math.min(miny,p[2]);maxy=math.max(maxy,p[2]) end
 for y=miny,maxy do
  local hits={}; local sy=y+.25
  for i,a in ipairs(points) do local b=points[i%#points+1]; if (a[2]<=sy and b[2]>sy)or(b[2]<=sy and a[2]>sy)then hits[#hits+1]=a[1]+(sy-a[2])*(b[1]-a[1])/(b[2]-a[2]) end end
  table.sort(hits); for i=1,#hits-1,2 do for x=math.ceil(hits[i]),math.floor(hits[i+1]) do pixel(x,y,c) end end
 end
end
local function shape(points,c)
 for i,a in ipairs(points)do local b=points[i%#points+1];line(a[1],a[2],b[1],b[2],'ink',3)end
 poly(points,c)
end
local function ellipse(cx,cy,rx,ry,c)
 for y=-ry,ry do for x=-rx,rx do if (x*x)/(rx*rx)+(y*y)/(ry*ry)<=1 then pixel(cx+x,cy+y,c) end end end
end
local function gem(cx,cy,r,c)
 shape({{cx,cy-r},{cx+r-1,cy},{cx,cy+r},{cx-r+1,cy}},c or 'cyan')
 line(cx,cy-r+1,cx,cy,'white');line(cx,cy,cx-r+2,cy,c=='pink' and 'magenta' or 'blue')
end
local function stitch(x1,y1,x2,y2,c)
 local n=math.floor(math.max(math.abs(x2-x1),math.abs(y2-y1))/3)
 for i=0,n do pixel(x1+(x2-x1)*i/math.max(n,1),y1+(y2-y1)*i/math.max(n,1),c) end
end
local function buckle(cx,cy,w)
 rect(cx-w/2-1,cy-3,w+2,7,'ink');rect(cx-w/2,cy-2,w,5,'gold');rect(cx-w/2+1,cy-1,w-2,3,'darkleather');line(cx-w/2+1,cy-2,cx+w/2-2,cy-2,'goldlight');line(cx,cy,cx+w/2,cy,'goldlight')
end
local function rings(x,y,w,h)
 for yy=y,y+h-3,4 do for xx=x+(math.floor((yy-y)/4)%2)*2,x+w-4,4 do
  pixel(xx+1,yy,'light');pixel(xx+2,yy,'mid');pixel(xx,yy+1,'steel');pixel(xx+3,yy+1,'deep');pixel(xx+1,yy+2,'shadow');pixel(xx+2,yy+2,'deep')
 end end
end
local function shine(x,y,c) line(x-2,y,x+2,y,c);line(x,y-2,x,y+2,c);pixel(x,y,'white') end
local function save(name)
 app.activeSprite=sp
 sp:saveAs(base..'source/icons/'..name..'.aseprite')
 sp:saveCopyAs(base..'exports/icons/'..name..'.png')
 sp:saveCopyAs(runtime..name..'.png')
 local flattened=Image(48,48,ColorMode.RGB);flattened:drawSprite(sp,1);cache[#cache+1]={name=name,image=flattened};sp:close()
end

-- Leather: warm hide, stitched edges, brass fittings, cold enamel stones.
canvas('leather_helmet')
shape({{10,33},{11,20},{16,10},{24,6},{33,12},{38,26},{37,37},{29,40},{27,32},{28,19},{21,16},{17,24},{18,38}},'brown')
poly({{13,28},{14,20},{20,11},{24,9},{26,14},{20,16},{16,25},{16,34}},'leatherlight')
poly({{25,9},{32,14},{35,26},{33,34},{29,32},{31,20}},'leather')
layer('02 - seams and talisman');line(15,33,17,23,'darkleather');stitch(12,30,18,15,'goldlight');stitch(30,16,35,32,'leatherlight');line(18,14,28,15,'gold');gem(25,13,3);rect(12,35,6,2,'gold');save('leather_helmet')
canvas('leather_chest')
shape({{8,17},{15,10},{20,10},{22,14},{26,14},{28,10},{34,11},{40,18},{35,25},{31,23},{33,40},{15,40},{17,23},{12,26}},'brown')
poly({{16,13},{20,16},{24,18},{23,37},{17,37},{19,22},{13,21},{11,17}},'leather')
poly({{27,17},{32,13},{36,18},{32,20},{29,36},{25,37}},'darkleather')
layer('02 - harness, studs and seams');line(16,13,29,31,'darkleather',4);line(15,13,29,31,'leatherlight');line(24,19,24,37,'ink');stitch(18,38,30,38,'goldlight');rect(15,30,18,5,'darkleather');buckle(24,32,7);for _,x in ipairs({13,34})do pixel(x,18,'goldlight');pixel(x+1,21,'gold')end;save('leather_chest')
canvas('leather_gloves')
shape({{8,11},{18,10},{20,21},{24,24},{21,30},{21,37},{16,41},{10,38},{7,30},{9,24}},'brown')
shape({{28,14},{38,15},{37,27},{40,30},{36,37},{29,39},{25,35},{25,28},{28,25}},'brown')
poly({{10,14},{16,13},{17,26},{20,28},{18,36},{14,37},{11,30}},'leatherlight')
poly({{30,18},{35,18},{34,28},{37,31},{33,36},{29,35},{28,29}},'leather')
layer('02 - cuff straps and fingers');line(9,21,19,20,'darkleather',3);line(28,24,36,25,'darkleather',3);rect(12,19,4,3,'gold');rect(31,24,3,2,'goldlight');for _,x in ipairs({12,15,18})do line(x,32,x+1,36,'brown')end;line(29,32,30,35,'darkleather');line(32,31,33,34,'darkleather');save('leather_gloves')
canvas('leather_belt')
shape({{6,17},{15,14},{34,14},{42,18},{41,28},{34,31},{15,30},{7,27}},'darkleather')
poly({{8,19},{17,17},{34,17},{39,20},{39,25},{32,27},{16,26},{8,24}},'leather')
layer('02 - embossed border, pouch and buckle');stitch(9,18,38,18,'leatherlight');stitch(10,25,35,28,'brown');buckle(24,22,12);rect(9,21,2,1,'ink');rect(13,21,2,1,'ink');shape({{32,25},{39,25},{38,36},{31,34}},'brown');line(33,27,38,28,'leatherlight');pixel(35,29,'goldlight');save('leather_belt')
canvas('leather_trousers')
shape({{13,8},{34,8},{34,21},{32,40},{24,40},{23,26},{21,40},{12,39},{12,23}},'brown')
poly({{15,12},{23,12},{21,26},{18,37},{14,37}},'leather')
poly({{26,12},{31,12},{31,37},{26,37},{26,25},{24,20}},'darkleather')
layer('02 - knee pads and stitching');rect(13,9,20,4,'darkleather');buckle(24,11,6);shape({{13,26},{20,27},{19,33},{13,32}},'leatherlight');shape({{26,27},{31,26},{32,32},{27,33}},'leather');stitch(14,15,14,24,'goldlight');stitch(31,14,30,24,'leatherlight');line(25,17,27,22,'brown');save('leather_trousers')
canvas('leather_boots')
shape({{9,10},{20,10},{19,30},{23,33},{23,40},{6,40},{6,35},{10,30}},'brown')
shape({{28,12},{39,12},{37,30},{43,33},{43,40},{27,40},{26,35},{29,30}},'darkleather')
poly({{12,14},{17,14},{15,32},{10,36},{19,37},{8,37},{9,34},{13,30}},'leatherlight')
poly({{30,16},{36,16},{34,32},{39,35},{39,37},{29,37},{29,34},{31,29}},'leather')
layer('02 - brass ankle buckles');for _,xy in ipairs({{10,20},{10,26},{28,22},{28,28}})do rect(xy[1],xy[2],9,3,'darkleather');rect(xy[1]+4,xy[2],3,2,'gold')end;line(7,39,22,39,'ink');line(28,39,42,39,'ink');stitch(17,15,16,19,'goldlight');save('leather_boots')

-- Chainmail: blue steel, articulated edges and individual interlocking rings.
canvas('chainmail_helmet')
shape({{10,29},{11,17},{16,9},{24,5},{32,9},{37,18},{37,30},{30,38},{25,39},{24,27},{21,28},{20,38},{13,35}},'steel')
poly({{13,20},{17,11},{24,8},{24,18},{15,24}},'light')
poly({{26,9},{32,12},{34,19},{31,25},{28,35},{26,35}},'shadow')
layer('02 - visor, cheek plates and cyan rune');shape({{12,22},{22,23},{22,27},{13,25}},'ink');shape({{26,23},{35,21},{34,25},{27,27}},'ink');line(14,24,20,25,'cyan');line(28,25,33,23,'cyan');line(24,10,24,36,'mid',2);pixel(23,11,'white');line(14,28,15,33,'mid');line(30,29,29,33,'mid');gem(24,17,3);save('chainmail_helmet')
canvas('chainmail_chest')
shape({{7,15},{15,10},{20,10},{22,14},{27,14},{29,10},{34,11},{41,16},{37,26},{32,24},{33,41},{14,41},{15,24},{10,26}},'shadow')
poly({{15,16},{21,18},{27,18},{31,15},{31,38},{16,38}},'steel');rings(15,18,17,22)
layer('02 - shoulder plates and straps');shape({{7,16},{15,11},{19,15},{16,22},{10,22}},'mid');shape({{29,15},{34,12},{40,17},{36,23},{31,21}},'steel');line(9,16,15,13,'light');line(32,15,37,18,'light');rect(15,31,17,4,'deep');buckle(24,33,7);line(20,12,22,16,'gold');line(28,12,26,16,'gold');pixel(24,17,'cyan');save('chainmail_chest')
canvas('chainmail_gloves')
shape({{8,9},{20,10},{18,22},{23,25},{21,33},{18,40},{10,40},{7,32},{9,23}},'shadow')
shape({{28,12},{39,13},{37,24},{41,27},{39,33},{35,39},{28,38},{25,31},{28,24}},'shadow')
layer('02 - articulated finger plates');shape({{9,11},{18,12},{17,23},{11,22}},'mid');shape({{29,15},{37,15},{35,25},{29,24}},'steel');line(10,13,17,14,'light');line(29,16,36,17,'light');for y=25,34,4 do line(10,y,18,y+1,'mid',2);line(29,y+2,36,y+3,'steel',2)end;for x=10,18,3 do line(x,34,x,38,'light')end;line(29,34,29,36,'light');line(33,34,33,37,'mid');rect(12,18,3,2,'cyan');rect(31,21,3,1,'cyan');save('chainmail_gloves')
canvas('chainmail_belt')
shape({{6,17},{16,13},{34,13},{42,18},{41,29},{34,32},{14,31},{6,26}},'deep')
for i=0,4 do local x=8+i*7;shape({{x,18},{x+5,16},{x+5,27},{x,26}},i<2 and 'mid' or 'steel');pixel(x+2,18,'light');pixel(x+2,25,'gold')end
layer('02 - central clasp');shape({{18,17},{24,14},{30,18},{29,28},{24,31},{18,27}},'gold');poly({{20,19},{24,17},{28,20},{27,26},{24,28},{20,26}},'shadow');gem(24,23,4);save('chainmail_belt')
canvas('chainmail_trousers')
shape({{13,8},{34,8},{35,21},{33,40},{25,40},{23,26},{21,40},{12,40},{12,23}},'shadow');rings(14,13,19,13);rings(13,25,10,15);rings(25,24,9,16)
layer('02 - knee and waist steel');rect(13,9,20,4,'steel');line(14,9,31,9,'light');buckle(24,11,6);shape({{12,26},{21,26},{20,33},{16,35},{12,32}},'mid');shape({{25,27},{34,26},{34,33},{29,35},{26,32}},'steel');line(14,27,19,27,'light');line(27,28,31,27,'mid');line(15,30,19,30,'steel');save('chainmail_trousers')
canvas('chainmail_boots')
shape({{10,9},{21,10},{20,29},{24,34},{23,40},{6,40},{5,35},{10,28}},'shadow')
shape({{29,11},{40,12},{38,29},{43,34},{42,40},{26,40},{26,35},{29,29}},'shadow')
layer('02 - greaves and layered sabatons');shape({{11,11},{19,12},{17,30},{11,31}},'mid');line(12,13,12,27,'light');shape({{30,14},{37,15},{36,30},{29,32}},'steel');line(31,15,31,29,'mid');for y=31,37,3 do line(9,y+1,20,y+2,'mid',2);line(29,y+1,39,y+2,'steel',2)end;line(7,38,22,38,'light');line(28,38,41,38,'mid');gem(15,19,3);gem(34,21,3);save('chainmail_boots')

-- Silk: tailored arcane cloth, layered violet folds and embroidered gold edges.
canvas('silk_helmet')
shape({{8,34},{13,25},{15,17},{24,6},{33,17},{35,26},{41,34},{33,39},{27,35},{29,23},{24,17},{19,23},{21,35},{15,39}},'silk')
poly({{11,33},{17,18},{24,9},{22,16},{17,23},{17,35}},'silklight');poly({{27,13},{32,20},{32,29},{36,35},{31,35},{29,25}},'purple')
layer('02 - embroidered cowl');line(18,21,24,15,'gold');line(24,15,30,22,'goldlight');line(18,23,20,34,'magenta');line(30,24,28,34,'pink');stitch(12,34,16,36,'goldlight');stitch(32,36,37,34,'goldlight');gem(24,14,3,'pink');save('silk_helmet')
canvas('silk_chest')
shape({{8,15},{18,9},{23,14},{26,14},{30,9},{40,16},{37,28},{31,25},{34,41},{13,41},{17,25},{11,28}},'silk')
poly({{18,13},{23,18},{21,27},{17,38},{14,39},{19,26},{14,21},{12,18}},'silklight')
poly({{29,13},{34,17},{31,25},{32,38},{27,38},{25,23}},'purple')
layer('02 - gold trim and arcane sash');line(19,11,24,18,'goldlight');line(29,11,24,18,'gold');rect(17,25,14,4,'magenta');gem(24,27,3);poly({{25,30},{28,30},{31,40},{26,40}},'magenta');line(15,39,31,39,'gold');line(11,24,15,23,'goldlight');line(34,23,37,25,'gold');pixel(24,21,'cyan');save('silk_chest')
canvas('silk_gloves')
shape({{9,9},{19,11},{17,24},{21,27},{19,34},{16,40},{10,38},{8,31},{10,24}},'silk')
shape({{29,12},{39,14},{36,26},{39,30},{35,38},{29,39},{26,35},{27,29},{29,25}},'purple')
poly({{11,13},{16,14},{14,25},{17,29},{15,36},{12,35},{11,29}},'silklight');poly({{30,16},{35,18},{33,27},{35,30},{33,36},{30,36},{29,30}},'silk')
layer('02 - wrist embroidery');line(10,20,17,22,'gold',2);line(29,24,35,26,'gold',2);gem(14,20,3,'pink');gem(33,24,3,'pink');line(12,29,13,35,'light');line(30,30,31,35,'magenta');save('silk_gloves')
canvas('silk_belt')
shape({{7,17},{17,15},{33,15},{40,18},{39,27},{29,28},{34,39},{29,41},{22,28},{9,27}},'silk')
poly({{9,19},{19,17},{33,18},{37,20},{36,24},{12,24}},'silklight');poly({{25,27},{29,28},{32,37},{30,38}},'purple')
layer('02 - embroidered sash and clasp');line(9,18,35,18,'goldlight');line(10,26,37,26,'gold');gem(24,22,5,'pink');line(26,29,31,38,'magenta');line(29,40,33,38,'goldlight');save('silk_belt')
canvas('silk_trousers')
shape({{13,8},{34,8},{35,25},{32,40},{25,40},{23,27},{21,40},{13,40},{11,25}},'silk')
poly({{16,12},{22,12},{20,27},{18,38},{14,38},{14,25}},'silklight');poly({{27,12},{32,12},{31,24},{31,38},{27,38},{26,24}},'purple')
layer('02 - folds, gold cuffs and sash');rect(13,9,20,4,'magenta');gem(24,11,3);line(16,17,16,31,'light');line(19,19,18,32,'steel');line(29,17,28,31,'magenta');rect(13,37,8,2,'gold');rect(25,37,8,2,'gold');pixel(15,38,'goldlight');pixel(27,38,'goldlight');save('silk_trousers')
canvas('silk_boots')
shape({{11,10},{21,10},{19,28},{23,32},{22,38},{12,41},{6,39},{7,35},{12,29}},'silk')
shape({{29,13},{39,13},{36,29},{42,33},{43,37},{37,40},{27,39},{26,35},{30,29}},'purple')
poly({{13,13},{18,13},{16,30},{19,34},{14,37},{9,37},{10,34},{14,28}},'silklight');poly({{31,16},{36,16},{33,30},{39,34},{38,36},{30,36},{29,34},{32,28}},'silk')
layer('02 - delicate gold filigree');line(12,13,19,13,'goldlight');line(30,16,37,16,'gold');line(13,20,16,27,'gold');line(33,22,34,29,'gold');gem(15,24,3,'pink');gem(33,26,3,'pink');line(9,38,16,39,'goldlight');line(30,38,40,37,'gold');save('silk_boots')

-- Weapons: directional silhouettes, readable edges and separately shaded faces.
canvas('weapon_sword')
shape({{15,30},{32,8},{39,5},{38,13},{21,35}},'steel')
poly({{17,30},{34,9},{37,8},{35,14},{20,32}},'light');line(18,29,36,9,'white');line(22,29,34,14,'cyan');
layer('02 - crossguard and leather grip');line(10,28,24,39,'ink',5);line(10,28,24,39,'gold',3);line(10,28,17,32,'goldlight');line(15,33,8,41,'ink',5);line(15,33,8,41,'brown',3);line(14,35,16,37,'goldlight');line(11,38,13,40,'gold');ellipse(7,42,3,3,'ink');ellipse(7,42,2,2,'goldlight');gem(17,33,3);save('weapon_sword')
canvas('weapon_dagger')
shape({{20,28},{25,16},{34,9},{36,9},{35,18},{25,32}},'steel');poly({{22,27},{29,16},{34,12},{31,20},{25,28}},'light');line(24,27,33,14,'white');line(22,28,29,22,'cyan');
layer('02 - hooked guard and bound grip');line(14,27,27,36,'ink',5);line(14,27,27,36,'gold',3);line(15,27,19,30,'goldlight');line(20,32,12,41,'ink',6);line(20,32,12,41,'purple',3);line(17,35,20,37,'magenta');line(14,38,17,40,'pink');ellipse(11,42,3,3,'gold');pixel(10,41,'goldlight');save('weapon_dagger')
canvas('weapon_staff')
line(14,42,29,17,'ink',6);line(14,42,29,17,'brown',4);line(14,41,27,19,'gold');
shape({{25,22},{22,15},{24,8},{30,3},{36,6},{39,13},{36,20},{31,23}},'gold')
poly({{26,19},{25,14},{27,8},{31,6},{35,9},{36,14},{33,19},{30,20}},'deep')
layer('02 - crystal, prongs and spark');gem(31,12,6);line(24,11,26,7,'goldlight');line(36,9,37,13,'goldlight');line(26,19,30,22,'goldlight');line(18,32,21,34,'goldlight',2);line(15,38,18,40,'gold',2);shine(41,6,'cyan');pixel(21,4,'cyan');save('weapon_staff')
canvas('artifact_aegis_of_dawn')
shape({{9,12},{17,9},{24,5},{31,9},{39,12},{37,29},{32,36},{24,43},{16,37},{11,29}},'gold')
shape({{13,15},{20,12},{24,9},{29,12},{35,15},{33,28},{28,35},{24,38},{18,34},{15,28}},'steel')
poly({{14,16},{24,11},{24,35},{19,32},{17,26}},'mid');poly({{25,12},{32,16},{30,29},{25,35}},'shadow')
layer('02 - dawn sun, wings and rivets');for _,a in ipairs({{24,14,24,17},{15,22,18,22},{30,22,33,22},{18,16,20,18},{28,18,30,16},{18,28,20,26},{28,27,30,29}})do line(a[1],a[2],a[3],a[4],'goldlight')end;gem(24,23,6,'goldlight');line(24,19,24,25,'white');for _,xy in ipairs({{11,14},{36,14},{16,32},{32,32}})do pixel(xy[1],xy[2],'white')end;save('artifact_aegis_of_dawn')
canvas('artifact_zeus_nail')
-- A short curved divine nail clipping, held in a gold reliquary. It is not a weapon claw.
shape({{11,15},{17,9},{30,7},{37,12},{40,22},{36,33},{26,40},{15,36},{8,28}},'gold')
poly({{12,17},{18,12},{29,11},{34,15},{37,23},{33,30},{25,36},{16,32},{12,27}},'deep')
shape({{15,21},{17,17},{23,15},{29,16},{33,20},{34,23},{32,25},{29,22},{24,21},{20,23},{17,26},{15,24}},'light')
poly({{17,20},{19,18},{24,17},{29,18},{32,21},{31,22},{28,20},{23,19},{20,20},{17,23}},'white');line(17,24,20,22,'mid');line(29,22,32,24,'cyan');
layer('02 - divine lightning and reliquary');line(24,5,22,11,'cyan',2);line(22,11,27,10,'cyan',2);line(27,10,25,15,'ice',2);line(34,28,30,32,'cyan',2);line(30,32,34,32,'cyan',2);line(34,32,29,38,'ice',2);gem(13,29,3,'pink');pixel(15,14,'goldlight');pixel(34,15,'goldlight');save('artifact_zeus_nail')
canvas('artifact_athena_mirror')
ellipse(24,19,13,16,'ink');ellipse(24,19,11,14,'gold');ellipse(24,19,8,11,'blue');
poly({{18,13},{22,10},{29,12},{30,20},{23,26},{18,25}},'mid');poly({{19,13},{24,11},{21,22},{18,23}},'cyan');line(26,11,20,25,'ice',2);
shape({{20,31},{27,31},{29,41},{24,44},{19,41}},'gold');poly({{23,33},{25,33},{26,40},{23,41}},'purple')
layer('02 - owl crest and polished frame');shape({{15,7},{17,3},{24,6},{31,3},{33,7},{28,10},{24,9},{20,10}},'gold');pixel(20,7,'white');pixel(28,7,'white');pixel(24,9,'ink');line(14,17,14,24,'goldlight');line(31,24,28,30,'goldlight');gem(24,37,3,'pink');shine(37,11,'cyan');save('artifact_athena_mirror')

-- A native contact sheet at exact 2x pixels. Each row is one material family.
local font={A={'010','101','111','101','101'},B={'110','101','110','101','110'},C={'011','100','100','100','011'},D={'110','101','101','101','110'},E={'111','100','110','100','111'},F={'111','100','110','100','100'},G={'011','100','101','101','011'},H={'101','101','111','101','101'},I={'111','010','010','010','111'},J={'001','001','001','101','010'},K={'101','101','110','101','101'},L={'100','100','100','100','111'},M={'101','111','111','101','101'},N={'101','111','111','111','101'},O={'010','101','101','101','010'},P={'110','101','110','100','100'},Q={'010','101','101','111','011'},R={'110','101','110','101','101'},S={'011','100','010','001','110'},T={'111','010','010','010','010'},U={'101','101','101','101','111'},V={'101','101','101','101','010'},W={'101','101','111','111','101'},X={'101','101','010','101','101'},Y={'101','101','010','010','010'},Z={'111','001','010','100','111'},['_']={'000','000','000','000','111'},[' ']={'000','000','000','000','000'}}
sp=Sprite(768,464,ColorMode.RGB);sp.layers[1].name='24 original item icons - 2x nearest pixels';img=sp.cels[1].image
rect(0,0,768,464,'101725')
local labels={'LEATHER HOOD','LEATHER CHEST','LEATHER GLOVES','LEATHER BELT','LEATHER TROUSERS','LEATHER BOOTS','STEEL HELMET','CHAINMAIL CHEST','STEEL GAUNTLETS','STEEL BELT','CHAINMAIL LEGS','STEEL BOOTS','SILK COWL','SILK ROBE','SILK GLOVES','SILK SASH','SILK TROUSERS','SILK BOOTS','SWORD','DAGGER','STAFF','AEGIS OF DAWN','ZEUS NAIL','ATHENA MIRROR'}
for i,entry in ipairs(cache)do
 local col=(i-1)%6;local row=math.floor((i-1)/6);local ox=col*128;local oy=row*116
 rect(ox+4,oy+4,120,108,'1b2638');rect(ox+5,oy+5,118,106,'111b2a')
 for y=0,47 do for x=0,47 do local v=entry.image:getPixel(x,y);local c=Color(v);if c.alpha>0 then for dy=0,1 do for dx=0,1 do img:drawPixel(ox+16+x*2+dx,oy+5+y*2+dy,c)end end end end end
 local label=labels[i];local tx=ox+math.floor((128-#label*4)/2)
 for n=1,#label do local g=font[label:sub(n,n)];if g then for yy=1,5 do for xx=1,3 do if g[yy]:sub(xx,xx)=='1'then pixel(tx+(n-1)*4+xx-1,oy+106+yy-1,'light')end end end end end
end
sp:saveAs(base..'source/icons/contact_sheet.aseprite');sp:saveCopyAs(base..'review/inventory-icons-contact.png');sp:close()
print('EXPORTED: 24 original 48x48 icons, editable Aseprite layers, PNG review, runtime PNG, 2x contact sheet.')
