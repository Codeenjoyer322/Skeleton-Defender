-- Remove baked foundations so Unity can anchor each single base precisely.
local v='C:/Users/Life/Desktop/Testnew2/NeonGothic/v081/'
local base=app.open(v..'source/environment/cemetery_battlefield.aseprite')
local flat=Image(base.spec);flat:drawSprite(base,1)
local sp=Sprite(512,320,ColorMode.RGB);sp.cels[1].image:drawImage(flat,Point(0,0))
sp:saveCopyAs(v..'source/environment/foundation_cleanup_input.png')
local mask=Sprite(512,320,ColorMode.RGB);local im=mask.cels[1].image
local centers={{15,129},{118,109},{118,225},{234,136},{235,212},{357,121},{242,288},{365,288},{470,209},{460,103}}
for y=0,319 do for x=0,511 do
 local edit=false;for _,p in ipairs(centers) do if math.abs(x-p[1])/28+math.abs(y-p[2])/17<=1 then edit=true;break end end
 im:drawPixel(x,y,Color{r=edit and 255 or 0,g=edit and 255 or 0,b=edit and 255 or 0,a=255})
end end
mask:saveCopyAs(v..'source/environment/foundation_cleanup_mask.png')
mask:close();sp:close();base:close()
