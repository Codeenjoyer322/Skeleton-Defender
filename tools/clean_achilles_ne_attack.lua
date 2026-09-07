-- Native weapon-only correction. Independently authored NE body/arm poses remain intact.
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local input=root..'raw/actors/achilles_attack_north_east_locked_v4/job_00/frames/'
local output=root..'source/actors/achilles_attack_north-east_clean/';app.fs.makeAllDirectories(output..'frames')
local indices={1,2,3,4,5,6,8,9,10,11,12,1}
local hilts={{61,54},{64,32},{60,29},{60,29},{60,29},{60,29},{60,30},{66,34},{61,54},{61,59},{59,63},{61,54}}
local tips={{79,38},{66,7},{37,7},{26,20},{26,20},{26,20},{29,12},{72,9},{86,62},{82,73},{75,82},{79,38}}
local clear={{62,34,81,52},{61,10,70,31},{56,16,61,22},nil,nil,nil,nil,{63,10,74,35},{62,36,81,52},{63,50,81,61},{60,56,80,65},{62,34,81,52}}
local times={60,60,60,60,60,60,70,70,100,100,100,100}
local function col(r,g,b)return Color{r=r,g=g,b=b,a=255}end
local function polygon(im,points,c)
 for y=0,91 do local yy=y+.25;local hit={}
  for i,p in ipairs(points)do local q=points[i%#points+1];if(p[2]<=yy and q[2]>yy)or(q[2]<=yy and p[2]>yy)then hit[#hit+1]=p[1]+(yy-p[2])*(q[1]-p[1])/(q[2]-p[2])end end
  table.sort(hit);for i=1,#hit-1,2 do for x=math.max(0,math.ceil(hit[i])),math.min(91,math.floor(hit[i+1]))do im:drawPixel(x,y,c)end end
 end
end
local function line(im,x1,y1,x2,y2,c)
 local steps=math.max(math.abs(x2-x1),math.abs(y2-y1));for i=0,steps do im:drawPixel(math.floor(x1+(x2-x1)*i/steps+.5),math.floor(y1+(y2-y1)*i/steps+.5),c)end
end
local sprite=Sprite(92,92,ColorMode.RGB);sprite.layers[1].name='Readable full length steel blade - behind own NE body'
local bodyLayer=sprite:newLayer();bodyLayer.name='Original independent NE body and arm animation'
local gripLayer=sprite:newLayer();gripLayer.name='Gold crossguard at actual sword hand'
for i,sourceIndex in ipairs(indices)do
 local frame=i==1 and sprite.frames[1] or sprite:newEmptyFrame();frame.duration=times[i]/1000
 local source=app.open(input..string.format('%03d.png',sourceIndex));local body=Image(92,92,ColorMode.RGB);body:drawSprite(source,1);source:close()
 local region=clear[i]
 if region then for y=region[2],region[4]do for x=region[1],region[3]do local c=Color(body:getPixel(x,y))
  if c.alpha>0 and math.abs(c.red-c.green)<30 and math.abs(c.green-c.blue)<30 then body:drawPixel(x,y,0)end
 end end end
 local blade=Image(92,92,ColorMode.RGB);local h,t=hilts[i],tips[i];local dx,dy=t[1]-h[1],t[2]-h[2];local length=math.sqrt(dx*dx+dy*dy);local nx,ny=-dy/length,dx/length
 local function p(along,across)return{h[1]+dx*along+nx*across,h[2]+dy*along+ny*across}end
 polygon(blade,{p(0,-2.5),p(.77,-2.5),p(1,0),p(.77,2.5),p(0,2.5)},col(25,31,38))
 polygon(blade,{p(.04,-1.5),p(.77,-1.5),p(.96,0),p(.77,1.5),p(.04,1.5)},col(168,185,194))
 polygon(blade,{p(.04,-1.3),p(.76,-1.3),p(.94,0),p(.04,0)},col(237,242,234))
 line(blade,math.floor(h[1]+.3),math.floor(h[2]+.3),math.floor(t[1]-dx*.08+.3),math.floor(t[2]-dy*.08+.3),col(211,222,226))
 local guard=Image(92,92,ColorMode.RGB);local a,b=p(0,-3),p(0,3)
 line(guard,math.floor(a[1]+.5),math.floor(a[2]+.5),math.floor(b[1]+.5),math.floor(b[2]+.5),col(176,108,25))
 guard:drawPixel(h[1],h[2],col(245,194,75))
 if i==1 then sprite.cels[1].image=blade else sprite:newCel(sprite.layers[1],frame,blade)end
 sprite:newCel(bodyLayer,frame,body);sprite:newCel(gripLayer,frame,guard)
 local flat=Sprite(92,92,ColorMode.RGB);local image=Image(92,92,ColorMode.RGB);image:drawImage(blade);image:drawImage(body);image:drawImage(guard);flat.cels[1].image=image
 flat:saveAs(output..'frames/'..string.format('frame_%03d.png',i-1));flat:close()
end
sprite:newTag(1,12).name='attack';sprite:saveAs(output..'animation.aseprite');sprite:saveCopyAs(root..'review/achilles_attack_north-east_clean.gif')
local mf=io.open(output..'native-cleanup.json','w');mf:write(json.encode({durationsMs=times,contactFrameIndex=8,size=92,originalSource=input,weaponHands=hilts,bladeTips=tips}));mf:close()
local contact=Sprite(736,552,ColorMode.RGB);local image=contact.cels[1].image
for y=0,551 do for x=0,735 do image:drawPixel(x,y,col(13,23,37))end end
for i=1,12 do local flat=Image(92,92,ColorMode.RGB);flat:drawSprite(sprite,i);local ox=((i-1)%4)*184;local oy=math.floor((i-1)/4)*184
 for y=0,91 do for x=0,91 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+x*2+dx,oy+y*2+dy,c)end end end end end
end
contact:saveCopyAs(root..'review/qa_achilles_attack_north-east_clean_2x.png');contact:close();sprite:close()
