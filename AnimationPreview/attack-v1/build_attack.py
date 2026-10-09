"""Spine 4.2 rigid-part attacks, articulated arms and planted two-bone IK legs.
No raster editing: PNGs are used intact through native UV attachment polygons.
"""
from pathlib import Path
import json,math,base64
from PIL import Image
ROOT=Path(__file__).resolve().parent
bones=[];world={};lengths={}
def bone(name,parent,screen,angle=0,length=0):
    wx,wy=screen[0]-627,1215-screen[1]
    px,py,pa=world.get(parent,(0,0,0));c,s=math.cos(math.radians(pa)),math.sin(math.radians(pa))
    dx,dy=wx-px,wy-py
    item={'name':name,'x':round(c*dx+s*dy,6),'y':round(-s*dx+c*dy,6),'rotation':round(angle-pa,6),'length':round(length,6)}
    if parent:item['parent']=parent
    bones.append(item);world[name]=(wx,wy,angle);lengths[name]=length
def angle(a,b):return math.degrees(math.atan2(a[1]-b[1],b[0]-a[0]))
def distance(a,b):return math.dist(a,b)
bone('root',None,(627,1215));bone('pelvis','root',(660,600));bone('torso','pelvis',(651,420))
bone('head','torso',(692,305));bone('hair','head',(580,330));bone('cape','pelvis',(660,570))
bone('upper_staff','torso',(778,430),angle((778,430),(862,515)),distance((778,430),(862,515)))
bone('forearm_staff','upper_staff',(862,515),angle((862,515),(987,478)),distance((862,515),(987,478)))
bone('wrist_staff','forearm_staff',(987,478));bone('staff','wrist_staff',(1030,492))
bone('upper_free','torso',(590,405),angle((590,405),(545,517)),distance((590,405),(545,517)))
bone('forearm_free','upper_free',(545,517),angle((545,517),(429,531)),distance((545,517),(429,531)))
bone('wrist_free','forearm_free',(429,531))
# Independent melee staff drives both grip points and both arm IK targets.
# Authored positions keep the targets inside normal arm reach.
bone('melee_staff','root',(840,520),-15)
def melee_screen(x,y):
    dx=(x-662)*.65;dy=(y-740)*.65
    a=math.radians(20)
    return (840+math.cos(a)*dx-math.sin(a)*dy,520+math.sin(a)*dx+math.cos(a)*dy)
second_grip=melee_screen(575,900)
bone('melee_left_grip','melee_staff',second_grip,-15)
bone('melee_right_wrist','melee_staff',(768,518))
bone('melee_left_wrist','melee_staff',(second_grip[0]-72,second_grip[1]-2))

ik=[]
for side,hip,knee,ankle,bend in [('left',(637,738),(579,868),(490,1020),1),('right',(734,810),(767,919),(784,1027),-1)]:
    bone('thigh_'+side,'pelvis',hip,angle(hip,knee),distance(hip,knee))
    bone('shin_'+side,'thigh_'+side,knee,angle(knee,ankle),distance(knee,ankle))
    bone('foot_'+side,'root',ankle)
    ik.append({'name':'plant_'+side,'order':len(ik),'bones':['thigh_'+side,'shin_'+side],'target':'foot_'+side,'mix':1,'bendPositive':bend>0,'softness':5})

ik.extend([
 {'name':'double_right','order':2,'bones':['upper_staff','forearm_staff'],'target':'melee_right_wrist','mix':0,'bendPositive':False,'softness':0},
 {'name':'double_left','order':3,'bones':['upper_free','forearm_free'],'target':'melee_left_wrist','mix':0,'bendPositive':True,'softness':0}
])

