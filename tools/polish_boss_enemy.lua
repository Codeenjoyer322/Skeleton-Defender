local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=assert(io.open(root..'source/qc-inputs/boss.json','r'));local clips=json.decode(f:read('*a'));f:close()
local dirs={south=0,['south-east']=1,east=2,['north-east']=3,north=4,['north-west']=5,west=6,['south-west']=7}
local contacts={walk=Image(128*8,128*8,ColorMode.RGB),attack=Image(128*8,128*8,ColorMode.RGB)}
local clear=app.pixelColor.rgba(11,0,0,0)
local function rgba(r,g,b)return app.pixelColor.rgba(r,g,b,255)end
local function imageOf(path)
 local input=app.open(path);local im=Image(128,128,ColorMode.RGB)
 for _,cel in ipairs(input.cels)do if cel.frame.frameNumber==1 then im:drawImage(cel.image,cel.position)end end
 input:close();return im
end
local function clean(im,dir)
 local reference=imageOf(root..'source/actors/boss_views_clean/'..dir..'.png');local minY=127
 for y=0,127 do for x=0,127 do if app.pixelColor.rgbaA(reference:getPixel(x,y))>200 then minY=math.min(minY,y) end end end
 for y=0,minY-3 do for x=0,127 do im:drawPixel(x,y,clear) end end
 -- Punctuation above the crown is generated markup, not the boss's ability.
 for y=math.max(0,minY-2),minY+3 do for x=0,127 do local p=im:getPixel(x,y)
  if math.min(app.pixelColor.rgbaR(p),app.pixelColor.rgbaG(p),app.pixelColor.rgbaB(p))>235 then im:drawPixel(x,y,clear) end
 end end
end
local function southPunch(base,index)
 local im=Image(base);if index==1 or index==8 then return im end
 local function rect(x,y,w,h,p)for yy=y,y+h-1 do for xx=x,x+w-1 do im:drawPixel(xx,yy,p)end end end
 local ink=rgba(28,19,18);local bone=rgba(223,202,147);local shade=rgba(162,132,81)
 -- Restore the cloak behind the moving arm; preserve its original torso and crown.
 for y=61,91 do for x=76,88 do im:drawPixel(x,y,clear) end end
 for y=64,92 do for x=76,math.min(85,77+math.floor((y-64)/4)) do im:drawPixel(x,y,x==76 and rgba(57,25,34) or rgba(94,42,53)) end end
 rect(74,59,6,5,rgba(72,56,30));rect(76,60,4,2,rgba(100,77,41))
 local function line(x,y,xx,yy,w,p)local n=math.max(math.abs(xx-x),math.abs(yy-y),1);for k=0,n do rect(math.floor(x+(xx-x)*k/n-w/2+.5),math.floor(y+(yy-y)*k/n-w/2+.5),w,w,p)end end
 local poses={{},{84,69,80,62},{82,75,75,82},{80,80,69,93},{80,80,69,93},{82,78,75,88},{81,76,81,81}}
 local p=poses[index];line(78,62,p[1],p[2],4,ink);line(p[1],p[2],p[3],p[4],4,ink);line(78,62,p[1],p[2],2,bone);line(p[1],p[2],p[3],p[4],2,shade)
 local size=(index==4 or index==5) and 5 or 4;rect(p[3]-2,p[4]-2,size+2,size+2,ink);rect(p[3]-1,p[4]-1,size,size,bone);rect(p[3]-1,p[4]+size-2,size,1,shade);im:drawPixel(p[3]+1,p[4]+1,shade)
 return im
