"""Create a Spine 4.2 weighted mesh rig and offline preview; no raster edits."""
from pathlib import Path
import math, json, base64
from PIL import Image

ROOT = Path(__file__).resolve().parent
W = H = 1254
DURATION = 6.0
positions = [
    ('root',None,(627,1215)),
    ('hips','root',(652,641)),
    ('chest','hips',(651,420)),
    ('head','chest',(692,305)),
    ('hair_mid','head',(468,413)),
    ('hair_tail','hair_mid',(303,509)),
    ('hem_left','hips',(518,677)),
    ('hem_right','hips',(760,700)),
    ('arm_free','chest',(539,427)),
    ('arm_staff','chest',(774,434)),
    ('staff','arm_staff',(994,521)),
    ('hat_charm','head',(415,111))]
world = {name:(p[0]-627,1215-p[1]) for name,_,p in positions}
indices = {name:i for i,(name,_,_) in enumerate(positions)}
bones = []
for name,parent,p in positions:
    wx,wy=world[name]; px,py=world.get(parent,(0,0))
    bone={'name':name,'x':round(wx-px,4),'y':round(wy-py,4),'length':30}
    if parent: bone['parent']=parent
    bones.append(bone)

def smooth(a,b,x):
    q=max(0,min(1,(x-a)/(b-a)))
    return q*q*(3-2*q)

def influences(x,y):
    # Keep feet and lower legs planted; chest and head share a breathing chain.
    chest=1-smooth(470,770,y)
    head=1-smooth(305,365,y)
    result={'root':1-chest,'chest':chest*(1-head),'head':chest*head}
    def assign(name,weight):
        nonlocal result
        weight=max(0,min(1,weight))
        result={k:v*(1-weight) for k,v in result.items()}
        result[name]=result.get(name,0)+weight
    # Rear braid and ponytail: smooth transition near the head, more travel at tips.
    hair=(1-smooth(480,620,x))*smooth(260,400,y)*(1-smooth(660,730,y))
    assign('hair_mid',hair*.55)
    tail=(1-smooth(270,380,x))*smooth(340,430,y)*(1-smooth(670,715,y))
    assign('hair_tail',tail)
    # Cloth moves while the bare legs and shoes remain stationary.
    left=(1-smooth(510,590,x))*smooth(670,750,y)*(1-smooth(1010,1070,y))
    right=smooth(845,930,x)*smooth(670,760,y)*(1-smooth(1010,1080,y))
    assign('hem_left',left)
    assign('hem_right',right)
    free=smooth(300,355,x)*(1-smooth(500,580,x))*smooth(440,510,y)*(1-smooth(670,730,y))
    assign('arm_free',free*.85)
    arm=smooth(785,850,x)*(1-smooth(1020,1090,x))*smooth(365,455,y)*(1-smooth(720,780,y))
    assign('arm_staff',arm*.85)
    # Bind the staff to the grip, including its distant top and lower shaft.
    staff_x=1207-y*.407
    shaft=(1-smooth(48,86,abs(x-staff_x)))*smooth(200,340,y)*(1-smooth(1030,1100,y))
    crown=smooth(1000,1050,x)*(1-smooth(260,315,y))
    assign('staff',max(shaft,crown))
    charm=(1-smooth(18,43,abs(x-414)))*(1-smooth(220,255,y))*smooth(90,120,y)
    assign('hat_charm',charm*.85)
    result={k:v for k,v in result.items() if v>.003}
    total=sum(result.values())
    return [(indices[k],v/total) for k,v in result.items()]

def combat_stance(x,y):
    """Shape the native mesh into a forward, lower combat stance; preserve UVs."""
    planted=1-smooth(800,1100,y)
    forward=46*(1-smooth(440,850,y))+20*planted
    sink=28*planted
    free=(1-smooth(455,570,x))*smooth(440,530,y)*(1-smooth(660,740,y))
    tx,ty=x+forward+30*free,y+sink-24*free
    staff_x=1207-y*.407
    shaft=(1-smooth(48,86,abs(x-staff_x)))*smooth(210,340,y)*(1-smooth(1050,1100,y))
    crown=smooth(1000,1050,x)*(1-smooth(260,315,y))
    staff=max(shaft,crown)
    angle=math.radians(7)
    dx,dy=x-994,y-521
    sx=994+dx*math.cos(angle)-dy*math.sin(angle)+45
    sy=521+dx*math.sin(angle)+dy*math.cos(angle)+28
    return tx*(1-staff)+sx*staff,ty*(1-staff)+sy*staff

