"""Build placeholder art and language-specific localization tables."""
from pathlib import Path
import json, re, math
import expansion_content as expansion
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
assets = root / 'Assets' / 'Frostsworn'
cards = [(cls,name,desc) for cls,name,desc,_ in expansion.cards]

def slug(s): return re.sub(r'(?<!^)(?=[A-Z])','_',s).upper()
tables = {'cards':{}, 'characters':{}, 'powers':{}, 'enchantments':{}, 'relics':{}, 'epochs':{}, 'static_hover_tips':{}, 'ancients':{}, 'potions':{}}
for cls,name,desc in cards:
    desc=desc.replace('将{Amount}张冰晶加入手牌','获得{Amount}张冰晶').replace('其他手牌','牌').replace('选择解冻','解冻')
    key='FROSTSWORN_CARD_'+slug(cls)
    tables['cards'][key+'.title']=name
    tables['cards'][key+'.description']=desc
    tables['cards'][key+'.selectionScreenPrompt']='选择一张牌。'
char='FROSTSWORN_CHARACTER_FROSTSWORN_CHARACTER'
from ancient_dialogues import dialogues
for ancient,lines in dialogues.items():
    for i,(speaker,line) in enumerate(lines):
        tables['ancients'][f'{ancient}.talk.{char}.0-{i}r.{speaker}']=line
        # Native AncientDialogueSet derives this sibling key for every non-final
        # line. Missing it throws while the event layout updates its next button.
        if i<len(lines)-1:
            tables['ancients'][f'{ancient}.talk.{char}.0-{i}r.next']='继续'
    if ancient=='THE_ARCHITECT':
        tables['ancients'][f'{ancient}.talk.{char}.0-startattack']='None'
        tables['ancients'][f'{ancient}.talk.{char}.0-endattack']='None'
for suffix,text in {
    'title':'霜誓者','titleObject':'霜誓者','description':'将城市封入寒冰的流亡法师。\n以寒霜推迟敌人的行动，用冷藏保存下一回合的机会。',
    'pronounObject':'他','possessiveAdjective':'他的','pronounPossessive':'他的','pronounSubject':'他',
    'cardsModifierTitle':'霜誓者卡牌','cardsModifierDescription':'本局使用霜誓者卡池。','unlockText':'霜誓已立。',
    'bestiaryQuote':'让这个冬天再停留片刻。','bestiaryKillQuote':'冰终会融化。','eventDeathPrevention':'尚未到解冻之时。',
}.items(): tables['characters'][char+'.'+suffix]=text
for state,text in [('alive','再给你一点时间。'),('dead','接下来就交给你们了。')]:
    tables['characters'][char+'.banter.'+state+'.endTurnPing']=text
powers=expansion.powers
for cls,(name,desc) in powers.items():
    key='FROSTSWORN_POWER_'+slug(cls)
    tables['powers'].update({key+'.title':name,key+'.description':desc})
    # Native live power tooltips bind DynamicVars only through smartDescription.
    tables['powers'][key+'.smartDescription']=desc
    if cls=='EndlessStormPower': tables['powers'][key+'.description']='雪势在回合结束时只减少固定层数。'
tables['enchantments'].update({'FROSTSWORN_ENCHANTMENT_SHARP_ENCHANTMENT.title':'锐利','FROSTSWORN_ENCHANTMENT_SHARP_ENCHANTMENT.description':'这张牌施加1层易伤。','FROSTSWORN_ENCHANTMENT_SHARP_ENCHANTMENT.extraCardText':'给予1层易伤。'})
key='FROSTSWORN_RELIC_WINTER_CORE'
for cls,(name,desc) in expansion.relics.items():
    prefix='FROSTSWORN_RELIC_'+slug(cls)
    tables['relics'].update({prefix+'.title':name,prefix+'.description':desc,prefix+'.flavor':'冬日留下的遗物。'})
tables['relics'].update({key+'.title':'封冬之核',key+'.description':'每场战斗开始时，给予所有敌人3层寒霜。',key+'.flavor':'它保存着一座城市最后的温度。'})
for suffix,text in {'title':'霜誓','description':'封存过去，走入尖塔。','unlockInfo':'霜誓者的旅途。','unlockText':'霜誓者','storyTitle':'霜誓'}.items():
    tables['epochs'][char+'_EPOCH.'+suffix]=text
