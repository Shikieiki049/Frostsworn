"""Wrap supplied PNG artwork in the native hand canvas without redrawing it."""
from pathlib import Path
import base64
import shutil

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'ArtSources/hands-0828'
DEST = ROOT / 'Assets/Frostsworn/multiplayer'
DEST.mkdir(parents=True, exist_ok=True)
# Shared uniform scale and bottom anchor across all gestures. The lower sleeve
# can extend off the canvas like native arms; fingers remain inside the canvas.
for pose in ('point', 'rock', 'paper', 'scissors'):
    encoded = base64.b64encode((SOURCE / f'{pose}.png').read_bytes()).decode()
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="422" height="1200" viewBox="0 0 422 1200"><image x="-80" y="196.8" width="564.6" height="1003.2" xlink:href="data:image/png;base64,{encoded}"/></svg>'
    (DEST / f'{pose}.svg').write_text(svg, encoding='utf-8')
shutil.copy2(SOURCE / 'death.png', ROOT / 'Assets/Frostsworn/idle/death.png')
print('Four native hand canvases and original static death art prepared.')
