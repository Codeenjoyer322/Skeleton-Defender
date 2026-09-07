local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
app.fs.makeAllDirectories(root..'v082/review')
local function load(path)
 local sp=Sprite{fromFile=path};local im=Image(sp.spec)
 for _,c in ipairs(sp.cels)do if c.frame.frameNumber==1 then im:drawImage(c.image,c.position)end end
 sp:close();return im
end
local sheet=Image(144*3,64*3,ColorMode.RGB)
for kind=0,2 do for level=1,3 do
 local im=load(root..'v081/exports/environment/tower_'..kind..'_'..level..'.png')
 for y=128,191 do for x=0,143 do sheet:drawPixel((level-1)*144+x,kind*64+y-128,im:getPixel(x,y))end end
end end
sheet:resize{width=1296,height=576};sheet:saveAs(root..'v082/review/tower-bases-before-3x.png')
local archer=load(root..'v081/exports/environment/tower_0_2.png');archer:resize{width=576,height=768};archer:saveAs(root..'v082/review/archer-II-before-4x.png')
