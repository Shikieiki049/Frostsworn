"""Generate code-native atlas regions; source and cutout PNGs remain unchanged."""
from pathlib import Path
import json, sys
from PIL import Image
import math
sys.path.insert(0, str(Path(__file__).parent))
from powers_data import powers

root = Path(__file__).resolve().parents[1]
folder = root / 'Assets/Frostsworn/buffs'
mapping = []
# Sheet indices follow the supplied row-major names, rather than filesystem sorting.
names = [
 '下回合冰甲，冰甲，冷藏，永夜风暴，积雪，雪势，下回合解冻，冰窖',
 '热交换，速释装置，冰晶护符，冷库扩张，寒霜，极寒晶核，祝福之风，铭刻于心',
 '晶锋，漫冬，藏锋秘术，风暴核心，冰川形态，冰镜圣所，寒血回响，暴雪庇护',
 '分形雪花，晶群共鸣，流淌而出的强大力量，自霜，雪眼，冬日筹备，冰释，命定的物语']
by_name = {v[0]: k for k, v in powers.items()}
for sheet, row in enumerate(names, 1):
    with Image.open(folder / f'sheet{sheet}.png') as image:
        assert image.mode == 'RGBA' and image.getextrema()[3][0] == 0
        assert image.width == image.height * 2
        # Godot imports the full sheet at 512x256: each icon is native 128x128.
        # Keep source cutouts unchanged; avoid oversized native particle textures.
        width, height = 512, 256
    importer = folder / f'sheet{sheet}.png.import'
    if importer.exists():
        importer.write_text(importer.read_text('utf-8').replace('process/size_limit=0', 'process/size_limit=512'), encoding='utf-8')
    for index, name in enumerate(row.split('，')):
        cls = by_name[name]
        x0, x1 = round(index % 4 * width / 4), round((index % 4 + 1) * width / 4)
        y0, y1 = round(index // 4 * height / 2), round((index // 4 + 1) * height / 2)
        # Measure visible artwork without changing the PNG. Match native icons'
        # visual occupancy and center uneven source whitespace independently.
        with Image.open(folder / f'sheet{sheet}.png') as source:
            alpha = source.getchannel('A')
            sx0, sx1 = round(index % 4 * source.width / 4), round((index % 4 + 1) * source.width / 4)
            sy0, sy1 = round(index // 4 * source.height / 2), round((index // 4 + 1) * source.height / 2)
            pixels = alpha.load()
            visible = [(x,y) for y in range(sy0,sy1) for x in range(sx0,sx1) if pixels[x,y] > 24]
            assert visible
            left = max(x0, math.floor(min(p[0] for p in visible) * width / source.width)-1)
            top = max(y0, math.floor(min(p[1] for p in visible) * height / source.height)-1)
            right = min(x1, math.ceil((max(p[0] for p in visible)+1) * width / source.width)+1)
            bottom = min(y1, math.ceil((max(p[1] for p in visible)+1) * height / source.height)+1)
        w, h = right-left, bottom-top
        side = min(128, math.ceil(max(w,h)/0.90))
        mx, my = (side-w)//2, (side-h)//2
        text = f'''[gd_resource type="AtlasTexture" load_steps=2 format=3]

[ext_resource type="Texture2D" path="res://Frostsworn/buffs/sheet{sheet}.png" id="1"]

[resource]
atlas = ExtResource("1")
region = Rect2({left}, {top}, {w}, {h})
margin = Rect2({mx}, {my}, {side-w}, {side-h})
filter_clip = true
'''
        (folder / f'{cls}.tres').write_text(text, encoding='utf-8')
        mapping.append({'power': cls, 'name': name, 'sheet': sheet, 'index': index})
(folder / 'mapping.json').write_text(json.dumps(mapping, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(f'BUFF_ATLASES: {len(mapping)}')
