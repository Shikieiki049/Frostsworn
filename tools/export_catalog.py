"""Export the current card data; does not change game assets."""
from pathlib import Path
import collections, json, re, sys
from cards_data import cards
from powers_data import powers
from version_info import version
VERSION = version()

root = Path(__file__).resolve().parents[1]
out = Path(sys.argv[1]) if len(sys.argv)>1 else root.parent/'outputs'
out.mkdir(parents=True, exist_ok=True)
groups=json.loads((root/'tools/catalog_groups.json').read_text('utf-8'))
order=['寒霜','冰晶','自霜','冰甲','冷藏','解冻','雪势','通用']
rows=[]
for cls,name,desc,spec in cards:
    values={k:int(v) for k,v in re.findall(r'(\w+): (-?\d+)',spec)}
    for key in ['Damage','Block','Amount','Extra','Third']:
        n=values.get(key,0);u=values.get(key+'Upgrade',0)
        desc=re.sub(r'\{'+key+r'(?::diff\(\))?\}',str(n)+(f'（{n+u}）' if u else ''),desc)
    if 'RemoveExhaustUpgrade: true' in spec:desc=desc.replace('消耗。','消耗（升级后去掉消耗）。')
    if 'RetainUpgrade: true' in spec:desc+='\n升级后保留。'
    if 'Unplayable: true' in spec:desc='不能被打出。\n'+desc
    if 'Ethereal: true' in spec:desc+='\n虚无。'
    if 'InnateUpgrade: true' in spec:desc+='\n升级后固有。'
    if cls=='AbsoluteBeam':desc=desc.replace('一半','一半（升级后为全部）')
    if cls=='IceRelease':desc=desc.replace('下X张','下X（X+1）张')
    if cls=='WakeIce':desc+='\n升级后选择解冻。'
    if cls in ['Glitter','CrystalVolley']:desc=desc.replace('冰晶','冰晶（升级后为冰晶+）')
    assert '{' not in desc,(cls,desc)
    cost=int(spec.split(',')[0]);up=values.get('CostUpgrade',0)
    cost='X' if 'CostsX: true' in spec else '—' if cost<0 else str(cost)+(f'（{cost+up}）' if up else '')
    kind={'Attack':'攻击','Skill':'技能','Power':'能力','Status':'状态','Ancient':'先古'}[re.search(r'CardType\.(\w+)',spec)[1]]
    rarity={'Basic':'初始牌','Token':'生成牌','Common':'普通','Uncommon':'罕见','Rare':'稀有','Status':'状态','Ancient':'先古'}[re.search(r'CardRarity\.(\w+)',spec)[1]]
    primary=groups[cls];tags={primary}
    for tag in order[:-1]:
        if tag in desc:tags.add(tag)
    if '碎冰' in desc or '冰封敌人' in desc:tags.add('寒霜')
    rows.append(dict(cls=cls,name=name,desc=desc,cost=cost,kind=kind,rarity=rarity,primary=primary,tags=[g for g in order if g in tags]))
counts=collections.Counter(c['kind'] for c in rows)
assert counts=={'攻击':33,'技能':38,'能力':21,'状态':1}
parts=[f'''# 霜誓者 {VERSION} 卡牌总览

合并本轮攻击、技能与能力牌修改。共 93 张牌：33 张攻击、38 张技能、21 张能力、1 张衍生状态牌。旧相变及两个衍生选项已移除，新的相变列于技能牌。锐利冰晶是冰晶的附魔版本，不重复计数。

括号内为升级后的数值；未改变的数值不重复标注。每张牌按主要体系全文展示一次，并标注关联体系，文末提供跨体系索引。

卡池归属：角色卡池89张；冰晶、耗尽位于衍生卡池；命定的物语、极冻光束位于原版先古之民使用的事件卡池。\n\n初始遗物：封冬之核，每场战斗开始时给予所有敌人 3 层寒霜。
初始牌组：4 张打击、4 张防御、1 张冻结射线、1 张深埋。冻结射线造成 3（4）点伤害，给予 10（11）层寒霜。深埋获得 5（8）点格挡，从抽牌堆选择至多 1 张牌冷藏。

## 机制

寒霜：达到冰封门槛时清空并跳过敌人下次行动。初始门槛为最大生命 12% 向上取整，最低 12、最高 54；每次冰封后增加最大生命的 12%（向上取整，最低 4、最高 12），两次之间须正常行动一次。人工制品可抵消寒霜。

碎冰 X：移除目标至多 X 层寒霜，按实际移除层数计算收益。

冰甲：格挡后抵挡攻击伤害及可格挡的非攻击伤害（如紧缠），消耗对应层数；不抵挡自霜等直接失去生命。抽牌前融化，剩余冰甲减半（向上取整，例如3层剩2层）；冰川形态阻止自然融化。

自霜：下回合抽牌前失去等量生命并清除；格挡、冰甲、人工制品不能抵消。

雪势：回合结束时对所有敌人造成等量非攻击伤害，随后减半，向下取整；永夜风暴改变衰减。

冷藏：基础容量 4，最高 10。默认回合开始正常抽牌后解冻 1 张；速释装置与冻结心愿增加数量。

解冻：回到手牌，仅下一次打出费用 -1；X 费不减费，手牌满则保留在冷藏区。热交换、积雪和冰窖在实际解冻时触发。同一张牌可以在同一回合多次冷藏和解冻，减费不叠加。

锐利：新附魔，这张牌施加 1 层易伤。晶莹生成附有锐利的冰晶，升级后生成锐利冰晶+。

本轮结算：伤害转换按原版拳斗口径计算，包含格挡吸收与溢出伤害。战斗中会显示部分公式的当前结果，霜噬、冰盾反击、冰库重锤使用原版计算伤害变量。霜噬统计本场战斗实际成功施加寒霜的次数（含自身先施加的寒霜）；雪、出鞘统计本回合实际给敌人施加的负面状态层数，包含寒霜，人工制品阻挡的不计入。冰断对每个敌人先造成基础伤害，再施加寒霜，消耗该目标全部寒霜并造成追加伤害。白夜奔袭遵守冷藏容量，冷藏区满时正常弃置。耗尽以任何方式进入手牌时立即阻止本回合抽牌，包括抽取、生成、检索和解冻；抽到耗尽会中断本次剩余抽牌。

静雪冥想的延迟效果：使用原版下回合能量状态，另有下回合冰甲状态。
''']
for kind in ['攻击','技能','能力','状态']:
    parts.append(f'## {kind}牌（{counts[kind]} 张）\n')
    for group in order:
        subset=[c for c in rows if c['kind']==kind and c['primary']==group]
        parts.append(f'### {group}体系（{len(subset)} 张）\n')
        if not subset:parts.append('暂无以此为主要体系的牌；跨体系关联见文末索引。\n')
        for c in subset:
            parts.append(f"#### {c['name']}｜{c['cost']}费｜{c['rarity']}\n\n关联体系：{'、'.join(c['tags'])}。\n\n"+c['desc'].replace('\n','  \n')+'\n')