def triangulate(poly):
    ids=list(range(len(poly)))
    area=sum(poly[i][0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*poly[i][1] for i in ids)
    if area<0:ids.reverse()
    def cross(a,b,c):return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    result=[]
    while len(ids)>3:
        for i,b in enumerate(ids):
            a,c=ids[i-1],ids[(i+1)%len(ids)]
            if cross(poly[a],poly[b],poly[c])<=1e-9:continue
            def inside(p):return cross(poly[a],poly[b],p)>=0 and cross(poly[b],poly[c],p)>=0 and cross(poly[c],poly[a],p)>=0
            if any(inside(poly[j]) for j in ids if j not in (a,b,c)):continue
            result.extend([a,c,b]);ids.pop(i);break
        else:raise ValueError('Self-intersecting UV polygon')
    result.extend(ids[::-1]);return result
def affine(scale=1,anchor=(0,0),target=(0,0),degrees=0,mirror=False):
    c,s=math.cos(math.radians(degrees)),math.sin(math.radians(degrees))
    def apply(x,y):
        dx=(x-anchor[0])*scale*(-1 if mirror else 1);dy=(y-anchor[1])*scale
        return target[0]+c*dx-s*dy,target[1]+s*dx+c*dy
    return apply
slots=[];attachments={};images={}
def part(name,path,parent,poly=None,transform=None):
    with Image.open(ROOT/'assets'/f'{path}.png') as im:w,h=im.size
    if poly is None:poly=[(0,0),(w,0),(w,h),(0,h)]
    transform=transform or (lambda x,y:(x,y));bx,by,ba=world[parent];c,s=math.cos(math.radians(ba)),math.sin(math.radians(ba));vertices=[]
    for x,y in poly:
        px,py=transform(x,y);dx,dy=px-627-bx,1215-py-by
        vertices.extend([round(c*dx+s*dy,6),round(-s*dx+c*dy,6)])
    mesh={'type':'mesh','path':path,'uvs':[v for x,y in poly for v in (x/w,y/h)],'triangles':triangulate(poly),'vertices':vertices,'hull':len(poly),'width':w,'height':h}
    attachments[name]={name:mesh};slots.append({'name':name,'bone':parent,'attachment':name});images[path]=(w,h)

part('back-hair','full','hair',[(620,230),(673,280),(645,353),(580,425),(501,495),(408,550),(354,588),(287,690),(140,714),(67,630),(83,488),(178,405),(296,431),(383,447),(415,386),(508,329),(570,270)])
part('shin-left','full','shin_left',[(586,835),(626,857),(565,953),(532,1048),(482,1048),(471,1027),(517,927),(548,868)])
part('thigh-left','full','thigh_left',[(617,680),(700,710),(700,765),(605,907),(548,899),(575,823),(590,770)])
part('shin-right','full','shin_right',[(747,893),(791,890),(802,1033),(751,1040),(738,942)])
part('thigh-right','full','thigh_right',[(716,764),(781,782),(805,880),(789,928),(741,920),(689,837)])
part('shoe-left','shoe-left','foot_left',transform=affine(.20,(680,100),(490,1015)))
part('shoe-right','shoe-right','foot_right',transform=affine(.225,(560,100),(777,970)))
bodymap=affine(.70,(650,100),(681,341))
# One continuous attachment from collar to hem. A waist cut on a separately
# rotating cape exposed two moving edges across the belt and chest.
part('torso','torso-clean-v2','torso',transform=bodymap)
rightmap=affine(.31,(240,200),(778,430),degrees=-7)
rightfore=affine(.235,(470,510),(862,515),degrees=-33)
leftmap=affine(.34,(300,200),(590,405),degrees=-3,mirror=True)
leftfore=affine(.26,(450,520),(545,517),degrees=21,mirror=True)
upperpoly=[(0,0),(620,0),(660,460),(610,590),(410,650),(200,600),(0,600)]
forepoly=[(350,450),(1254,450),(1254,1254),(350,1254)]
part('upper-free','sleeve-free-v2','upper_free',upperpoly,leftmap)
part('forearm-free','sleeve-free-v2','forearm_free',forepoly,leftfore)
part('upper-staff','sleeve-staff-v2','upper_staff',upperpoly,rightmap)
part('forearm-staff','sleeve-staff-v2','forearm_staff',forepoly,rightfore)
# The fixed shoulder mantle is in front of both sleeve roots, so the rotating
# sleeve cut edges stay underneath the same continuous chest artwork.
part('shoulder-mantle','torso-clean-v2','torso',[(400,0),(920,0),(920,310),(870,360),(800,325),(790,409),(705,313),(640,364),(570,414),(565,331),(430,347),(405,310)],bodymap)
# Shaft is in front of the clothing, with the gripping fingers above it.
part('staff','staff','staff',transform=affine(.65,(662,740),(1030,492),degrees=5))
part('hand-free','arm-staff','wrist_free',[(955,495),(1254,495),(1254,800),(955,800)],affine(.55,(980,640),(429,531),mirror=True))
part('hand-staff','arm-staff','wrist_staff',[(955,495),(1254,495),(1254,800),(955,800)],affine(.50,(1118,594),(1030,492)))
part('double-staff','staff','melee_staff',transform=affine(.65,(662,740),(840,520),degrees=20))
slots[-1].pop('attachment')
part('double-hand-right','arm-staff','melee_staff',[(955,495),(1254,495),(1254,800),(955,800)],affine(.50,(1118,594),(840,520),degrees=20))
slots[-1].pop('attachment')
part('double-hand-left','arm-staff','melee_left_grip',[(955,495),(1254,495),(1254,800),(955,800)],affine(.50,(1118,594),second_grip,degrees=20))
slots[-1].pop('attachment')
part('head','full','head',[(375,0),(1000,0),(1000,305),(874,320),(899,447),(818,495),(743,453),(717,370),(660,372),(626,330),(570,290),(380,318)])
part('eyes','full-blink','head',[(665,247),(755,247),(755,299),(665,299)]);slots[-1].pop('attachment')

# Interpolate authored anticipation, acceleration, contact, follow-through and recovery.
def sample(keys,duration):
    result=[]
    for i in range(round(duration*60)+1):
        t=min(i/60,duration)
        for j in range(len(keys)-1):
            a,b=keys[j],keys[j+1]
            if a[0]<=t<=b[0]+1e-8:
                u=max(0,min(1,(t-a[0])/(b[0]-a[0])));mode=a[2] if len(a)>2 else 'smooth'
                if mode=='in':u=u*u*u
                elif mode=='out':u=1-(1-u)**3
                elif mode!='linear':u=u*u*(3-2*u)
                value=a[1]+(b[1]-a[1])*u;break
        else:value=keys[-1][1]
        result.append((round(t,6),round(value,6)))
    return result
def animation(duration,rotations,translations=None):
    tracks={n:{'rotate':[{'time':t,'value':v} for t,v in sample(keys,duration)]} for n,keys in rotations.items()}
    for name,xy in (translations or {}).items():
        x=sample(xy[0],duration);y=sample(xy[1],duration)
        tracks.setdefault(name,{})['translate']=[{'time':t,'x':vx,'y':vy} for (t,vx),(_,vy) in zip(x,y)]
    return {'bones':tracks}

M=2.10;P=1.85
magic=animation(M,{
 'torso':[(0,0),(.36,4),(.85,-4),(1.08,-6),(1.2,2),(1.5,-2),(M,0)],
 'head':[(0,0),(.38,-3),(.90,2),(1.24,-1),(M,0)],
 'upper_staff':[(0,0),(.25,18),(.58,32),(.91,20),(1.04,22),(1.17,28),(1.48,12),(M,0)],
 'forearm_staff':[(0,0),(.26,12),(.59,-35),(.93,-65,'in'),(1.08,-68,'out'),(1.23,-58),(1.5,-30),(M,0)],
 'upper_free':[(0,0),(.30,-15),(.68,-32),(1.03,-35),(1.24,-20),(M,0)],
 'forearm_free':[(0,0),(.35,-12),(.74,-26),(1.1,-23),(1.5,-12),(M,0)],
 'hair':[(0,0),(.45,2),(.94,-2),(1.26,-5),(1.55,2),(M,0)],
 'cape':[(0,0),(.50,1.5),(1.10,-2),(1.30,-4),(1.62,2),(M,0)],
 'staff':[(0,0),(.60,2),(1.05,0),(1.23,1.5),(M,0)]
},{'pelvis':([(0,0),(.6,3),(1.05,8),(1.24,2),(M,0)],[(0,0),(.65,-6),(1.05,-10),(1.32,-5),(M,0)])})
physical=animation(P,{
 'torso':[(0,0),(.22,4),(.52,9,'in'),(.74,-9),(.80,-9,'out'),(.96,-12),(1.1,-5),(P,0)],
 'head':[(0,0),(.40,-4),(.66,2),(.86,5),(1.14,-1),(P,0)],
 'upper_staff':[(0,0),(.18,8),(.52,62,'in'),(.74,-12),(.80,-12,'out'),(.94,-24),(1.1,-5),(1.42,4),(P,0)],
 'forearm_staff':[(0,0),(.18,12),(.52,38,'in'),(.74,-43),(.80,-43,'out'),(.94,-54),(1.12,-30),(1.43,-10),(P,0)],
 'upper_free':[(0,0),(.48,-18),(.72,-5),(.95,14),(1.28,-4),(P,0)],
 'forearm_free':[(0,0),(.5,-35),(.77,-20),(.95,-2),(P,0)],
 'hair':[(0,0),(.48,4),(.72,1),(.88,-7),(1.08,-4),(1.35,3),(P,0)],
 'cape':[(0,0),(.53,3),(.74,-1),(.94,-5),(1.17,3),(1.44,-1),(P,0)],
 'staff':[(0,0),(.52,4),(.74,-2),(.80,-2),(.96,-1),(P,0)]
},{'pelvis':([(0,0),(.22,-5),(.52,-12,'in'),(.74,20),(.80,20),(.96,26),(1.14,12),(P,0)],[(0,0),(.50,-7),(.74,-15),(.80,-15),(.96,-18),(1.18,-6),(P,0)])})
# Two-hand melee: the rigid staff and its two grips move as one unit;
# the arm joints solve towards wrist points without scaling the artwork.
physical['bones'].pop('upper_staff');physical['bones'].pop('forearm_staff')
physical['bones'].pop('upper_free');physical['bones'].pop('forearm_free')
physical['bones']['melee_staff']={
 'rotate':[{'time':t,'value':v} for t,v in sample([(0,0),(.25,18),(.52,70,'in'),(.74,-50),(.80,-50,'out'),(.96,-65),(1.18,-20),(P,0)],P)],
 'translate':[{'time':t,'x':x,'y':y} for (t,x),(_,y) in zip(sample([(0,0),(.25,-65),(.52,-155,'in'),(.74,30),(.80,30),(.96,20),(1.18,0),(P,0)],P),sample([(0,0),(.25,35),(.52,80,'in'),(.74,-40),(.80,-40),(.96,-85),(1.18,-20),(P,0)],P))]
}
physical['ik']={n:[{'time':0,'mix':1},{'time':P,'mix':1}] for n in ['double_right','double_left']}
physical['slots']={n:{'attachment':[{'time':0,'name':None}]} for n in ['staff','hand-staff','hand-free']}
for n in ['double-staff','double-hand-right','double-hand-left']:
 physical['slots'][n]={'attachment':[{'time':0,'name':n}]}
idle_melee=animation(6,{'torso':[(0,0),(6,0)]})
idle_melee['ik']={n:[{'time':0,'mix':1},{'time':6,'mix':1}] for n in ['double_right','double_left']}
idle_melee['slots']=physical['slots']
idle=animation(6,{'torso':[(0,0),(1.5,.3),(3,0),(4.5,-.3),(6,0)],'hair':[(0,0),(1.5,1),(3,0),(4.5,-1),(6,0)],'cape':[(0,0),(1.5,.45),(3,0),(4.5,-.45),(6,0)]})
idle['slots']={'eyes':{'attachment':[{'time':0,'name':None},{'time':2.40,'name':'eyes'},{'time':2.51,'name':None},{'time':6,'name':None}]}}
magic['events']=[{'time':1.05,'name':'spell_release'}]
physical['events']=[{'time':.75,'name':'melee_hit'}]
skeleton={'skeleton':{'spine':'4.2.43','x':-627,'y':-39,'width':1254,'height':1254,'images':'./assets/'},'bones':bones,'ik':ik,'slots':slots,'skins':[{'name':'default','attachments':attachments}],'events':{'spell_release':{},'melee_hit':{}},'animations':{'idle_loop':idle,'attack_magic':magic,'attack_melee':physical,'idle_melee':idle_melee}}
(ROOT/'frostsworn-attacks.json').write_text(json.dumps(skeleton,separators=(',',':')),encoding='utf8')
atlas=''
for n,(w,h) in images.items():atlas+=f'assets/{n}.png\nsize: {w},{h}\nformat: RGBA8888\nfilter: Linear,Linear\nrepeat: none\npma: false\n{n}\n  bounds: 0,0,{w},{h}\n  offsets: 0,0,{w},{h}\n\n'
(ROOT/'frostsworn-attacks.atlas').write_text(atlas,encoding='utf8')
# Position of the staff crystal in local coordinates, for precise VFX anchoring.
tip=affine(.65,(662,740),(1030,492),degrees=5)(865,140)
sx,sy,sa=world['staff'];tiplocal=[tip[0]-627-sx,1215-tip[1]-sy]
melee_tip_screen=melee_screen(865,140)
mbx,mby,mba=world['melee_staff']; mdx,mdy=melee_tip_screen[0]-627-mbx,1215-melee_tip_screen[1]-mby
mc,ms=math.cos(math.radians(mba)),math.sin(math.radians(mba))
melee_tip=[mc*mdx+ms*mdy,-ms*mdx+mc*mdy]
embedded={'skeleton':skeleton,'atlas':atlas,'staffTip':tiplocal,'meleeTip':melee_tip,'images':{f'assets/{n}.png':'data:image/png;base64,'+base64.b64encode((ROOT/'assets'/f'{n}.png').read_bytes()).decode() for n in images}}
template=(ROOT/'preview-template.html').read_text('utf8')
(ROOT/'\u653b\u51fb\u52a8\u753b\u6f14\u793a.html').write_text(template.replace('/*RUNTIME*/',(ROOT/'spine-webgl.js').read_text('utf8')).replace('/*GIFENC*/',(ROOT/'gifenc.js').read_text('utf8')).replace('/*DATA*/',json.dumps(embedded,separators=(',',':'))),encoding='utf8')
print(f'ATTACKS: {len(bones)} bones, {len(slots)} rigid parts, 2 planted IK chains; magic {M}s / melee {P}s')
