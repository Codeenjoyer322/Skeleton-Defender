local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=assert(io.open(root..'source/qc-inputs/trex.json','r'));local clips=json.decode(f:read('*a'));f:close()
local dirs={south=0,['south-east']=1,east=2,['north-east']=3,north=4,['north-west']=5,west=6,['south-west']=7}
local contacts={walk=Image(1024,1024,ColorMode.RGB),attack=Image(1024,1024,ColorMode.RGB)}
local clear=app.pixelColor.rgba(11,10,11,0)
local function imageOf(path)
 local sp=app.open(path);local im=Image(128,128,ColorMode.RGB)
 for _,cel in ipairs(sp.cels)do if cel.frame.frameNumber==1 then im:drawImage(cel.image,cel.position)end end
 sp:close();return im
end
for _,clip in ipairs(clips)do
 local output=root..'source/actors/enemies/trex/'..clip.action..'/'..clip.direction..'/'
 app.fs.makeAllDirectories(output);local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='PixelLab skeleton tyrannosaur - native Aseprite view repair';local base
 for index,file in ipairs(clip.files)do
  local im=imageOf(file)
  if clip.action=='attack'and clip.direction=='north'then
   if not base then base=Image(im)end
   im=Image(base)
   -- The original generated motion turned to face the camera mid-bite.
   -- Keep the independently authored back skull and move neck/head toward the north target.
   local shift=({0,1,2,4,3,1,0,0})[index]
   for y=0,44 do for x=0,127 do im:drawPixel(x,y,clear)end end
   for y=0,44 do for x=0,127 do
    local dst=y-shift
    if dst>=0 then im:drawPixel(x,dst,base:getPixel(x,y))end
   end end
   for y=45-shift,44 do for x=0,127 do im:drawPixel(x,y,base:getPixel(x,44))end end
  end
  for y=0,127 do for x=0,127 do if app.pixelColor.rgbaA(im:getPixel(x,y))==0 then im:drawPixel(x,y,clear)end end end
  im:saveAs(output..string.format('frame_%03d.png',index-1))
  local frame=index==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=.1
  if index==1 then sprite.cels[1].image=im else sprite:newCel(sprite.layers[1],index,im,Point(0,0))end
  contacts[clip.action]:drawImage(im,Point((index-1)*128,dirs[clip.direction]*128))
 end
 sprite:saveAs(output..'review.aseprite');sprite:saveCopyAs(output..'review.gif');sprite:close()
end
for action,im in pairs(contacts)do im:saveAs(root..'review/trex-'..action..'-8directions-clean.png')end
print('Tyrannosaur 16 native directional clips saved.')
