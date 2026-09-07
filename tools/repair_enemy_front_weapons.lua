-- Restore the existing characters' held equipment on separate Aseprite layers.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local sp,img
local function c(h) return Color{r=tonumber(h:sub(1,2),16),g=tonumber(h:sub(3,4),16),b=tonumber(h:sub(5,6),16),a=255} end
local function dot(x,y,h) if x>=0 and y>=0 and x<img.width and y<img.height then img:drawPixel(x,y,c(h)) end end
local function rect(x,y,w,h,color) for yy=y,y+h-1 do for xx=x,x+w-1 do dot(xx,yy,color) end end end
local function line(x,y,xx,yy,width,color)
 local n=math.max(math.abs(xx-x),math.abs(yy-y),1)
 for i=0,n do rect(math.floor(x+(xx-x)*i/n-width/2+.5),math.floor(y+(yy-y)*i/n-width/2+.5),width,width,color) end
end
local function poly(points,color)
 local x0,y0,x1,y1=128,128,0,0
 for _,p in ipairs(points) do x0=math.min(x0,p[1]);x1=math.max(x1,p[1]);y0=math.min(y0,p[2]);y1=math.max(y1,p[2]) end
 for y=y0,y1 do for x=x0,x1 do
  local inside=false;local j=#points
  for i=1,#points do local a,b=points[i],points[j]
   if (a[2]>y)~=(b[2]>y) and x<(b[1]-a[1])*(y-a[2])/(b[2]-a[2])+a[1] then inside=not inside end
   j=i
  end
  if inside then dot(x,y,color) end
 end end
end
local function layer(name,behind)
 local l=sp:newLayer();l.name=name;if behind then l.stackIndex=1 end
 img=sp:newCel(l,sp.frames[1],Image(128,128,ColorMode.RGB),Point(0,0)).image;return l
end
local function begin(role)
 sp=app.open(root..'raw/actors/'..role..'_front_edit_pixen/job_00/frames/000.png')
 sp.layers[1].name='PixelLab frontal body - preserved'
 layer('Aseprite restored equipment behind hand',true)
end
local function save(role)
 local dest=root..'source/references/'..role..'/';app.fs.makeAllDirectories(dest)
 sp:saveAs(dest..'front-fixed.aseprite');sp:saveCopyAs(dest..'front-fixed.png')
 sp:resize{width=512,height=512};sp:saveCopyAs(root..'review/'..role..'-front-fixed-4x.png');sp:close()
end
begin('tutankhamun')
line(78,116,83,60,4,'15202a');line(78,115,83,59,2,'85622f');line(79,115,84,60,1,'c5aa58')
poly({{79,60},{79,53},{83,48},{87,53},{86,59},{82,63}},'15202a')
poly({{80,58},{80,53},{83,50},{86,53},{85,58},{82,61}},'167785')
poly({{81,56},{81,53},{83,51},{85,53},{84,57}},'51d3c6');line(83,51,83,55,1,'b5f4d6')
rect(81,61,4,2,'cfb265');rect(80,66,4,2,'b99a4d')
layer('Bone fingers holding turquoise staff')
line(79,83,82,84,2,'dbcd95');dot(82,85,'f1e3ae');dot(80,87,'ad985e')
save('tutankhamun')

begin('samurai')
-- Lower guard position: hand at 43,85; the blade points away from the legs.
line(46,79,41,91,4,'161b28');line(46,79,41,91,2,'624c34')
dot(44,82,'b8a377');dot(43,86,'b8a377')
line(37,89,45,94,3,'161b28');line(37,89,45,94,1,'c2a354')
poly({{39,93},{43,95},{35,106},{20,118},{15,120},{19,115},{31,102}},'182331')
poly({{39,95},{41,96},{33,106},{19,117},{17,118},{24,109},{32,103}},'909fa6')
line(40,96,32,105,1,'dce5dc');line(32,105,18,118,1,'dce5dc');line(37,99,27,109,1,'536779')
layer('Bone fingers gripping katana')
line(42,82,44,84,2,'d4bf8c');dot(42,85,'e7d49d');dot(43,86,'9b8252')
save('samurai')

begin('knight')
-- Sword in the character's right hand; shield in the left hand.
line(45,79,42,92,4,'101d29');line(45,79,42,92,2,'705537')
line(37,90,47,93,3,'101d29');line(37,90,47,93,1,'c1b28b')
poly({{41,94},{45,95},{35,115},{29,121},{30,113}},'11212c')
poly({{41,95},{43,96},{33,117},{30,119},{33,111}},'859da8');line(42,97,31,119,1,'cddbd7')
layer('Knight shield - held in front of forearm')
poly({{87,67},{99,74},{97,94},{87,106},{76,96},{74,74}},'122332')
poly({{87,69},{97,76},{95,93},{87,102},{78,94},{76,76}},'6f8e9e')
poly({{87,72},{94,78},{92,92},{87,98},{81,92},{79,78}},'304b60')
line(87,74,87,95,1,'718a99');line(81,83,92,83,1,'718a99')
rect(85,82,4,4,'9aabb0');dot(85,82,'d4dbce')
save('knight')

begin('warlock')
line(84,118,87,38,4,'111a27');line(84,118,87,38,2,'806044');line(85,115,88,39,1,'c0a375')
poly({{82,37},{81,31},{84,23},{87,19},{90,26},{93,31},{91,37},{87,42}},'191b30')
poly({{84,36},{83,31},{85,25},{87,22},{89,28},{91,31},{89,36},{87,39}},'635293')
poly({{85,34},{84,30},{87,24},{90,31},{88,35},{87,37}},'bb89d9')
line(87,25,87,34,1,'f3d6ec');dot(86,30,'dbc6ef');dot(88,32,'dbc6ef')
rect(85,39,5,2,'c09a62');rect(84,45,5,2,'ab8659')
layer('Bone fingers holding violet staff')
line(82,83,85,84,2,'d3c29c');dot(85,85,'ece0b4');dot(83,87,'9b8664')
save('warlock')
print('Four existing frontal characters have their held equipment restored in native Aseprite.')
