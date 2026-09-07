-- Local native Aseprite cleanup of PixelLab's pilot; never modifies raw generation.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local source=root..'raw/actors/circe_8views_reference_v1/animations/staff_attack/east/'
local output=root..'source/actors/circe_staff_attack_east_clean/'
app.fs.makeAllDirectories(output..'frames')
local centers={{65,36},{70,35},{80,52},{80,62},{79,62},{80,61},{80,58},{75,39},{65,36}}
local regions={{59,24,73,44},{63,27,78,44},{67,39,91,61},{70,43,91,76},{70,43,91,79},{70,50,91,70},{70,47,91,69},{66,28,87,48},{59,24,73,44}}
local stems={{64,45,65,40},{66,45,69,39},{72,55,76,53},{73,62,76,62},{71,62,75,62},{74,61,76,61},{73,59,76,58},{70,46,74,43},{64,45,65,40}}
local times={120,160,270,90,100,150,140,50,80}
local sprite=Sprite(104,104,ColorMode.RGB);sprite.layers[1].name='Original body and staff - cyan overshoot removed'
local crystalLayer=sprite:newLayer();crystalLayer.name='Hand refined cyan crystal - no free projectile'
local function col(r,g,b)return Color{r=r,g=g,b=b,a=255}end
local function poly(im,pts,c)
 for y=0,103 do local hit={};local yy=y+.25
  for i,p in ipairs(pts)do local q=pts[i%#pts+1];if(p[2]<=yy and q[2]>yy)or(q[2]<=yy and p[2]>yy)then hit[#hit+1]=p[1]+(yy-p[2])*(q[1]-p[1])/(q[2]-p[2])end end
  table.sort(hit);for i=1,#hit-1,2 do for x=math.ceil(hit[i]),math.floor(hit[i+1])do im:drawPixel(x,y,c)end end
 end
end
for i=1,#centers do
 local frame=i==1 and sprite.frames[1]or sprite:newEmptyFrame()
 frame.duration=times[i]/1000
 local input=app.open(source..string.format('frame_%03d.png',i==9 and 0 or i-1))
 local body=Image(104,104,ColorMode.RGB);body:drawSprite(input,1);input:close()
 local box=regions[i]
 for y=box[2],box[4]do for x=box[1],box[3]do local c=Color(body:getPixel(x,y))
  if c.alpha>0 and ((c.blue>130 and c.green>115 and c.blue>c.red+8)or(c.red>190 and c.green>195 and c.blue>200))then body:drawPixel(x,y,0)end
 end end
 if i==1 then sprite.cels[1].image=body else sprite:newCel(sprite.layers[1],frame,body)end
 local crystal=Image(104,104,ColorMode.RGB);local cx,cy=centers[i][1],centers[i][2]
 local st=stems[i];local steps=math.max(math.abs(st[3]-st[1]),math.abs(st[4]-st[2]))
 for n=0,steps do local x=math.floor(st[1]+(st[3]-st[1])*n/steps+.5);local y=math.floor(st[2]+(st[4]-st[2])*n/steps+.5)
  crystal:drawPixel(x,y+1,col(57,34,31));crystal:drawPixel(x,y,col(130,85,54))
 end
 local points=(i>=3 and i<=7)and{{6,0},{3,-3},{-1,-4},{-4,-2},{-4,2},{-1,4},{3,3}}or{{0,-6},{3,-3},{4,1},{2,4},{-2,4},{-4,1},{-3,-3}}
 local pts={};for _,p in ipairs(points)do pts[#pts+1]={cx+p[1],cy+p[2]}end
 poly(crystal,pts,col(88,195,236))
 if i>=3 and i<=7 then poly(crystal,{{cx+3,cy},{cx+1,cy-2},{cx-2,cy-2},{cx-2,cy+2},{cx+1,cy+2}},col(220,250,253))
 else poly(crystal,{{cx,cy-4},{cx+2,cy-1},{cx+1,cy+2},{cx-2,cy+1},{cx-2,cy-1}},col(220,250,253))end
 crystal:drawPixel(cx,cy,col(248,246,244));sprite:newCel(crystalLayer,frame,crystal)
 local flat=Sprite(104,104,ColorMode.RGB);local image=Image(104,104,ColorMode.RGB);image:drawImage(body);image:drawImage(crystal);flat.cels[1].image=image
 flat:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));flat:close()
end
app.activeSprite=sprite;sprite:newTag(1,9).name='staff_attack';sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(root..'review/circe_staff_attack_east_clean.gif')
local contact=Sprite(1248,936,ColorMode.RGB);local image=contact.cels[1].image
for y=0,image.height-1 do for x=0,image.width-1 do image:drawPixel(x,y,col(13,23,37))end end
for i=1,9 do local flat=Image(104,104,ColorMode.RGB);flat:drawSprite(sprite,i);local ox=((i-1)%4)*312;local oy=math.floor((i-1)/4)*312
 for y=0,103 do for x=0,103 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,2 do for dx=0,2 do image:drawPixel(ox+x*3+dx,oy+y*3+dy,c)end end end end end
end
contact:saveCopyAs(root..'review/circe_staff_attack_east_clean_3x.png');contact:close();sprite:close()
