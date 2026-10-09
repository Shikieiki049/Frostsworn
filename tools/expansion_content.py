from cards_data import cards
from powers_data import powers
relics = {'WinterCrown':('至冬核心','每场战斗开始时，给予所有敌人5层寒霜。\n每回合给予所有敌人2层寒霜。')}
relics.update({
'SnowBookmark':('雪花书签','回合结束时，冷藏区每有1张牌，获得1层冰甲。'),
'PolarGlobe':('极地天球','每冷藏3张牌，可以选择至多1张牌解冻。'),
'WinterBottle':('余冬之瓶','每场战斗首次抽牌前，从抽牌堆选择1张牌冷藏。'),
'IceKey':('冰匙','每回合首次冰封敌人时，对其造成12点伤害。'),
'FrozenSoil':('冻土','每场战斗首次冰甲融化结算后，获得7层冰甲。'),
'SnowPrimer':('超简单的雪系法术入门教程！','翻倍你每回合首次获得的雪势。'),
'FrostDiploma':('高阶冰系法师证明','每次对敌人造成伤害时，给予其1层寒霜。'),
})

if __name__ == '__main__':
    from pathlib import Path
    import re
    text='// Generated from tools/cards_data.py; edit that file.\nnamespace Frostsworn;\n\n'
    for cls,name,desc,spec in cards:
        slug=re.sub(r'(?<!^)(?=[A-Z])','_',cls).upper()
        starter = ', RegisterCharacterStarterCard(typeof(FrostswornCharacter), 4, Order = 10)' if cls=='FrostStrike' else ', RegisterCharacterStarterCard(typeof(FrostswornCharacter), 4, Order = 20)' if cls=='FrostDefend' else ', RegisterCharacterStarterCard(typeof(FrostswornCharacter), Order = 30)' if cls=='FreezeRay' else ', RegisterCharacterStarterCard(typeof(FrostswornCharacter), Order = 40)' if cls=='CrystalRite' else ''
        pool='MegaCrit.Sts2.Core.Models.CardPools.TokenCardPool' if cls in ('IceCrystal','Depleted') else 'MegaCrit.Sts2.Core.Models.CardPools.EventCardPool' if cls in ('FatedStory','AbsoluteBeam') else 'FrostCardPool'
        text+=f'[RegisterCard(typeof({pool}), FullPublicEntry = "FROSTSWORN_CARD_{slug}"){starter}]\npublic sealed class {cls}() : FrostCard(new({spec}))'
        tag='Strike' if cls=='FrostStrike' else 'Defend' if cls=='FrostDefend' else None
        text+=f' {{ protected override HashSet<CardTag> CanonicalTags => [CardTag.{tag}]; }}\n' if tag else ';\n'
    (Path(__file__).resolve().parents[1]/'Source/ExpansionCards.cs').write_text(text,encoding='utf-8')