tables['epochs']['frostsworn_character_frostsworn_character.title']='霜誓'
tables['static_hover_tips']={
    'FROSTSWORN_ECLIPSE.title':'日食 {Level}',
    'FROSTSWORN_ECLIPSE_REST.description':'恢复{Heal}点生命。{ExtraText}',
    'FROSTSWORN_ECLIPSE_MEND.description':'恢复{HasTarget:{Name}|其他角色}最大生命值的{Percent}%{HasTarget:（{Heal}）|}。',
    'FROSTSWORN_FATED.cardDescription':'[gold]【物语】下次打出重放{FatedReplays}。[/gold]',
    'FROSTSWORN_CARDPILE_COLD_STORAGE.title':'冷藏区',
    'FROSTSWORN_CARDPILE_COLD_STORAGE.description':'基础容量4，可扩至10。每回合正常抽牌后选择解冻1张。解冻的牌下次打出费用-1。',
    'FROSTSWORN_CARDPILE_COLD_STORAGE.empty':'冷藏区为空。使用雪藏存放手牌。',
    'FROSTSWORN_STORE.prompt':'选择要冷藏的其他手牌，也可以跳过。',
    'FROSTSWORN_STORE_REQUIRED.prompt':'选择要冷藏的其他手牌。',
    'FROSTSWORN_SEARCH.prompt':'选择要冷藏的牌。',
    'FROSTSWORN_THAW.prompt':'选择1张牌解冻：下次打出费用降低1。',
    'FROSTSWORN_THAW.cardDescription':'\n[blue]已解冻：仅下次打出费用降低1。[/blue]',
}
for key,name,desc in [
    ('SHATTER','碎冰','碎冰X：移除目标至多X层寒霜。至少移除1层才算成功碎冰，收益按实际移除层数计算。'),
    ('FROST','寒霜 / 冰封','出牌结算后，寒霜达到最大生命的12%（向上取整，初始限制为12～54）则清空并跳过下次行动，保留原意图。每次冰封后门槛增加最大生命的12%（向上取整，最低4、最高12）；再次冰封前须正常行动一次。不可打断的动作不能冰封。'),
    ('COLD','冷藏 / 解冻','基础容量4，上限10。冷藏牌离开手牌；下回合正常抽牌后选择解冻1张。手牌满时保留在冷藏区。解冻牌仅下次打出费用-1，不叠加。X费不减费。'),
    ('ARMOR','冰甲','后于格挡结算，每层抵挡1点伤害（只能抵挡伤害，不能抵挡“失去生命”）并消耗。每回合抽牌前融化，剩余冰甲减半（向上取整）。'),
    ('SELF','自霜','下回合抽牌前，每层使你失去1点生命，然后清除。无视格挡、冰甲，不能被人工制品抵消，可以致死。'),
    ('SNOW','雪势','回合结束对所有敌人造成等于层数的非攻击伤害，然后层数减半（向下取整）。'),
]:
    tables['static_hover_tips']['FROSTSWORN_'+key+'.title']=name
    tables['static_hover_tips']['FROSTSWORN_'+key+'.description']=desc
for key, text in tables['cards'].items():
    if key.endswith('.description'):
        tables['cards'][key] = re.sub(r'\{(Damage|Block|Amount|Extra|Third)\}', r'{\1:diff()}', text)
for cls,name,desc,spec in expansion.cards:
    upgraded=desc
    if cls=='AbsoluteBeam': upgraded=upgraded.replace('一半','全部')
    if cls=='IceRelease': upgraded=upgraded.replace('下X张','下X+1张')
    if cls=='WakeIce': upgraded=upgraded.replace('随机解冻','选择解冻')
    if cls in ('CrystalVolley','Glitter'): upgraded=upgraded.replace('冰晶','冰晶+')
    upgraded=re.sub(r'\{(Damage|Block|Amount|Extra|Third)\}',r'{\1:diff()}',upgraded)
    tables['cards']['FROSTSWORN_CARD_'+slug(cls)+'.upgradeDescription']=upgraded
# Match the native Shiv reference color, while the hover tip itself is a CardHoverTip.
for cls in ('FrostBite','ShieldCounter','ColdHammer'):
    for suffix in ('.description','.upgradeDescription'):
        key='FROSTSWORN_CARD_'+slug(cls)+suffix
        tables['cards'][key]=tables['cards'][key].replace('{Damage:diff()}','{CalculatedDamage:diff()}')
for cls in ('CrystalAmuletPower','SnowEyePower'):
    k='FROSTSWORN_POWER_'+slug(cls)+'.smartDescription'
    tables['powers'][k]=tables['powers'][k].replace('{Amount}','{EffectiveAmount}')