parts.append("""## 先古联动与专属药水

尘封魔典（达弗）：给予先古能力牌「命定的物语」。

古老牙齿（欧洛巴斯）：将冻结射线替换为极冻光束，继承升级与附魔。极冻光束使用目标当前冰封门槛，普通版取一半并向上取整，升级版取全部；指向敌人时在描述中显示实际数值。

欧洛巴斯之触（欧洛巴斯）：将封冬之核替换为先古遗物「至冬核心」。每场战斗开始时给予所有敌人5层寒霜，每回合给予所有敌人2层寒霜。第一回合两项效果均触发，合计7层。

霜誓者独有药水：
- 冰冰药水｜普通：给予一个敌人16层寒霜。可被人工制品抵消，达到门槛立即触发冰封。
- 液氮｜罕见：选择最多3张手牌冷藏，受当前冷藏容量限制。
- 分形雪花｜稀有：本场战斗中，生成的所有冰晶获得锐利附魔。与冰晶自动升级兼容，已有锐利不重复叠加。

本轮联动说明：
- 命定的物语生效时，解冻牌顶部显示金色“【物语】下次打出重放N”一次性标识；N为实际重放次数，打出后移除，再次解冻后重新出现。
- 白夜奔袭每实际进入冷藏区一次，本场战斗永久降低1费，最低0费；解冻本身的下次打出减费另行计算。
- 冰释作用于接下来打出的非X费解冻牌，含已在手里的解冻牌。每张牌消耗一次额度，重放不额外消耗额度。X费牌不受冰释影响，也不消耗冰释额度。
- 守护、前行分别获得X次冰甲、X次雪势；祝福之风与铭刻于心在每次获得时应用。
- 冰封时刻分别从手牌、弃牌堆、抽牌堆选取，三个来源共享剩余冷藏容量。
- 能力牌正文的冰甲、雪势数值只显示基础／升级数值，不受敏捷、力量影响；实际收益仍应用属性加成。
- 冰晶护符与雪眼的状态层数保存基础收益，状态悬停描述显示加上当前敏捷／力量后的每次收益，不将属性重复计入能力层数。

""")
parts.append('## 跨体系索引\n\n索引允许同一张牌出现多次，卡牌总数不重复计算。\n')
for kind in ['攻击','技能','能力','状态']:
    parts.append(f'### {kind}牌\n')
    for group in order:
        names=[c['name'] for c in rows if c['kind']==kind and group in c['tags']]
        parts.append(f"- {group}："+('、'.join(names) if names else '无')+'。\n')
parts.append('## 状态说明\n\nX 表示当前状态层数或效果量。\n')
for cls,(name,desc) in powers.items():
    desc=desc.replace('{Amount}','X').replace('{Decay}','X')
    parts.append('### '+name+'\n\n'+desc+'\n')
md='\n'.join(parts)
assert len(re.findall('^#### ',md,re.M))==93
stem=f'霜誓者_{VERSION}_卡牌总览_分类版'
(out/(stem+'.md')).write_text(md,'utf-8-sig')
(out/(stem+'.txt')).write_text(re.sub(r'^#{1,4} ','',md,flags=re.M).replace('  \n','\n'),'utf-8-sig')
print('Exported 93 cards: 33 attacks, 38 skills, 21 powers, 1 status.')


