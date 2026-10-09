"""Make native-sized relic textures for the inventory and its GPU particle flash."""
from pathlib import Path
import re
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets'

def generate():
    sources = list((ASSETS / 'Frostsworn/relics083').glob('*.tres'))
    sources += [ASSETS / f'Frostsworn/art075/{name}.tres' for name in ('WinterCore', 'WinterCrown')]
    for resource in sources:
        if resource.stem.endswith('_small'):
            continue
        text = resource.read_text('utf-8')
        texture = re.search(r'path="res://([^"]+)"', text).group(1)
        region = tuple(map(int, re.search(r'region = Rect2\(([^)]+)\)', text).group(1).split(',')))
        x, y, w, h = region
        with Image.open(ASSETS / texture) as image:
            icon = image.crop((x, y, x+w, y+h)).convert('RGBA')
        icon.thumbnail((64, 64), Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA', (64, 64))
        canvas.alpha_composite(icon, ((64-icon.width)//2, (64-icon.height)//2))
        canvas.save(resource.with_name(resource.stem + '_small.png'))
        relative = resource.with_name(resource.stem + '_small.png').relative_to(ASSETS).as_posix()
        resource.with_name(resource.stem + '_small.tres').write_text(
            '[gd_resource type="AtlasTexture" load_steps=2 format=3]\n'
            f'[ext_resource type="Texture2D" path="res://{relative}" id="1"]\n'
            '[resource]\natlas = ExtResource("1")\nregion = Rect2(0, 0, 64, 64)\nfilter_clip = true\n', encoding='utf-8')
    print('Native 64px relic icons generated; large-detail textures preserved.')

if __name__ == '__main__':
    generate()