for cls,name,desc in [('FrostBottle','冰冰药水','给予{Amount}层寒霜。'),('LiquidNitrogen','液氮','冷藏至多3张手牌。'),('FractalSnowflake','分形雪花','本场战斗中，生成的冰晶获得锐利。')]:
    k='FROSTSWORN_POTION_'+slug(cls)
    tables['potions'].update({k+'.title':name,k+'.description':desc,k+'.selectionScreenPrompt':'选择要冷藏的牌。'})
for table_name in ('cards','powers','enchantments','relics','potions'):
    for key,text in list(tables[table_name].items()):
        if key.endswith(('.description','.upgradeDescription','.smartDescription','.extraCardText')):
            if table_name=='cards':
                # CardModel automatically renders these gameplay keywords in gold.
                # Strip only standalone keyword sentences, never effects such as
                # "消耗所有冰甲" or "消耗1张牌". Source data keeps them for exported docs.
                text='\n'.join(line for line in text.splitlines() if line.strip() not in ('消耗。','保留。','固有。')).strip()
            # EnergyVars retain upgrade values; fixed gains use the owning character's icon.
            text=re.sub(r'\{Amount(?::diff\(\))?\}点能量', '{Amount:energyIcons()}',text)
            text=re.sub(r'(\d+)点能量',lambda m:'{energyPrefix:energyIcons('+m.group(1)+')}',text)
            text=text.replace('并获得等于解冻牌数的能量','每解冻1张，获得{energyPrefix:energyIcons(1)}')
            # Color terms only: numbers continue to use native diff/preview colors.
            tables[table_name][key]=re.sub(r'(?:锐利)?冰晶\+?|消耗牌堆|抽牌堆|弃牌堆|手牌|消耗|格挡|力量|敏捷|伤害|易伤|虚弱|寒霜|碎冰|冰甲|自霜|雪势|冷藏|解冻',lambda m:'[gold]'+m.group(0)+'[/gold]',text)
for cls,line in {
    'ShellRecycle':'当前获得{LiveAmount:diff()}点[gold]格挡[/gold]。',
    'SnowFinale':'当前回复{LiveAmount:diff()}点生命。',
    'SnowUnsheathed':'当前给予{LiveAmount:diff()}层[gold]寒霜[/gold]。',
    'ColdReturn':'当前获得{LiveAmount:diff()}层[gold]冰甲[/gold]。',
    'SnowCharge':'翻倍后为{LiveAmount:diff()}层[gold]雪势[/gold]。',
    'PhaseShift':'当前目标可提供{LiveAmount:diff()}层[gold]冰甲[/gold]。',
}.items():
    for suffix in ('.description','.upgradeDescription'):
        key='FROSTSWORN_CARD_'+slug(cls)+suffix
        tables['cards'][key+'.combat']=tables['cards'][key]+'\n'+line
for suffix in ('.description','.upgradeDescription'):
    k='FROSTSWORN_CARD_ABSOLUTE_BEAM'+suffix
    tables['cards'][k+'.combat']=tables['cards'][k].replace('敌人[gold]寒霜[/gold]上限一半的','{BeamFrost:diff()}层').replace('敌人[gold]寒霜[/gold]上限全部的','{BeamFrost:diff()}层')
