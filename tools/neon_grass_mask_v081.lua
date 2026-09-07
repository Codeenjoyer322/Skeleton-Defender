local base='C:/Users/Life/Desktop/Testnew2/NeonGothic/v081/'
local f=io.open('C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/battlefield-layout.json','r')
local data=json.decode(f:read('*a'));f:close()
local sp=Sprite(528,320,ColorMode.RGB);sp.layers[1].name='Inpaint only cemetery grass - all architecture protected'
local im=sp.cels[1].image
local function nearRoad(x,y)
 for _,p in ipairs(data.path) do if (x-p.x/2)^2+(y-p.y/2)^2<19^2 then return true end end
 return false
end
for y=0,319 do for x=0,527 do
 local editable=(x>=38 and x<=430 and y>=82 and y<=266)
  or (x>=203 and x<=423 and y>=3 and y<=94)
  or (x>=431 and x<=519 and y>=157 and y<=275)
  or (x>=0 and x<=60 and y>=78 and y<=169)
  or (x>=195 and x<=376 and y>=264 and y<=297)
 if editable then
  if nearRoad(x,y) then editable=false end
  for _,p in ipairs(data.sites) do
   if math.abs(x-p.x/2)/28+math.abs(y-p.y/2)/17<1 then editable=false end
  end
  -- Preserve lamps/foreground grave silhouettes even inside broad ground regions.
  if (x>=380 and x<=405 and y>=250) or (x>=280 and x<=319 and y>=277)
   or (x>=330 and x<=370 and y>=285) then editable=false end
 end
 im:drawPixel(x,y,Color{r=editable and 255 or 0,g=editable and 255 or 0,b=editable and 255 or 0,a=255})
end end
sp:saveAs(base..'source/environment/grass_mask.aseprite')
sp:saveCopyAs(base..'source/environment/grass_mask.png');sp:close()
print('Grass-only mask saved at528x320; crop x8,width512 for inpaint.')
