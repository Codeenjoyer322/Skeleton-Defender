"""Build a local review gallery from accepted assets only; never edits raster art."""
from pathlib import Path
import argparse, html, json, os
from PIL import Image

parser=argparse.ArgumentParser()
parser.add_argument('--root', default='C:/Users/Life/Desktop/Testnew2/NeonGothic')
args=parser.parse_args()
root=Path(args.root); review=root/'review'; review.mkdir(parents=True,exist_ok=True)
def rel(path): return Path(os.path.relpath(path,review)).as_posix()
def link(path): return rel(path) if path and Path(path).is_file() else None
def first(directory, pattern): return next(Path(directory).glob(pattern),None)
roles={'achilles':'Ахилл','circe':'Цирцея','normal':'Обычный скелет','ninja':'Ниндзя',
 'tutankhamun':'Тутанхамон','giant':'Гигант','crawler':'Скелет без ног','pirate':'Пират',
 'samurai':'Самурай','sarcophagus':'Саркофаг','sarcophagus_bare':'После саркофага',
 'trex':'T-Rex','knight':'Рыцарь','warlock':'Колдун','mad':'Безумный','boxer':'Боксёр',
 'boss':'Финальный босс','mini_mummy':'Мини-мумия','blue_frog':'Синяя жаба'}
actions={'idle':'Ожидание','walk':'Ходьба','attack':'Атака','sword_attack':'Удар мечом',
 'staff_attack':'Атака посохом','bite':'Укус','shoot':'Выстрел','cast':'Заклинание',
 'pistol_shot':'Выстрел из пистолета','lightning_cast':'Молния','cast_fail':'Срыв заклинания',
 'summon':'Призыв','execution':'Казнь','execution_bite':'Смертельный укус'}
directions={'south':'Юг','south-east':'Юго-восток','east':'Восток','north-east':'Северо-восток',
 'north':'Север','north-west':'Северо-запад','west':'Запад','south-west':'Юго-запад'}
manifest_path=review/'accepted-manifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig')) if manifest_path.exists() else {'clips':[]}
provenance_path=root/'exports'/'provenance.json'
provenance=json.loads(provenance_path.read_text(encoding='utf-8-sig')) if provenance_path.exists() else []
by_gif={str(Path(p['gif']).resolve()):p for p in provenance if p.get('gif')}
clips=[]
for c in manifest['clips']:
 if not c.get('approved'): continue
 directory=root/'exports'/'actors'/c['role']/c['action']/c['direction']
 gif=directory/'animation.gif'
 if not gif.exists(): continue
 exported=by_gif.get(str(gif.resolve()),{})
 native=Path(exported['native']) if exported.get('native') else first(directory,'*.aseprite')
 if not native:
  native=first(root/'source'/'actors'/c['role']/c['action']/c['direction'],'*.aseprite')
 still=directory/'still.png' if (directory/'still.png').exists() else first(directory,'frame*.png')
 if not still:
  input_paths=[p.get('path') if isinstance(p,dict) else p for p in c.get('inputs',[])]
  still=next((Path(p) for p in input_paths if p and str(p).lower().endswith('.png') and Path(p).exists()),None)
 with Image.open(gif) as gif_image: preview_width,preview_height=gif_image.size
 clips.append({'role':c['role'],'name':roles.get(c['role'],c['role']),
  'action':c['action'],'actionName':actions.get(c['action'],c['action']),
  'filterAction':'attack' if c['action'] in ('attack','staff_attack','sword_attack') else c['action'],
  'filterActionName':'Обычная атака' if c['action'] in ('attack','staff_attack','sword_attack') else actions.get(c['action'],c['action']),
  'direction':c['direction'],'directionName':directions.get(c['direction'],c['direction']),
  'gif':rel(gif),'still':link(still),'native':link(native),'width':preview_width,
  'height':preview_height,'sheet':link(Path(exported['sheet']) if exported.get('sheet') else directory/'animation.png'),'events':c.get('events',[])})
art=[]
for name,title in [('moonlit_courtyard_menu','Главное меню'),('cemetery_battlefield','Skeleton Cemetry'),
 ('portrait_arch','Рамка портрета'),('building_pad','Площадка башни')]:
 p=root/'exports'/'environment'/(name+'.png')
 art.append({'title':title,'image':link(p),'native':link(root/'source'/'environment'/(name+'.aseprite'))})
for kind,title in enumerate(['Стрелковая','Огненная','Ледяная']):
 for level in range(1,4):
  name=f'tower_{kind}_{level}'
  art.append({'title':f'{title} · {level} уровень','image':link(root/'exports'/'environment'/(name+'.png')),
   'native':link(root/'source'/'environment'/(name+'.aseprite'))})