from english_localization import build_english
tables['static_hover_tips'].update({
    'FROSTSWORN_COLD_STATUS.description':'\u5f53\u524d\u51b7\u85cf\uff1a{Count}/{Capacity} \u5f20\n\u5f53\u524d\u5bb9\u91cf\u4e0a\u9650\uff1a{Capacity} \u5f20\uff08\u57fa\u78404\uff0c\u6700\u9ad810\uff09\u3002\n\u6bcf\u56de\u5408\u6b63\u5e38\u62bd\u724c\u540e\u9009\u62e9\u89e3\u51bb{Thaw}\u5f20\u3002\n\u89e3\u51bb\u4ec5\u4e0b\u6b21\u6253\u51fa\u51cf\u8d391\uff0c\u4e0d\u53e0\u52a0\u3002\u624b\u724c\u6ee1\u65f6\u7559\u5728\u51b7\u85cf\u533a\u3002',
    'FROSTSWORN_ARMOR_STATUS.description':'\u5f53\u524d\u51b0\u7532\uff1a{Amount}\n{Description}',
    'FROSTSWORN_ECLIPSE_UNRECORDED.title':'日食：未记录',
    'FROSTSWORN_ECLIPSE_QUOTE.description':'"你唯能在光亮处庆祝......全因我允许你这么做。"',
    **{f'FROSTSWORN_ECLIPSE_{i}.description':text for i,text in enumerate([
        '无特殊效果。','先古之民只会回复你已损失生命值的70%。','每一幕首个Boss战的前3层不可见。','初始金币-20。',
        '偶数回合开始时，敌人获得3点格挡。','从除先古之民外获得的回复量减少三分之一。','从事件所受的伤害增加50%。',
        '奇数回合开始时，敌人获得1点临时力量（不被人工制品抵消）。','友方受到永久伤害（最多50%），下一幕开始时恢复。'])},
    'FROSTSWORN_FROST_FROZEN.description':'已冰封：跳过下一次行动。',
    'FROSTSWORN_FROST_RECOVERY.description':'恢复期：正常行动一次后才能再次冰封。',
    'FROSTSWORN_FROST_UNINTERRUPTIBLE.description':'当前动作不可打断。',
    'FROSTSWORN_FROST_READY.description':'达到门槛后，在出牌结算结束时冰封。',
    'FROSTSWORN_FROST_STATUS.description':'当前寒霜：{Frost}\n当前冰封门槛：{Threshold}\n{Status}\n本场已冰封 {Count} 次；每次冰封后门槛增加12。',
})
english = build_english(tables)
for lang in ('zhs','eng','zht'):
    path=assets/'localization'/lang; path.mkdir(parents=True,exist_ok=True)
    for name,table in (english if lang=='eng' else tables).items(): (path/(name+'.json')).write_text(json.dumps(table,ensure_ascii=False,indent=2),encoding='utf-8')

def art(path,index,size=(512,384),icon=False):
    w,h=size
    im=Image.new('RGBA',size,(0,0,0,0) if icon else (13,29,47,255)); d=ImageDraw.Draw(im)
    cx,cy=w/2,h/2; radius=min(w,h)*.38
    if not icon:
        for y in range(h):
            d.line((0,y,w,y),fill=(12+int(y/h*10),28+int(y/h*20),45+int(y/h*27)))
        d.ellipse((cx-radius*1.15,cy-radius*1.15,cx+radius*1.15,cy+radius*1.15),outline=(45,85,109),width=3)
    spokes=6
    for j in range(spokes):
        a=math.pi*j/3-math.pi/2
        ex,ey=cx+radius*math.cos(a),cy+radius*math.sin(a)
        d.line((cx,cy,ex,ey),fill=(130,218,242),width=max(3,int(w*.014)))
        for ratio in (.5,.75):
            bx,by=cx+radius*ratio*math.cos(a),cy+radius*ratio*math.sin(a)
            for sign in (-1,1):
                da=a+sign*math.pi/3
                d.line((bx,by,bx-radius*.2*math.cos(da),by-radius*.2*math.sin(da)),fill=(194,242,255),width=max(2,int(w*.008)))
    # Distinct small crystals around the rim identify the prototype cards.
    for j in range(index%7+1):
        a=j*math.pi/3.5+index*.3
        x,y=cx+radius*.9*math.cos(a),cy+radius*.9*math.sin(a)
        r=w*.025
        d.polygon([(x,y-r),(x+r*.65,y),(x,y+r),(x-r*.65,y)],fill=(228,251,255))
    path.parent.mkdir(parents=True,exist_ok=True); im.save(path)
for i,(cls,_,_) in enumerate(cards): art(assets/'art'/(cls+'.png'),i)
for i,cls in enumerate([*powers,*expansion.relics,'cold','core']): art(assets/'icons'/(cls+'.png'),i,(128,128),True)
from power_icons import write_icons
write_icons(assets/'icons', list(powers))
from power_icons import SHAPES
potion_dir=assets/'potions';potion_dir.mkdir(exist_ok=True)
for cls,shape,color in [('FrostBottle','snow','#64D9FC'),('LiquidNitrogen','box','#8FEBC3'),('FractalSnowflake','crystal','#D3A1FF')]:
    svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><path d="M47 8H81V37Q111 58 106 99Q101 120 64 120Q27 120 22 99Q17 58 47 37Z" fill="#152C44" stroke="{color}" stroke-width="6"/><path d="M44 14H84" stroke="#D8C5A0" stroke-width="12"/><g transform="translate(18 31) scale(.72)" fill="none" stroke="{color}" stroke-width="7" stroke-linejoin="round">{SHAPES[shape]}</g></svg>'
    (potion_dir/(cls+'.svg')).write_text(svg,encoding='utf-8')
print(f'Generated {len(cards)} portraits and 3 localization sets.')