def mesh(path, xs, ys, blink=False):
    coords=[(x,y) for y in ys for x in xs]
    corners=[(xs[0],ys[0]),(xs[-1],ys[0]),(xs[-1],ys[-1]),(xs[0],ys[-1])]
    ordered=corners+[p for p in coords if p not in corners]
    lookup={p:i for i,p in enumerate(ordered)}
    uvs=[];vertices=[];triangles=[]
    for x,y in ordered:
        uvs.extend([round(x/W,7),round(y/H,7)])
        weights=influences(x,y)
        posed_x,posed_y=combat_stance(x,y)
        vertices.append(len(weights))
        for index,weight in weights:
            wx,wy=world[positions[index][0]]
            vertices.extend([index,round(posed_x-627-wx,5),round(1215-posed_y-wy,5),round(weight,7)])
    for j in range(len(ys)-1):
        for i in range(len(xs)-1):
            a,b,c,d=[lookup[p] for p in [(xs[i],ys[j]),(xs[i+1],ys[j]),(xs[i+1],ys[j+1]),(xs[i],ys[j+1])]]
            triangles.extend([a,d,c,a,c,b])
    return {'type':'mesh','path':path,'uvs':uvs,'triangles':triangles,'vertices':vertices,'hull':4,'width':W,'height':H}

grid=[round(i*W/50,4) for i in range(51)]
body=mesh('full',grid,grid)
# Only the tiny eye area is replaced. The AI's other pixels never enter the pose.
blink=mesh('full-blink',[665,680,695,710,725,740,755],[247,260,273,286,299])

def curve(fn,field):
    return [{'time':round(i*.1,3),field:round(fn(i*.1),5)} for i in range(61)]
def oscillate(a,phase=0):
    return lambda t:a*(math.sin(t*math.tau/DURATION+phase)-math.sin(phase))

tracks={
 'hips':{'translate':[{'time':round(i*.1,3),'x':round(3*math.sin(i*.1*math.tau/6),5),'y':round(-3*(1-math.cos(i*.1*math.tau/6)),5)} for i in range(61)]},
 'chest':{'translate':[{'time':round(i*.1,3),'x':round(5*math.sin(i*.1*math.tau/6),5),'y':round(9*(1-math.cos(i*.1*math.tau/6)),5)} for i in range(61)],'rotate':curve(oscillate(.85),'value')},
 'head':{'rotate':curve(oscillate(.7,.4),'value')},
 'hair_mid':{'rotate':curve(oscillate(2.5,.75),'value')},
 'hair_tail':{'rotate':curve(oscillate(4.5,1.1),'value')},
 'hem_left':{'rotate':curve(oscillate(2,.5),'value')},
 'hem_right':{'rotate':curve(oscillate(-1.8,.85),'value')},
 'arm_free':{'rotate':curve(oscillate(2.2,.35),'value')},
 'arm_staff':{'rotate':curve(oscillate(.55,.3),'value')},
 'staff':{'rotate':curve(oscillate(-.25,.3),'value')},
 'hat_charm':{'rotate':curve(oscillate(3.6,.9),'value')}}
animation={'bones':tracks,'slots':{'eyes':{'attachment':[{'time':0,'name':None},{'time':2.40,'name':'blink'},{'time':2.51,'name':None},{'time':5.16,'name':'blink'},{'time':5.26,'name':None},{'time':6,'name':None}]}}}
skeleton={'skeleton':{'spine':'4.2.43','x':-627,'y':-39,'width':W,'height':H,'images':'./assets/'},
 'bones':bones,'slots':[{'name':'character','bone':'root','attachment':'body'},{'name':'eyes','bone':'root'}],
 'skins':[{'name':'default','attachments':{'character':{'body':body},'eyes':{'blink':blink}}}],
 'animations':{'idle_loop':animation}}
(ROOT/'frostsworn-idle.json').write_text(json.dumps(skeleton,separators=(',',':')),encoding='utf-8')
atlas=''
for name in ['full','full-blink']:
    with Image.open(ROOT/'assets'/f'{name}.png') as image: w,h=image.size
    atlas+=f'assets/{name}.png\nsize: {w},{h}\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\npma: false\n{name}\n  bounds: 0,0,{w},{h}\n  offsets: 0,0,{w},{h}\n\n'
(ROOT/'frostsworn-idle.atlas').write_text(atlas,encoding='utf-8')
embedded={'skeleton':skeleton,'atlas':atlas,'images':{f'assets/{name}.png':'data:image/png;base64,'+base64.b64encode((ROOT/'assets'/f'{name}.png').read_bytes()).decode() for name in ['full','full-blink']}}
template=(ROOT/'preview-template.html').read_text('utf-8')
runtime=(ROOT/'spine-webgl.js').read_text('utf-8')
gifenc=(ROOT/'gifenc.js').read_text('utf-8')
(ROOT/'待机动画演示.html').write_text(template.replace('/*RUNTIME*/',runtime).replace('/*GIFENC*/',gifenc).replace('/*DATA*/',json.dumps(embedded,separators=(',',':'))),encoding='utf-8')
print(f'IDLE_RIG: {len(bones)} bones, 2601 vertices, 5000 triangles, 6s loop, eye overlay only')

