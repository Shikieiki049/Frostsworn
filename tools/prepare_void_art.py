"""Slice the transparent relic sheets in reading order; preserve alpha and native 64px icons."""
from pathlib import Path
from PIL import Image,ImageOps
repo=Path(__file__).resolve().parents[1]
a=repo/'Assets/Frostsworn/void';a.mkdir(exist_ok=True)
source=repo/'art-source/void'
for filename,names in [
 ('relics-1.png','BloodlessCord DrownedFloat RuminationSac MirrorParasite MemorySlough LightMothCocoon CurledPolyp TriocularWax'.split()),
 ('relics-2.png','FaceborrowMembrane MeristemTeeth HangingStarSac MarrowTouch MoldyCookie GymnospermPendant OverdriveGearbox LighthouseJelly'.split())]:
 im=Image.open(source/filename).convert('RGBA');print(filename,im.size,'corner alpha',im.getpixel((0,0))[3])
 assert im.getpixel((0,0))[3]==0
 for i,name in enumerate(names):
  x=i%4;y=i//4;w,h=im.size;tile=im.crop((round(x*w/4),round(y*h/2),round((x+1)*w/4),round((y+1)*h/2)))
  tile=tile.crop(tile.getbbox())
  for size,suffix in [(256,''),(64,'_small')]:
   scaled=ImageOps.contain(tile,(round(size*.88),round(size*.88)),Image.Resampling.LANCZOS)
   canvas=Image.new('RGBA',(size,size));canvas.alpha_composite(scaled,((size-scaled.width)//2,(size-scaled.height)//2));canvas.save(a/(name+suffix+'.png'))