data={'clips':clips,'art':art,'animatedMenu':link(review/'moonlit_courtyard_animated.gif'),
 'iconContact':link(review/'inventory-icons-contact.png'),'count':len(clips)}
template='''<!doctype html><html lang="ru"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Skeleton Defender · просмотр графики</title>
<style>
[hidden]{display:none!important}
:root{color-scheme:dark;--ink:#0b1020;--panel:#141e32;--edge:#30465f;--cyan:#63eee3;--pink:#ed75bb;--muted:#a0b1c9;--zoom:2}
*{box-sizing:border-box}body{margin:0;background:radial-gradient(ellipse at 85% 0,#21304f 0,transparent 50%),var(--ink);color:#e4edf6;font:16px/1.5 system-ui,sans-serif}header,main,footer{max-width:1392px;margin:auto;padding:36px 32px}header{border-bottom:1px solid var(--edge)}.eyebrow{color:var(--cyan);letter-spacing:.16em;font-size:12px}h1{font-size:clamp(30px,4vw,50px);line-height:1.12;margin:16px 0}h2{font-size:26px;margin:0 0 12px}p{color:var(--muted);max-width:900px}a{color:var(--cyan);text-underline-offset:3px}.nav{display:flex;gap:26px;flex-wrap:wrap;margin-top:24px}.nav a{text-decoration:none}.stats{display:flex;gap:24px;margin-top:24px;color:#f2c184}.section{padding:24px 0 52px}.artGrid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}.artCard,.clip{background:var(--panel);border:1px solid var(--edge);border-top-color:#427b8e;border-bottom-color:#64446b;border-radius:6px;overflow:hidden}.artCard .picture{min-height:160px;background:#0c1424;display:flex;align-items:center;justify-content:center}.artCard img{display:block;max-width:100%;width:auto;height:auto;image-rendering:pixelated}.artCard.wide{grid-column:1/-1}.artCard.wide img{width:100%;max-width:1056px}.artCard.small .picture{height:200px}.artCard.small img{height:192px;object-fit:contain}.caption{padding:16px 20px;display:flex;gap:18px;align-items:center;justify-content:space-between}.caption span{font-size:12px;color:var(--muted)}.controls{display:flex;align-items:center;flex-wrap:wrap;gap:12px;padding:18px;background:var(--panel);border:1px solid var(--edge);border-radius:6px;margin:22px 0}.controls label{display:flex;gap:8px;align-items:center;color:var(--muted);font-size:13px}button,select,input{font:inherit;accent-color:var(--cyan)}select,button,input[type=search]{color:#e4edf6;border:1px solid #46617d;background:#101a2d;padding:10px 12px;border-radius:4px}button{cursor:pointer}button:hover{border-color:var(--cyan)}button.active{background:#397775;color:white}.clips{display:grid;grid-template-columns:repeat(auto-fill,minmax(275px,1fr));gap:16px}.stage{min-height:310px;display:flex;justify-content:center;align-items:center;overflow:auto;background-color:#0a111f;background-image:linear-gradient(45deg,#122034 25%,transparent 25%,transparent 75%,#122034 75%),linear-gradient(45deg,#122034 25%,transparent 25%,transparent 75%,#122034 75%);background-position:0 0,12px 12px;background-size:24px 24px}.stage img{width:calc(var(--w)*var(--zoom)*1px);height:auto;image-rendering:pixelated;flex-shrink:0}.clip .caption{align-items:start;flex-direction:column;gap:6px}.links{display:flex;gap:18px;font-size:12px}.muted{color:var(--muted)}#more{display:block;margin:26px auto}.empty{padding:32px;border:1px dashed var(--edge);color:var(--muted)}footer{border-top:1px solid var(--edge);font-size:13px;color:var(--muted)}@media(max-width:720px){header,main,footer{padding:24px 16px}.artGrid{grid-template-columns:1fr}.stats{font-size:13px;gap:16px}.controls{align-items:stretch}.controls select{max-width:100%}}
</style>
<header><div class="eyebrow">SKELETON DEFENDER / NEON GOTHIC</div><h1>Графика для просмотра</h1><p>Камень цвета ночи, холодное свечение магии и тёплые фонари. Здесь можно рассмотреть рисунки и проверенные анимации отдельно от боя.</p><nav class="nav"><a href="#environment">Окружение и башни</a><a href="#inventory">Инвентарь</a><a href="#actors">Персонажи и враги</a></nav><div class="stats"><span id="count"></span><span>PNG · GIF · Aseprite</span><span>Без подключения к интернету</span></div></header>
<main><section class="section" id="environment"><h2>Окружение и башни</h2><p>Архитектура, дорога и освещение сохранены слоями в Aseprite. Для каждой башни подготовлены три уровня.</p><div id="art" class="artGrid"></div></section>
<section class="section" id="inventory"><h2>Снаряжение</h2><p>Отдельные иконки шести частей брони, оружия и трёх артефактов.</p><div class="artCard wide"><img id="icons" style="display:block;width:100%;image-rendering:pixelated" alt="Иконки инвентаря"><div class="caption"><a href="../source/icons/">Исходники иконок в Aseprite</a></div></div></section>
<section class="section" id="actors"><h2>Анимации по направлениям</h2><p>Выбери персонажа и действие. На клетчатом фоне видны границы оружия и прозрачность; увеличение сохраняет чёткие пиксели.</p><div class="controls"><select id="role" aria-label="Персонаж"><option value="">Все персонажи</option></select><select id="action" aria-label="Действие"><option value="">Все действия</option></select><select id="direction" aria-label="Направление"><option value="">Все направления</option></select><label>Масштаб <input type="range" id="zoom" min="1" max="4" step=".5" value="2"><output id="zoomText">2×</output></label><button id="motion">Остановить анимации</button></div><p id="shown"></p><div id="clips" class="clips"></div><button id="more">Показать ещё 24</button></section></main>
<footer>Рабочая папка: Testnew2 / NeonGothic. В эту галерею включаются только принятые клипы; отклонённые варианты хранятся отдельно в raw. Ссылки на исходники появляются после их экспорта.</footer>
<script>const data=__DATA__;
const $=id=>document.getElementById(id);const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
$('count').textContent=data.count+' принятых клипов';$('icons').src=data.iconContact||'';
$('art').innerHTML=data.art.filter(a=>a.image).map((a,i)=>`<article class="artCard ${i<2?'wide':'small'}"><div class="picture"><img loading="lazy" src="${esc(i===0&&data.animatedMenu?data.animatedMenu:a.image)}" alt="${esc(a.title)}"></div><div class="caption"><b>${esc(a.title)}</b><div class="links"><a href="${esc(a.image)}">PNG</a>${a.native?`<a href="${esc(a.native)}">Aseprite</a>`:''}</div></div></article>`).join('');
for(const [id,key,label] of [['role','role','name'],['action','filterAction','filterActionName'],['direction','direction','directionName']]){const map=new Map(data.clips.map(c=>[c[key],c[label]]));for(const [v,n] of map){const o=document.createElement('option');o.value=v;o.textContent=n;$(id).append(o)}}
let limit=24,moving=true;
function render(){let clips=data.clips.filter(c=>(!$('role').value||c.role===$('role').value)&&(!$('action').value||c.filterAction===$('action').value)&&(!$('direction').value||c.direction===$('direction').value));$('shown').textContent=clips.length?`Показано ${Math.min(limit,clips.length)} из ${clips.length}`:'В этой выборке пока нет экспортированных клипов.';$('more').hidden=limit>=clips.length;$('clips').innerHTML=clips.slice(0,limit).map(c=>`<article class="clip"><div class="stage"><img loading="lazy" style="--w:${c.width||128}" src="${esc(!moving&&c.still?c.still:c.gif)}" alt="${esc(c.name+' · '+c.actionName+' · '+c.directionName)}"></div><div class="caption"><b>${esc(c.name)}</b><span>${esc(c.actionName)} / ${esc(c.directionName)}</span><div class="links"><a href="${esc(c.gif)}">GIF</a>${c.sheet?`<a href="${esc(c.sheet)}">PNG</a>`:''}${c.native?`<a href="${esc(c.native)}">Aseprite</a>`:''}</div></div></article>`).join('')}
['role','action','direction'].forEach(id=>$(id).addEventListener('change',()=>{limit=24;render()}));$('more').onclick=()=>{limit+=24;render()};$('zoom').oninput=()=>{document.documentElement.style.setProperty('--zoom',$('zoom').value);$('zoomText').value=$('zoom').value+'×'};$('motion').onclick=()=>{moving=!moving;$('motion').textContent=moving?'Остановить анимации':'Включить анимации';render()};render();
</script></html>'''
payload=json.dumps(data,ensure_ascii=False).replace('<','\\u003c')
(review/'index.html').write_text(template.replace('__DATA__',payload),encoding='utf-8')
(review/'gallery-data.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'gallery':str(review/'index.html'),'acceptedClips':len(clips),'environmentAssets':len(art)},ensure_ascii=False))
