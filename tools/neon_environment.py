"""PixelLab environment jobs. Native Aseprite compositing follows generation.
Credentials are read only from the installed extension and never logged.
"""
import base64, datetime, json, pathlib, sys, urllib.request, urllib.error
ROOT = pathlib.Path(r'C:\Users\Life\Desktop\Testnew2\NeonGothic')
OUT = ROOT / 'raw' / 'environment'
CONFIG = pathlib.Path(r'C:\Users\Life\AppData\Roaming\Aseprite\extensions\PixelLab\package.json')
API = 'https://api.pixellab.ai/v2'
def read(p): return json.loads(pathlib.Path(p).read_text(encoding='utf-8-sig'))
def write(p, x): p.parent.mkdir(parents=True, exist_ok=True); p.write_text(json.dumps(x,ensure_ascii=False,indent=2),encoding='utf-8')
def request(path, payload=None):
    key = read(CONFIG)['secret']
    req = urllib.request.Request(API + path, data=None if payload is None else json.dumps(payload).encode(), headers={'Authorization':'Bearer '+key,'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(req, timeout=55) as res: return json.load(res)
    except urllib.error.HTTPError as e:
        try: detail=json.loads(e.read()).get('detail','')
        except Exception: detail='omitted'
        raise RuntimeError('HTTP '+str(e.code)+': '+str(detail)[:600]) from None
def ref(path, usage):
    # Reading dimensions and encoding the unchanged input is not image editing.
    from PIL import Image
    with Image.open(path) as im: w,h=im.size
    return {'image':{'base64':base64.b64encode(pathlib.Path(path).read_bytes()).decode()},'size':{'width':w,'height':h},'usage_description':usage}
def clean(x):
    if isinstance(x,dict): return {k:('<image saved>' if k=='base64' else clean(v)) for k,v in x.items()}
    if isinstance(x,list): return [clean(v) for v in x]
    return x
def submit(planpath):
    plan=read(planpath); folder=OUT/plan['id']; statefile=folder/'state.json'
    if statefile.exists(): raise RuntimeError('Submission already journalled; inspect existing state before retrying.')
    balance=request('/balance'); write(OUT/'balance_before_latest.json',balance)
    spent=sum(float((read(p).get('usage') or {}).get('generations',0)) for p in OUT.glob('*/state.json'))
    if spent + plan.get('reserve',40)>140: raise RuntimeError('Environment allocation would be exceeded.')
    if balance.get('subscription',{}).get('generations',0)<155.9+plan.get('reserve',40): raise RuntimeError('Project spending floor reached.')
    payload=plan['payload']
    if 'style_path' in plan: payload['style_image']=ref(plan['style_path'],'Match the stone, cyan/magenta and amber palette, strong pixel lighting, isometric perspective and crisp pixel clusters. Original new composition.')
    if 'reference_path' in plan: payload['reference_images']=[ref(plan['reference_path'],plan.get('reference_usage','Preserve layout and spatial arrangement.'))]
    state={'id':plan['id'],'status':'submitting','created':datetime.datetime.now(datetime.timezone.utc).isoformat()}
    write(statefile,state); write(folder/'plan.json',plan)
    try:
        r=request('/generate-image-v2',payload); state.update(clean(r))
    except Exception as e: state.update(status='submission_uncertain_or_rejected',error=str(e))
    write(statefile,state); print(json.dumps(state,ensure_ascii=False))
def poll():
    for p in OUT.glob('*/state.json'):
        s=read(p)
        if s['status'] in ['completed','failed','submission_uncertain_or_rejected']: continue
        r=request('/background-jobs/'+s['background_job_id']); s['status']=r.get('status'); last=r.get('last_response',{})
        if s['status']=='completed':
            imgs=last.get('images',[]) or ([last['image']] if 'image' in last else [])
            for i,im in enumerate(imgs):
                data=im.get('base64') if isinstance(im,dict) else im
                (p.parent/f'{i:03}.png').write_bytes(base64.b64decode(data.split(',')[-1]))
            s['image_count']=len(imgs)
            if r.get('usage') or last.get('usage'): s['usage']=r.get('usage') or last['usage']
        write(p.parent/'response.json',clean(r)); write(p,s); print(json.dumps(s,ensure_ascii=False))
if __name__=='__main__':
    if sys.argv[1]=='submit': submit(sys.argv[2])
    elif sys.argv[1]=='poll': poll()
    elif sys.argv[1]=='balance': print(json.dumps(request('/balance')))
