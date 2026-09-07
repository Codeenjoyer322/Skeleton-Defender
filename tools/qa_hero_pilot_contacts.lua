local base='C:/Users/Life/Desktop/Testnew2/NeonGothic/'
local jobs={
 {'circe_staff_attack','raw/actors/circe_8views_reference_v1/animations/staff_attack/east',8},
 {'achilles_attack','raw/actors/achilles_8views_reference_v2/animations/attack/east',8},
 {'achilles_overhead','raw/actors/achilles_8views_reference_v2/animations/overhead_cut/east',12},
 {'circe_walk','raw/actors/circe_walk_east_pilot/job_00/frames',4,true},
 {'achilles_walk','raw/actors/achilles_walk_east_pilot/job_00/frames',4,true}}
for _,job in ipairs(jobs) do
 local contact=Sprite(1248,math.ceil(job[3]/4)*312,ColorMode.RGB)
 contact.layers[1].name='Read-only QA grid - original pixels 3x'
 local out=contact.cels[1].image
 for y=0,out.height-1 do for x=0,out.width-1 do out:drawPixel(x,y,Color{r=13,g=23,b=37,a=255})end end
 for i=0,job[3]-1 do
  local path=base..job[2]..'/'..(job[4] and '' or 'frame_')..string.format('%03d.png',i)
  local source=app.open(path)
  local image=Image(source.width,source.height,ColorMode.RGB);image:drawSprite(source,1)
  local ox=(i%4)*312;local oy=math.floor(i/4)*312
  for y=0,image.height-1 do for x=0,image.width-1 do local c=Color(image:getPixel(x,y));if c.alpha>0 then
   for dy=0,2 do for dx=0,2 do out:drawPixel(ox+x*3+dx,oy+y*3+dy,c)end end
  end end end
  source:close()
 end
 app.activeSprite=contact
 contact:saveAs(base..'review/qa_'..job[1]..'_3x.aseprite')
 contact:saveCopyAs(base..'review/qa_'..job[1]..'_3x.png');contact:close()
end
