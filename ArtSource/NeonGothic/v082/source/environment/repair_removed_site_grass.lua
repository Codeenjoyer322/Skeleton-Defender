-- Copy nearby cemetery grass on a new native layer; no generated art or raster edits outside Aseprite.
local project='C:/Users/Life/Documents/ChatGPT/Skeleton Defender/'
local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/v082/'
app.fs.makeAllDirectories(root..'source/environment');app.fs.makeAllDirectories(root..'exports/environment');app.fs.makeAllDirectories(root..'review')
-- Always start from the preserved baseline so rerunning this recipe never accumulates edits.
local input=Sprite{fromFile='C:/Users/Life/Desktop/Testnew2/NeonGothic/v081/exports/environment/cemetery_battlefield.png'}
local original=Image(input.spec);original:drawSprite(input,1);input:close()
local sprite=Sprite{fromFile='C:/Users/Life/Desktop/Testnew2/NeonGothic/v081/source/environment/cemetery_battlefield.aseprite'}
local prior=Image(sprite.spec);prior:drawSprite(sprite,1)
local mismatch=0
for y=0,319 do for x=0,527 do if prior:getPixel(x,y)~=original:getPixel(x,y)then mismatch=mismatch+1 end end end
-- Keep the earlier editable layers. A visible latest-map layer preserves any later refinements.
if mismatch>0 then
 local latest=sprite:newLayer();latest.name='Latest runtime background preserved before site6 cleanup';sprite:newCel(latest,1,Image(original),Point(0,0))
end
local patch=Image(528,320,ColorMode.RGB);local changed=0
for y=252,308 do for x=211,279 do
 local dx=(x-245)/28;local dy=(y-280)/23;local radius=math.sqrt(dx*dx+dy*dy)
 local angle=math.atan(dy,dx)
 local contour=1+.075*math.sin(angle*7+.7)+.055*math.sin(angle*13+1.2)
 -- A ragged organic perimeter follows clumps instead of leaving a new rectangular stamp.
 local fringe=((x*37+y*19+math.floor(x/3)*11)%23)/23
 if radius<contour-.12 or radius<contour and fringe<(contour-radius)/.12 then
  local sx=x-8;local sy=y-45
  local p=original:getPixel(sx,sy)
  patch:drawPixel(x,y,p);changed=changed+1
 end
end end
local layer=sprite:newLayer();layer.name='Removed site6 - nearby cemetery grass, irregular clump edge';sprite:newCel(layer,1,patch,Point(0,0))
sprite:saveAs(root..'source/environment/cemetery_battlefield.aseprite')
local result=Image(528,320,ColorMode.RGB);result:drawSprite(sprite,1)
result:saveAs(root..'exports/environment/cemetery_battlefield.png')
result:saveAs(project..'Assets/Resources/NeonGothic/cemetery_battlefield.png')
local before=Image(128,90,ColorMode.RGB);before:drawImage(original,Point(-184,-230))
local after=Image(128,90,ColorMode.RGB);after:drawImage(result,Point(-184,-230))
before:saveAs(root..'review/old-site6-before.png');after:saveAs(root..'review/old-site6-after.png')
before:resize{width=768,height=540};after:resize{width=768,height=540}
before:saveAs(root..'review/old-site6-before-6x.png');after:saveAs(root..'review/old-site6-after-6x.png')
local out=assert(io.open(root..'source/environment/removed_site6_provenance.json','w'))
out:write(json.encode({targetCenter={x=245,y=280},sampleOffset={x=-8,y=-45},maximumWriteBounds={x=211,y=252,width=69,height=57},pixelCount=changed,previousEditableDifferencePixels=mismatch,recipe='repair_removed_site_grass.lua',method='Native Aseprite copy from adjacent cemetery grass on separate layer'}));out:close();sprite:close()
