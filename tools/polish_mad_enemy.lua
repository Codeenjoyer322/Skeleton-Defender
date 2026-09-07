local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=assert(io.open(root..'source/qc-inputs/mad.json'));local clips=json.decode(f:read('*a'));f:close()
local dirs={south=0,['south-east']=1,east=2,['north-east']=3,north=4,['north-west']=5,west=6,['south-west']=7}
local sheets={walk=Image(128*12,128*8,ColorMode.RGB),attack=Image(128*12,128*8,ColorMode.RGB)}
local function clean(im)
 local original=Image(im)
 for y=0,127 do for x=0,127 do local v=im:getPixel(x,y);local r,g,b=app.pixelColor.rgbaR(v),app.pixelColor.rgbaG(v),app.pixelColor.rgbaB(v)
  if app.pixelColor.rgbaA(v)>0 and ((r>240 and g>240 and b>220) or (r>245 and g>235 and b<105)) then
   local bone=false
   for yy=math.max(0,y-1),math.min(127,y+1) do for xx=math.max(0,x-1),math.min(127,x+1) do local p=original:getPixel(xx,yy);local rr,gg,bb=app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p)
    if app.pixelColor.rgbaA(p)>0 and rr>170 and gg>120 and bb>75 and bb<200 and rr>bb+20 then bone=true end
   end end
   im:drawPixel(x,y,bone and y>=24 and app.pixelColor.rgba(226,206,159,255) or 0)
  end
 end end
 -- The moving south-east skull reaches y=27: preserve the full head through the bob.
 for y=0,23 do for x=0,127 do im:drawPixel(x,y,0) end end
 local seen={}
 for y=24,43 do for x=0,127 do local key=y*128+x
  if not seen[key]and app.pixelColor.rgbaA(im:getPixel(x,y))>0 then
   local q={{x,y}};local head=1;seen[key]=true;local maxY=y
   while head<=#q do local p=q[head];head=head+1;maxY=math.max(maxY,p[2])
    for dy=-1,1 do for dx=-1,1 do local xx,yy=p[1]+dx,p[2]+dy;local k=yy*128+xx
     if xx>=0 and xx<128 and yy>=0 and yy<128 and not seen[k]and app.pixelColor.rgbaA(im:getPixel(xx,yy))>0 then seen[k]=true;q[#q+1]={xx,yy}end
    end end
   end
   if #q<12 and maxY<44 then for _,p in ipairs(q)do im:drawPixel(p[1],p[2],0)end end
  end
 end end
end
local function southPunch(base,index)
 local im=Image(base)
 if index==1 or index==8 then return im end
 for y=43,93 do for x=77,93 do im:drawPixel(x,y,0) end end
 local ink=app.pixelColor.rgba(34,24,15,255);local bone=app.pixelColor.rgba(226,206,159,255);local shade=app.pixelColor.rgba(171,139,91,255)
 local function rect(x,y,w,h,p) for yy=y,y+h-1 do for xx=x,x+w-1 do im:drawPixel(xx,yy,p) end end end
 local function line(x,y,xx,yy,w,p) local n=math.max(math.abs(xx-x),math.abs(yy-y),1);for k=0,n do rect(math.floor(x+(xx-x)*k/n-w/2+.5),math.floor(y+(yy-y)*k/n-w/2+.5),w,w,p) end end
 local poses={{},{84,58,80,48},{83,65,76,64},{82,73,73,83},{81,77,72,90},{83,71,77,76},{84,62,81,55}}
 local p=poses[index];local ex,ey,fx,fy=p[1],p[2],p[3],p[4]
 line(79,68,ex,ey,4,ink);line(ex,ey,fx,fy,4,ink);line(79,68,ex,ey,2,bone);line(ex,ey,fx,fy,2,shade)
 local size=index==5 and 6 or ((index==4 or index==6) and 5 or 4)
 rect(fx-2,fy-2,size+2,size+2,ink);rect(fx-1,fy-1,size,size,bone);rect(fx-1,fy+size-2,size,1,shade);im:drawPixel(fx+1,fy+1,shade)
 return im
end
local function projectedPunch(base,dir,index)
 local poses={
  east={s={64,62},r={57,52,13,22},b={64,55},f={89,62},torso=true},
  ['north-east']={s={76,64},r={77,46,16,28},b={84,51},f={98,48}},
  north={s={78,65},r={80,45,15,29},b={87,52},f={79,38}},
  ['north-west']={s={49,64},r={33,45,18,29},b={42,51},f={27,48}},
  ['south-west']={s={53,66},r={41,48,17,28},b={47,54},f={33,86},torso=true}
 }
 local p=poses[dir];if not p or index==1 or index==8 then return Image(base)end
 local im=Image(base);local clear=app.pixelColor.rgba(11,10,11,0)
 local ink=app.pixelColor.rgba(34,24,15,255);local bone=app.pixelColor.rgba(226,206,159,255);local shade=app.pixelColor.rgba(171,139,91,255)
 local function rect(x,y,w,h,v)for yy=y,y+h-1 do for xx=x,x+w-1 do im:drawPixel(xx,yy,v)end end end
 for y=p.r[2],p.r[2]+p.r[4]-1 do for x=p.r[1],p.r[1]+p.r[3]-1 do
  local torso=p.torso and y>=61 and ((dir=='east'and x>=58)or(dir=='south-west'and x>=51))
  im:drawPixel(x,y,torso and app.pixelColor.rgba(73,52,30,255)or clear)
 end end
 if dir=='east' then for y=58,61 do for x=67,71 do im:drawPixel(x,y,base:getPixel(x,y))end end end
 local t=({0,.06,.45,1,1,.65,.20,0})[index]
 local fx=math.floor(p.b[1]+(p.f[1]-p.b[1])*t+.5);local fy=math.floor(p.b[2]+(p.f[2]-p.b[2])*t+.5)
 local ex=math.floor((p.s[1]+fx)/2+.5);local ey=math.floor((p.s[2]+fy)/2+5*(1-t)+.5)
 local function line(x,y,xx,yy,w,v)local n=math.max(math.abs(xx-x),math.abs(yy-y),1);for k=0,n do rect(math.floor(x+(xx-x)*k/n-w/2+.5),math.floor(y+(yy-y)*k/n-w/2+.5),w,w,v)end end
 line(p.s[1],p.s[2],ex,ey,4,ink);line(ex,ey,fx,fy,4,ink)
 line(p.s[1],p.s[2],ex,ey,2,bone);line(ex,ey,fx,fy,2,shade)
 rect(ex-1,ey-1,3,3,bone);rect(fx-3,fy-3,7,6,ink);rect(fx-2,fy-2,5,4,bone);rect(fx-2,fy+1,5,1,shade);im:drawPixel(fx,fy,shade)
 return im
end
for _,clip in ipairs(clips) do
 local output=root..'source/actors/enemies/mad/'..clip.action..'/'..clip.direction..'/'
 app.fs.makeAllDirectories(output)
 local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='PixelLab locked view; native punctuation cleanup and front-facing punch'
 local southBase=nil;local nativeBase=nil;local walkUpper=nil
 for index,file in ipairs(clip.files) do
  local input=Sprite{fromFile=file};local im=Image(128,128,ColorMode.RGB)
  for _,cel in ipairs(input.cels) do if cel.frame.frameNumber==1 then im:drawImage(cel.image,cel.position) end end
  clean(im)
  if clip.action=='walk'and clip.direction=='north-west'then
   if not walkUpper then walkUpper=Image(im)end
   local shift=({0,-1,0,1,0,-1,0,1})[index]
   for y=0,81 do for x=0,127 do im:drawPixel(x,y,0)end end
   for y=0,81 do for x=0,127 do local dst=y+shift;if dst>=0 and dst<=81 then im:drawPixel(x,dst,walkUpper:getPixel(x,y))end end end
   -- Replace the generated feet-in-place bounce with alternating planted and raised feet.
   for y=92,127 do for x=0,127 do im:drawPixel(x,y,0)end end
   local stride={{0,0},{-1,-1},{-2,-2},{-1,-1},{0,0},{1,1},{2,1},{1,0}}
   for side=0,1 do
    local pose=stride[((index-1+side*4)%8)+1]
    for y=92,127 do for x=side==0 and 34 or 63,side==0 and 62 or 94 do
     local xx,yy=x+pose[1],y+pose[2]
     if xx>=0 and xx<128 and yy>=0 and yy<128 then local p=walkUpper:getPixel(x,y);if app.pixelColor.rgbaA(p)>0 then im:drawPixel(xx,yy,p)end end
    end end
   end
  end
  if clip.action=='attack' and clip.direction=='south' then if not southBase then local src=Sprite{fromFile=root..'source/actors/mad_views_clean/south.png'};southBase=Image(128,128,ColorMode.RGB);southBase:drawImage(src.cels[1].image,src.cels[1].position);src:close();clean(southBase) end im=southPunch(southBase,index) end
  if clip.action=='attack' and clip.direction~='south'and clip.direction~='south-east'and clip.direction~='west'then
   if not nativeBase then nativeBase=Image(im)end
   im=projectedPunch(nativeBase,clip.direction,index)
  end
  for y=0,127 do for x=0,127 do if app.pixelColor.rgbaA(im:getPixel(x,y))==0 then im:drawPixel(x,y,app.pixelColor.rgba(11,10,11,0))end end end
  im:saveAs(output..string.format('frame_%03d.png',index-1))
  local frame=index==1 and sprite.frames[1] or sprite:newEmptyFrame();frame.duration=.1
  if index==1 then sprite.cels[1].image=im else sprite:newCel(sprite.layers[1],index,im,Point(0,0)) end
  sheets[clip.action]:drawImage(im,Point((index-1)*128,dirs[clip.direction]*128));input:close()
 end
 sprite:saveAs(output..'review.aseprite');sprite:saveAs(output..'review.gif');sprite:close()
end
for action,im in pairs(sheets) do im:saveAs(root..'review/mad-'..action..'-8directions-clean.png') end
