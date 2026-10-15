"""Split transparent sheets by whole alpha components, never by clipping grid borders.

Centroid assignment retains intentional detached bubbles/crumbs, while removing
subpixel noise. Crops and native-size variants are centered from the true alpha bounds.
"""
from pathlib import Path
from collections import deque
from PIL import Image, ImageOps
repo=Path(__file__).resolve().parents[1]
a=repo/'Assets/Frostsworn/void'; a.mkdir(exist_ok=True)
source=repo/'art-source/void'
for filename,names in [
 ('relics-1.png','BloodlessCord DrownedFloat RuminationSac MirrorParasite MemorySlough LightMothCocoon CurledPolyp TriocularWax'.split()),
 ('relics-2.png','FaceborrowMembrane MeristemTeeth HangingStarSac MarrowTouch MoldyCookie GymnospermPendant OverdriveGearbox LighthouseJelly'.split())]:
 im=Image.open(source/filename).convert('RGBA'); w,h=im.size
 alpha=bytearray(im.getchannel('A').tobytes()); groups=[[] for _ in names]
 for pos in range(w*h):
  if alpha[pos]<16:continue
  pending=deque([pos]); alpha[pos]=0; points=[]
  while pending:
   p=pending.popleft(); points.append(p); x=p%w; y=p//w
   for q in ([p-1] if x else [])+([p+1] if x+1<w else [])+([p-w] if y else [])+([p+w] if y+1<h else []):
    if alpha[q]>=16:alpha[q]=0;pending.append(q)
  if len(points)<50:continue
  cx=sum(p%w for p in points)/len(points); cy=sum(p//w for p in points)/len(points)
  idx=min(1,int(cy*2/h))*4+min(3,int(cx*4/w));groups[idx].extend(points)
 for name,points in zip(names,groups):
  assert points,name
  # A 2px halo preserves antialiasing around retained components, without adjacent sprites.
  mask=Image.new('L',im.size); pixels=mask.load()
  for p in points:pixels[p%w,p//w]=255
  from PIL import ImageFilter,ImageChops
  mask=mask.filter(ImageFilter.MaxFilter(5)); mask=ImageChops.multiply(mask,im.getchannel('A'))
  tile=im.copy();tile.putalpha(mask);tile=tile.crop(mask.getbbox())
  for size,suffix in [(256,''),(64,'_small')]:
   scaled=ImageOps.contain(tile,(round(size*.88),round(size*.88)),Image.Resampling.LANCZOS)
   canvas=Image.new('RGBA',(size,size));canvas.alpha_composite(scaled,((size-scaled.width)//2,(size-scaled.height)//2));canvas.save(a/(name+suffix+'.png'))
 print(filename,'8 intact centered sprites')
