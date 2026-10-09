"""Portable source checks; needs neither the game nor Godot."""
import ast
import json
import re
from pathlib import Path
from cards_data import cards
from version_info import ROOT, version
from english_localization import validate as validate_english


def validate():
    languages = {lang: {p.stem: json.loads(p.read_text('utf-8-sig')) for p in (ROOT / 'Assets/Frostsworn/localization' / lang).glob('*.json')} for lang in ('zhs', 'eng')}
    validate_english(languages['zhs'], languages['eng'])
    for script in (ROOT / 'tools').glob('*.py'):
        ast.parse(script.read_text('utf-8-sig'), filename=str(script))
    names = [card[0] for card in cards]
    assert len(names) == len(set(names)) == 93, 'Card IDs must be unique; expected 93 cards.'
    manifest = json.loads((ROOT / 'mod_manifest.json').read_text('utf-8-sig'))
    assert manifest['id'] == 'Frostsworn' and re.fullmatch(r'\d+\.\d+\.\d+', version())
    assert f'"{version()}"' in (ROOT / 'Source/VersionInfo.Generated.cs').read_text('utf-8-sig')
    generated = (ROOT / 'Source/ExpansionCards.cs').read_text('utf-8-sig')
    for name in names:
        assert f'public sealed class {name}()' in generated, f'Missing generated card: {name}'
    for name in ('IceCrystal', 'Depleted'):
        assert re.search(r'RegisterCard\(typeof\(MegaCrit.Sts2.Core.Models.CardPools.TokenCardPool\)[^\n]*\npublic sealed class ' + name, generated)
    for name in ('FatedStory', 'AbsoluteBeam'):
        assert re.search(r'RegisterCard\(typeof\(MegaCrit.Sts2.Core.Models.CardPools.EventCardPool\)[^\n]*\npublic sealed class ' + name, generated)
    print(f'Source validation passed: version {version()}, {len(names)} unique cards.')


if __name__ == '__main__':
    validate()
