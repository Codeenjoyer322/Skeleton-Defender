local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local f=io.open(root..'review/manifests/achilles-attacks-seven-qa.json','r');local clips=json.decode(f:read('*a')).clips;f:close()
local function color(r,g,b)return Color{r=r,g=g,b=b,a=255}end
for part=0,1 do
 local rows=part==0 and 4 or 3;local sprite=Sprite(1024,rows*256,ColorMode.RGB);local image=sprite.cels[1].image
 for y=0,image.height-1 do for x=0,image.width-1 do image:drawPixel(x,y,color(13,23,37))end end
 for row=0,rows-1 do local clip=clips[part*4+row+1];local indices={0,2,clip.events[1].frameIndex,11}
  for column,index in ipairs(indices)do local input=clip.inputs[index+1];local source=app.open(input.file);local flat=Image(source.width,source.height,ColorMode.RGB);flat:drawSprite(source,1);source:close()
   local ox=(column-1)*256;local oy=row*256
   for y=0,flat.height-1 do for x=0,flat.width-1 do local c=Color(flat:getPixel(x,y));if c.alpha>0 then for dy=0,1 do for dx=0,1 do image:drawPixel(ox+(x+input.offsetX)*2+dx,oy+(y+input.offsetY)*2+dy,c)end end end end end
   for _,socket in ipairs(clip.sockets)do if socket.frameIndex==index then
    local cx,cy=ox+socket.position.x*2,oy+socket.position.y*2;local c=socket.name=='blade_tip' and color(50,240,255)or color(255,50,210)
    for d=-4,4 do image:drawPixel(cx+d,cy,c);image:drawPixel(cx,cy+d,c)end
   end end
  end
 end
 sprite:saveCopyAs(root..'review/qa_achilles_hand_sockets_part'..(part+1)..'.png');sprite:close()
end
