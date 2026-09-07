local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=assert(io.open(root..'source/qc-inputs/knight.json'));local clips=json.decode(f:read('*a'));f:close()
local dirs={south=0,['south-east']=1,east=2,['north-east']=3,north=4,['north-west']=5,west=6,['south-west']=7}
local sheets={walk=Image(1024,1024,ColorMode.RGB),attack=Image(1024,1024,ColorMode.RGB)}
local clear=app.pixelColor.rgba(11,10,11,0)
local function c(r,g,b)return app.pixelColor.rgba(r,g,b,255)end
local ink=c(20,34,43);local steel=c(93,124,138);local edge=c(168,210,220);local shade=c(47,69,81);local bone=c(196,182,135)
local function loadImage(path)
 local sp=app.open(path);local im=Image(128,128,ColorMode.RGB)
 for _,cel in ipairs(sp.cels)do if cel.frame.frameNumber==1 then im:drawImage(cel.image,cel.position)end end
 sp:close();return im
end
local function cleanWalk(im)
 -- Walking knights have no halos, arrows, fire or punctuation above the helmet.
 -- Remove connected overhead marks, preserving every row of the actual steel helmet.
 local seen={};local parts={}
 for y=0,127 do for x=0,127 do local key=y*128+x
  if not seen[key]and app.pixelColor.rgbaA(im:getPixel(x,y))>0 then
   local q={{x,y}};seen[key]=true;local head=1;local minY,maxY=y,y
   while head<=#q do local p=q[head];head=head+1;minY=math.min(minY,p[2]);maxY=math.max(maxY,p[2])
    for dy=-1,1 do for dx=-1,1 do local xx,yy=p[1]+dx,p[2]+dy;local k=yy*128+xx
     if xx>=0 and xx<128 and yy>=0 and yy<128 and not seen[k]and app.pixelColor.rgbaA(im:getPixel(xx,yy))>0 then seen[k]=true;q[#q+1]={xx,yy}end
    end end
   end
   parts[#parts+1]={p=q,minY=minY,maxY=maxY}
  end
 end end
 table.sort(parts,function(a,b)return #a.p>#b.p end)
 for i=2,#parts do local p=parts[i];if p.maxY<32 and #p.p<400 then for _,v in ipairs(p.p)do im:drawPixel(v[1],v[2],clear)end end end
 for y=0,48 do for x=0,127 do local p=im:getPixel(x,y);local r,g,b=app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p)
  if(r>235 and g>235 and b>230)or(r>220 and g<220 and b<100)or(b>220 and g>190 and r<100)then im:drawPixel(x,y,clear)end
 end end
 return im
end
local rig={
 south={s={47,62},arm={40,64,13,25},old={44,95,31,120},rest={43,87},ready={39,58},hit={51,83},aim={0,1}},
 ['south-east']={s={53,62},arm={43,65,13,25},old={49,95,54,120},rest={50,87},ready={48,56},hit={70,75},aim={.84,.55}},
 east={s={61,57},arm={57,62,13,26},old={66,91,85,114},rest={64,88},ready={62,47},hit={84,64},aim={1,.05}},
 ['north-east']={s={75,57},arm={76,61,12,29},old={79,90,98,117},rest={80,88},ready={74,45},hit={91,55},aim={.75,-.66}},
 north={s={79,58},arm={80,62,12,29},old={86,93,98,121},rest={84,90},ready={84,46},hit={82,51},aim={.08,-1}},
 ['north-west']={s={76,58},arm={75,65,10,25},old={78,91,85,121},rest={78,88},ready={79,47},hit={62,52},aim={-.75,-.66}},
 west={s={63,60},arm={65,66,8,18},old={53,99,47,114},rest={64,86},ready={67,48},hit={53,62},aim={-1,.05}},
 ['south-west']={s={52,61},arm={41,65,14,26},old={43,93,26,118},rest={44,89},ready={51,47},hit={42,79},aim={-.7,.72}}
}
local function rect(im,x,y,w,h,p)for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and xx<128 and yy>=0 and yy<128 then im:drawPixel(xx,yy,p)end end end end
local function line(im,x,y,xx,yy,w,p)local n=math.max(math.abs(xx-x),math.abs(yy-y),1);for k=0,n do rect(im,math.floor(x+(xx-x)*k/n-w/2+.5),math.floor(y+(yy-y)*k/n-w/2+.5),w,w,p)end end
local function sword(im,hx,hy,dx,dy)
 local len=math.sqrt(dx*dx+dy*dy);dx=dx/len;dy=dy/len;local nx,ny=-dy,dx
 local gx,gy=hx+dx*4,hy+dy*4
 line(im,hx-dx*4,hy-dy*4,gx,gy,3,ink);line(im,hx-dx*3,hy-dy*3,gx,gy,1,c(138,117,74))
 line(im,gx+dx*2,gy+dy*2,gx+dx*27,gy+dy*27,4,ink)
 line(im,gx+dx*2,gy+dy*2,gx+dx*26,gy+dy*26,2,steel)
 line(im,gx+dx*2+nx,gy+dy*2+ny,gx+dx*27,gy+dy*27,1,edge)
 line(im,gx-nx*5,gy-ny*5,gx+nx*5,gy+ny*5,3,ink);line(im,gx-nx*4,gy-ny*4,gx+nx*4,gy+ny*4,1,steel)
end
local function nativeSlash(base,d,index,lowerBody)
 if index==1 or index==8 then return Image(base)end
 local im=Image(base);local r=rig[d];local a=r.arm
 for y=a[2],a[2]+a[4]-1 do for x=a[1],a[1]+a[3]-1 do im:drawPixel(x,y,clear)end end
 line(im,r.old[1],r.old[2],r.old[3],r.old[4],9,clear)
 if d=='south'then line(im,36,90,49,94,5,clear)end
 if d=='south-east'then line(im,43,94,55,89,5,clear)end
 -- A generated raised-sword pose exposes intact legs behind the resting blade.
 -- Restore that real anatomy instead of erasing a diagonal strip through the shin.
 local legs={south={43,83},['south-east']={47,76},east={57,76},['north-east']={48,85},north={46,85},['north-west']={45,78},west={56,75},['south-west']={43,80}}
 for y=(d=='south'or d=='south-east')and 91 or 97,127 do for x=legs[d][1],legs[d][2]do im:drawPixel(x,y,lowerBody:getPixel(x,y))end end
 if d=='east'then
  for y=60,89 do for x=57,74 do im:drawPixel(x,y,lowerBody:getPixel(x,y))end end
 end
 -- Rebuild the visible near arm between its original shoulder and the moving gauntlet.
 local phase=({0,0,.12,.62,1,1.10,.22,0})[index]
 local hx=math.floor(r.ready[1]+(r.hit[1]-r.ready[1])*phase+.5)
 local hy=math.floor(r.ready[2]+(r.hit[2]-r.ready[2])*phase+.5)
 if index==7 then hx=math.floor((r.rest[1]+r.hit[1])*.5);hy=math.floor((r.rest[2]+r.hit[2])*.5)end
 local ex=math.floor((r.s[1]+hx)/2+.5);local ey=math.floor((r.s[2]+hy)/2+4*(1-math.min(phase,1))+.5)
 line(im,r.s[1],r.s[2],ex,ey,5,ink);line(im,r.s[1],r.s[2],ex,ey,3,bone)
 line(im,ex,ey,hx,hy,7,ink);line(im,ex,ey,hx,hy,5,steel);line(im,ex-1,ey-1,hx-1,hy-1,1,edge)
 rect(im,ex-2,ey-2,5,5,shade);rect(im,ex-1,ey-1,3,2,steel)
 local dx,dy
 if index<=3 and d=='north'then dx,dy=1,.4
 elseif index<=3 then dx,dy=-r.aim[1]*.5,-1
 elseif index==4 then dx,dy=r.aim[1],r.aim[2]-.65
 elseif index==5 then dx,dy=r.aim[1],r.aim[2]
 elseif index==6 then dx,dy=r.aim[1],r.aim[2]+.45
 else dx,dy=r.old[3]-r.old[1],r.old[4]-r.old[2]end
 sword(im,hx,hy,dx,dy)
 rect(im,hx-3,hy-2,6,5,ink);rect(im,hx-2,hy-1,4,3,steel);rect(im,hx-2,hy-1,3,1,edge)
 -- In the back views the helmet occludes the sword arm, as in the original artwork.
 if d=='north'or d=='north-west'then
  for y=24,47 do for x=51,76 do local p=base:getPixel(x,y);if app.pixelColor.rgbaA(p)>0 then im:drawPixel(x,y,p)end end end
 end
 return im
end
for _,clip in ipairs(clips)do
 local out=root..'source/actors/enemies/knight/'..clip.action..'/'..clip.direction..'/'
 app.fs.makeAllDirectories(out);local sp=Sprite(128,128,ColorMode.RGB);sp.layers[1].name='PixelLab directional knight - Aseprite complete sword stroke'
 local base=loadImage(root..'source/actors/knight_views_clean/'..clip.direction..'.png')
 local lowerBody=loadImage(clip.files[4])
 for index,path in ipairs(clip.files)do
  local im=clip.action=='attack'and nativeSlash(base,clip.direction,index,lowerBody)or cleanWalk(loadImage(path))
  if clip.action=='walk'and((clip.direction=='south-east'and index==2)or(clip.direction=='north-east'and index==2)or clip.direction=='west'or clip.direction=='north')then
   -- A few generated symbols had painted over the steel itself; deleting them alone
   -- left a notch. Restore the actual helmet crown from its approved native view.
   for y=0,31 do for x=0,127 do im:drawPixel(x,y,base:getPixel(x,y))end end
  end
  for y=0,127 do for x=0,127 do if app.pixelColor.rgbaA(im:getPixel(x,y))==0 then im:drawPixel(x,y,clear)end end end
  im:saveAs(out..string.format('frame_%03d.png',index-1))
  local frame=index==1 and sp.frames[1]or sp:newEmptyFrame();frame.duration=.1
  if index==1 then sp.cels[1].image=im else sp:newCel(sp.layers[1],index,im,Point(0,0))end
  sheets[clip.action]:drawImage(im,Point((index-1)*128,dirs[clip.direction]*128))
 end
 sp:saveAs(out..'review.aseprite');sp:saveCopyAs(out..'review.gif');sp:close()
end
for action,im in pairs(sheets)do im:saveAs(root..'review/knight-'..action..'-8directions-clean.png')end
print('Knight native directional sword strokes saved.')
