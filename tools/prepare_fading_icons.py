"""Split the supplied two-icon cutout without stretching; native 128px power textures."""
from pathlib import Path
from PIL import Image,ImageOps
import json
r=Path(__file__).resolve().parents[1];folder=r/'Assets/Frostsworn/void'
im=Image.open(r/'art-source/void/fading-stats.png').convert('RGBA');meta=[]
assert im.getpixel((0,0))[3]==0
for i,name in enumerate(['VoidFadingDexterityPower','VoidFadingStrengthPower']):
 tile=im.crop((round(i*im.width/2),0,round((i+1)*im.width/2),im.height))
 alpha=tile.getchannel('A');box=alpha.point(lambda a:255 if a>24 else 0).getbbox();assert box
 # Ignore invisible cutout noise while preserving disconnected intended fragments.
 tile=tile.crop(box);scaled=ImageOps.contain(tile,(115,115),Image.Resampling.LANCZOS)
 result=Image.new('RGBA',(128,128));result.alpha_composite(scaled,((128-scaled.width)//2,(128-scaled.height)//2));result.save(folder/(name+'.png'))
 meta.append(dict(power=name,source_width=tile.width,source_height=tile.height,scaled_width=scaled.width,scaled_height=scaled.height))
 assert abs(scaled.width/scaled.height-tile.width/tile.height)<.02
(folder/'fading-icons.json').write_text(json.dumps(meta,indent=2)+'\n',encoding='utf8')
print('Fading Dexterity/Strength: 128px transparent, aspect preserved, centered, 90% occupancy.')
