local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local role=assert(app.params.role);local action=assert(app.params.action);local direction=assert(app.params.direction)
local f=assert(io.open(root..'source/qc-inputs/'..role..'.json'));local clips=json.decode(f:read('*a'));f:close()
for _,clip in ipairs(clips)do if clip.action==action and clip.direction==direction then
 local sheet=Image(128*4,128*math.ceil(#clip.files/4),ColorMode.RGB)
 for i,path in ipairs(clip.files)do
  local sp=app.open(path);local frame=Image(128,128,ColorMode.RGB)
  for _,cel in ipairs(sp.cels)do if cel.frame.frameNumber==1 then frame:drawImage(cel.image,cel.position)end end
  sheet:drawImage(frame,Point(((i-1)%4)*128,math.floor((i-1)/4)*128));sp:close()
 end
 local big=Image(sheet.width*2,sheet.height*2,ColorMode.RGB)
 for y=0,big.height-1 do for x=0,big.width-1 do big:drawPixel(x,y,sheet:getPixel(math.floor(x/2),math.floor(y/2)))end end
 big:saveAs(root..'review/'..role..'-'..action..'-'..direction..'-focus.png')
end end
