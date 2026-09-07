-- PixelLab local inpaint, composited with its exact mask in native Aseprite.
local base='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local v=base..'v081/'
local sp=app.open(base..'source/environment/moonlit_courtyard_menu.aseprite')
local ps=app.open(v..'raw/jobs/menu_flat_paving_inpaint_v1/job_00/frames/000.png')
local ms=app.open(v..'source/menu/sigil_mask.png')
local patch=ps.cels[1].image;local mask=ms.cels[1].image
local layer=sp:newLayer();layer.name='Aseprite masked PixelLab inpaint - plain paving, no glowing sigil'
local out=sp:newCel(layer,1,Image(sp.width,sp.height,ColorMode.RGB),Point(0,0)).image
for y=0,mask.height-1 do for x=0,mask.width-1 do
 if app.pixelColor.rgbaR(mask:getPixel(x,y))>127 then out:drawPixel(x+336,y+256,patch:getPixel(x,y)) end
end end
sp:saveAs(v..'source/environment/moonlit_courtyard_menu.aseprite')
sp:saveCopyAs(v..'exports/environment/moonlit_courtyard_menu.png')
sp:saveCopyAs('C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/moonlit_courtyard_menu.png')
sp:close();ps:close();ms:close()
print('Original scene preserved outside exact inpaint mask.')