end
local function directedPunch(base,dir,index)
 if dir=='south' then return southPunch(base,index)end
 local im=Image(base);if index==1 or index==8 then return im end
 local function rect(x,y,w,h,p)for yy=y,y+h-1 do for xx=x,x+w-1 do if xx>=0 and xx<128 and yy>=0 and yy<128 then im:drawPixel(xx,yy,p)end end end end
 local ink=rgba(28,19,18);local bone=rgba(223,202,147);local shade=rgba(162,132,81)
 local poses={
  ['south-east']={shoulder={54,60},erase={48,63,13,27},elbow={63,72},fist={81,82},back={48,65},cloak=true},
  ['north-east']={shoulder={75,51},erase={77,55,16,35},elbow={85,55},fist={94,44},back={80,66},fullErase=true},
  north={shoulder={83,55},erase={84,59,10,31},elbow={91,54},fist={86,38},back={88,69},fullErase=true},
  ['north-west']={shoulder={49,51},erase={33,55,18,35},elbow={39,55},fist={30,44},back={44,66},fullErase=true},
  west={shoulder={59,56},erase={48,60,17,27},elbow={44,57},fist={29,54},back={67,62}},
  ['south-west']={shoulder={75,60},erase={68,63,14,27},elbow={64,73},fist={45,83},back={83,65},cloak=true}
 }
 local p=poses[dir];if not p then return im end
 -- Replace only the visible near arm. The native body, face, crown and cloak retain their authored view.
 local r=p.erase
 for y=r[2],r[2]+r[4]-1 do for x=r[1],r[1]+r[3]-1 do
  local v=base:getPixel(x,y);local rr,gg,bb=app.pixelColor.rgbaR(v),app.pixelColor.rgbaG(v),app.pixelColor.rgbaB(v)
  if p.fullErase or(rr>115 and gg>90 and rr>gg and gg>bb*1.05)then
   im:drawPixel(x,y,p.cloak and rgba(71,31,41)or clear)
  end
 end end
 if dir=='west' then
  for y=63,88 do for x=48,63 do
   if x<57 then im:drawPixel(x,y,clear)
   elseif y<82 then im:drawPixel(x,y,rgba(60,47,25))
   elseif y<87 and x<62 then im:drawPixel(x,y,rgba(45,35,22))end
  end end
 end
 local t=({0,.12,.55,1,1,.60,.15,0})[index]
 local fx=math.floor(p.back[1]+(p.fist[1]-p.back[1])*t+.5);local fy=math.floor(p.back[2]+(p.fist[2]-p.back[2])*t+.5)
 local ex=math.floor((p.shoulder[1]+fx)/2+.5);local ey=math.floor((p.shoulder[2]+fy)/2+4*(1-t)+.5)
 local function line(x,y,xx,yy,w,c)local n=math.max(math.abs(xx-x),math.abs(yy-y),1);for k=0,n do rect(math.floor(x+(xx-x)*k/n-w/2+.5),math.floor(y+(yy-y)*k/n-w/2+.5),w,w,c)end end
 line(p.shoulder[1],p.shoulder[2],ex,ey,4,ink);line(ex,ey,fx,fy,4,ink)
 line(p.shoulder[1],p.shoulder[2],ex,ey,2,bone);line(ex,ey,fx,fy,2,shade)
 rect(fx-3,fy-3,7,6,ink);rect(fx-2,fy-2,5,4,bone);rect(fx-2,fy+1,5,1,shade)
 im:drawPixel(fx,fy,shade)
 return im
end
for _,clip in ipairs(clips)do
 local output=root..'source/actors/enemies/boss/'..clip.action..'/'..clip.direction..'/'
 app.fs.makeAllDirectories(output);local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='PixelLab royal skeleton - Aseprite direction and artifact repair';local base
 for index,file in ipairs(clip.files)do
  local im=imageOf(file);clean(im,clip.direction)
  if clip.action=='attack'and clip.direction~='east' then if not base then base=Image(im)end;im=directedPunch(base,clip.direction,index)end
  im:saveAs(output..string.format('frame_%03d.png',index-1))
  local frame=index==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=.1
  if index==1 then sprite.cels[1].image=im else sprite:newCel(sprite.layers[1],index,im,Point(0,0))end
  contacts[clip.action]:drawImage(im,Point((index-1)*128,dirs[clip.direction]*128))
 end
 sprite:saveAs(output..'review.aseprite');sprite:saveCopyAs(output..'review.gif');sprite:close()
end
for action,im in pairs(contacts)do im:saveAs(root..'review/boss-'..action..'-8directions-clean.png')end
print('Royal skeleton native Aseprite cleanup saved for currently available clips.')
