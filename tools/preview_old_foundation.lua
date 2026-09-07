local root='C:/Users/Life/Desktop/Testnew2/NeonGothic/v082/'
local source=Sprite{fromFile='C:/Users/Life/Documents/ChatGPT/Skeleton Defender/Assets/Resources/NeonGothic/cemetery_battlefield.png'}
local im=Image(128,90,ColorMode.RGB);im:drawSprite(source,1,Point(-184,-230));source:close()
im:resize{width=768,height=540};im:saveAs(root..'review/old-site6-before-6x.png')
