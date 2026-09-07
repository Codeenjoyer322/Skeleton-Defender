local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=assert(io.open(root..'source/qc-inputs/normal.json','r'));local clips=json.decode(f:read('*a'));f:close()
local dirs={south=0,['south-east']=1,east=2,['north-east']=3,north=4,['north-west']=5,west=6,['south-west']=7}
local contacts={walk=Image(128*12,128*8,ColorMode.RGB),attack=Image(128*12,128*8,ColorMode.RGB)}
local function clean(im)
 local original=Image(im)
 -- Generated floating punctuation and white/yellow magic are not part of this unarmed skeleton.
 for y=0,127 do for x=0,127 do local p=im:getPixel(x,y)
  local r,g,b=app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p)
  if app.pixelColor.rgbaA(p)>0 and ((r>=240 and g>=240 and b>=220) or (r>245 and g>235 and b<105) or (r<6 and g<6 and b<6)) then
   local nearBone=false
   if b>=220 then for dy=-2,2 do for dx=-2,2 do local xx,yy=x+dx,y+dy
    if xx>=0 and yy>=0 and xx<128 and yy<128 then local n=original:getPixel(xx,yy);local nr,ng,nb=app.pixelColor.rgbaR(n),app.pixelColor.rgbaG(n),app.pixelColor.rgbaB(n)
     if app.pixelColor.rgbaA(n)>200 and nr>170 and ng>125 and nb>80 and nb<200 and nr>nb+20 then nearBone=true end
    end
   end end end
   im:drawPixel(x,y,nearBone and app.pixelColor.rgba(226,206,159,255) or 0)
  end
 end end
 local seen,components={},{}
 for y=0,127 do for x=0,127 do local key=y*128+x
  if not seen[key] and app.pixelColor.rgbaA(im:getPixel(x,y))>0 then
   local q={{x,y}};seen[key]=true;local head=1;local c={points={},minY=y,maxY=y}
   while head<=#q do local p=q[head];head=head+1;table.insert(c.points,p);c.minY=math.min(c.minY,p[2]);c.maxY=math.max(c.maxY,p[2])
    for dy=-1,1 do for dx=-1,1 do local xx,yy=p[1]+dx,p[2]+dy;local k=yy*128+xx
     if xx>=0 and yy>=0 and xx<128 and yy<128 and not seen[k] and app.pixelColor.rgbaA(im:getPixel(xx,yy))>0 then seen[k]=true;table.insert(q,{xx,yy}) end
    end end
   end
   table.insert(components,c)
  end
 end end
 table.sort(components,function(a,b)return #a.points>#b.points end)
 if #components>1 then for i=2,#components do local c=components[i]
  if c.maxY<components[1].minY+5 then for _,p in ipairs(c.points) do im:drawPixel(p[1],p[2],0) end end
 end end
 for y=0,30 do for x=0,127 do im:drawPixel(x,y,app.pixelColor.rgba(11,10,11,0)) end end
 for y=31,127 do for x=0,127 do local p=im:getPixel(x,y);if app.pixelColor.rgbaA(p)==0 then im:drawPixel(x,y,app.pixelColor.rgba(11,10,11,0)) end end end
end
local function southPunch(base,index)
 local im=Image(base)
 if index==1 or index==8 then return im end
 for y=68,94 do for x=77,91 do im:drawPixel(x,y,app.pixelColor.rgba(11,10,11,0)) end end
 local function rect(x,y,w,h,p) for yy=y,y+h-1 do for xx=x,x+w-1 do im:drawPixel(xx,yy,p) end end end
 local ink=app.pixelColor.rgba(27,18,14,255);local bone=app.pixelColor.rgba(226,206,159,255);local shade=app.pixelColor.rgba(171,139,91,255)
 local function line(x,y,xx,yy,w,p)local n=math.max(math.abs(xx-x),math.abs(yy-y),1);for k=0,n do rect(math.floor(x+(xx-x)*k/n-w/2+.5),math.floor(y+(yy-y)*k/n-w/2+.5),w,w,p)end end
 local poses={{},{85,73,82,67},{84,76,75,84},{81,81,72,93},{81,81,72,93},{83,79,78,87},{83,80,82,85}}
 local p=poses[index];local ex,ey,fx,fy=p[1],p[2],p[3],p[4]
 line(79,69,ex,ey,4,ink);line(ex,ey,fx,fy,4,ink);line(79,69,ex,ey,2,bone);line(ex,ey,fx,fy,2,shade)
 local size=(index==4 or index==5) and 5 or 4
 rect(fx-2,fy-2,size+2,size+2,ink);rect(fx-1,fy-1,size,size,bone);rect(fx-1,fy+size-2,size,1,shade)
 im:drawPixel(fx+1,fy+1,shade);return im
end
for _,clip in ipairs(clips)do
 local output=root..'source/actors/enemies/normal/'..clip.action..'/'..clip.direction..'/'
 local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='PixelLab motion with native Aseprite artifact cleanup'
 local southBase=nil
 for index,file in ipairs(clip.files)do
  local input=app.open(file);local im=Image(128,128,ColorMode.RGB)
  for _,cel in ipairs(input.cels)do if cel.frame.frameNumber==1 then im:drawImage(cel.image,cel.position)end end
  clean(im)
  if clip.action=='attack' and clip.direction=='south' then
   if not southBase then southBase=Image(im) end
   im=southPunch(southBase,index)
  end
  im:saveAs(output..string.format('frame_%03d.png',index-1))
  local frame=index==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=.1
  if index==1 then sprite.cels[1].image=im else sprite:newCel(sprite.layers[1],index,im,Point(0,0))end
  contacts[clip.action]:drawImage(im,Point((index-1)*128,dirs[clip.direction]*128));input:close()
 end
 sprite:saveAs(output..'review.aseprite');sprite:saveCopyAs(output..'review.gif');sprite:close()
end
for action,im in pairs(contacts)do im:saveAs(root..'review/normal-'..action..'-8directions-clean.png')end
local sp=app.open(root..'source/actors/enemies/normal/attack/south/frame_000.png');sp:resize{width=512,height=512};sp:saveCopyAs(root..'review/normal-south-body-4x.png');sp:close()
print('Normal skeleton punctuation and unsupported magic removed in Aseprite.')
