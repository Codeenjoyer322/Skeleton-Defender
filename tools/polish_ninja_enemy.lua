local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=assert(io.open(root..'source/qc-inputs/ninja.json','r'));local clips=json.decode(f:read('*a'));f:close()
local dirs={south=0,['south-east']=1,east=2,['north-east']=3,north=4,['north-west']=5,west=6,['south-west']=7}
local contacts={walk=Image(128*8,128*8,ColorMode.RGB),attack=Image(128*8,128*8,ColorMode.RGB)}
local clear=app.pixelColor.rgba(11,10,11,0)
local function clean(im)
 -- All approved ninja heads begin at y34 or below; remove unsupported floating punctuation.
 for y=0,32 do for x=0,127 do im:drawPixel(x,y,clear) end end
end
local function southPunch(base,index)
 local im=Image(base);if index==1 or index==8 then return im end
 for y=67,92 do for x=37,51 do im:drawPixel(x,y,clear) end end
 local function rect(x,y,w,h,p)for yy=y,y+h-1 do for xx=x,x+w-1 do im:drawPixel(xx,yy,p)end end end
 local ink=app.pixelColor.rgba(25,22,31,255);local bone=app.pixelColor.rgba(237,234,207,255);local shade=app.pixelColor.rgba(167,158,144,255)
 local function line(x,y,xx,yy,w,p)local n=math.max(math.abs(xx-x),math.abs(yy-y),1);for k=0,n do rect(math.floor(x+(xx-x)*k/n-w/2+.5),math.floor(y+(yy-y)*k/n-w/2+.5),w,w,p)end end
 local poses={{},{41,74,50,69},{43,80,52,87},{45,82,56,94},{45,82,56,94},{43,81,49,88},{43,80,45,85}}
 local p=poses[index];line(48,66,p[1],p[2],4,ink);line(p[1],p[2],p[3],p[4],4,ink);line(48,66,p[1],p[2],2,bone);line(p[1],p[2],p[3],p[4],2,shade)
 local size=(index==4 or index==5) and 5 or 4;rect(p[3]-2,p[4]-2,size+2,size+2,ink);rect(p[3]-1,p[4]-1,size,size,bone);rect(p[3]-1,p[4]+size-2,size,1,shade);im:drawPixel(p[3]+1,p[4]+1,shade)
 return im
end
for _,clip in ipairs(clips)do
 local output=root..'source/actors/enemies/ninja/'..clip.action..'/'..clip.direction..'/'
 app.fs.makeAllDirectories(output);local sprite=Sprite(128,128,ColorMode.RGB);sprite.layers[1].name='PixelLab ninja motion - native Aseprite cleanup';local base
 for index,file in ipairs(clip.files)do
  local input=app.open(file);local im=Image(128,128,ColorMode.RGB)
  for _,cel in ipairs(input.cels)do if cel.frame.frameNumber==1 then im:drawImage(cel.image,cel.position)end end
  clean(im)
  if clip.action=='attack'and clip.direction=='south' then if not base then base=Image(im)end;im=southPunch(base,index)end
  im:saveAs(output..string.format('frame_%03d.png',index-1))
  local frame=index==1 and sprite.frames[1]or sprite:newEmptyFrame();frame.duration=.1
  if index==1 then sprite.cels[1].image=im else sprite:newCel(sprite.layers[1],index,im,Point(0,0))end
  contacts[clip.action]:drawImage(im,Point((index-1)*128,dirs[clip.direction]*128));input:close()
 end
 sprite:saveAs(output..'review.aseprite');sprite:saveCopyAs(output..'review.gif');sprite:close()
end
for action,im in pairs(contacts)do im:saveAs(root..'review/ninja-'..action..'-8directions-clean.png')end
print('Ninja native cleanup complete for current available directions.')
