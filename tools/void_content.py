"""Void Cradle content and localization. Kept separate from the character's reward card pool."""
import re
from english_localization import gold

def highlight(text, eng):
 return gold(text) if eng else re.sub(r"抽牌堆|弃牌堆|手牌|消耗|格挡|力量|敏捷|活力|升级", lambda m:"[gold]"+m.group(0)+"[/gold]",text)

RELICS=[
('BloodlessCord','无血脐带','Bloodless Cord','每攀爬一层楼层，回复1点生命。','Whenever you climb a floor, heal 1 HP.','小血瓶','Blood Vial'),
('DrownedFloat','溺者浮囊',"Drowner's Float",'每场战斗中，第一次将对你造成生命损失的敌方攻击伤害降低15。','Once per combat, reduce the first enemy Attack that would cause you to lose HP by 15.','锚','Anchor'),
('RuminationSac','反刍胃袋','Ruminating Sac','每场战斗第一次洗牌后，抽3张牌。','After your first shuffle each combat, draw 3 cards.','准备背包','Bag of Preparation'),
('MirrorParasite','镜壳寄生虫','Mirror-Shell Parasite','每回合第一次有敌方攻击消耗你的格挡时，对攻击者造成等于该次消耗格挡的伤害，最多11点。','The first time each turn an enemy Attack uses your Block, deal damage to the attacker equal to the Block used, up to 11.','铜质鳞片','Bronze Scales'),
('MemorySlough','记忆蜕片','Memory Slough','每场战斗第一次损失生命时，查看抽牌堆顶3张牌。选择1张加入手牌，消耗其余牌。','The first time you lose HP each combat, look at the top 3 cards of your Draw Pile. Put 1 into your Hand and Exhaust the rest.','百年积木','Centennial Puzzle'),
('LightMothCocoon','食光蛾茧','Light-Eater Cocoon','每场战斗开始时，选择1张手牌。其本回合耗能降低2，并在本回合获得消耗。','At the start of each combat, choose a card in your Hand. It costs 2 less and gains Exhaust this turn.','灯笼','Lantern'),
('CurledPolyp','蜷缩息肉','Curled Polyp','敌人回合结束时，回复4点生命。受到来自敌人的生命伤害后，本场战斗此遗物失效。','At the end of the enemy turn, heal 4 HP. After you lose HP to an enemy, this relic is disabled for the rest of combat.','奥利哈钢','Orichalcum'),
('TriocularWax','三瞳蜡瘤','Three-Eyed Wax Growth','在你的第2回合开始时，随机3张手牌在本回合耗能降低1。','At the start of your 2nd turn, 3 random cards in your Hand cost 1 less this turn.','烛台','Candelabra'),
('FaceborrowMembrane','借颜唇膜','Borrowed-Face Membrane','每场战斗开始时，获得3点力量和3点敏捷。这些加成在你的每个回合结束时分别减少1点。','At the start of each combat, gain 3 Strength and 3 Dexterity. Lose 1 of each bonus at the end of each of your turns.','闪亮口红','Sparkling Rouge'),
('MeristemTeeth','分生凿齿','Budding Incisors','每场战斗开始时，选择抽牌堆中的2张牌，在本场战斗中升级。然后选择其中1张置入弃牌堆。','At the start of each combat, choose 2 cards in your Draw Pile to Upgrade for this combat. Put 1 of them into your Discard Pile.','碎石钻','Stone Cracker'),
('HangingStarSac','倒悬星囊','Inverted Star Sac','在你的第4回合开始时，所有手牌在本回合耗能变为0。','At the start of your 4th turn, all cards in your Hand cost 0 this turn.','吊灯','Chandelier'),
('MarrowTouch','寄髓之触','Marrow-Tethered Touch','战斗胜利时，若你的生命值至少为最大生命值的50%，获得5点最大生命值。','At the end of combat, if you have at least 50% of your Max HP, gain 5 Max HP.','带骨肉','Meat on the Bone'),
('MoldyCookie','发霉曲奇','Moldy Cookie','每2场战斗，该场战斗的所有卡牌奖励都获得升级。','Every 2nd combat, all card rewards from that combat are Upgraded.','吃不完的糖','Lasting Candy'),
('GymnospermPendant','裸子植物吊坠','Gymnosperm Pendant','使用药水时，获得2点敏捷。这些加成在你的每个回合结束时减少1点。','Whenever you use a Potion, gain 2 Dexterity. Lose 1 of this bonus at the end of each of your turns.','爬行动物饰品','Reptile Trinket'),
('OverdriveGearbox','超变速箱','Overdrive Gearbox','每打出7张牌，获得等于当前自身回合数两倍的活力，最多20点。','Every 7 cards you play, gain Vigor equal to twice your current turn number, up to 20.','音叉','Tuning Fork'),
('LighthouseJelly','灯塔水母','Lighthouse Jellyfish','当你即将死亡时，若最大生命值大于10，回复最大生命值的15%，获得16点格挡，并失去11点最大生命值。','When you would die, if you have more than 10 Max HP, heal 15% of your Max HP, gain 16 Block, and lose 11 Max HP.','蜥蜴尾巴','Lizard Tail'),
]
def slug(s):return re.sub(r'(?<!^)(?=[A-Z])','_',s).upper()
def apply(zh,en):
 for tables,eng in [(zh,False),(en,True)]:
  for cls,zname,ename,zdesc,edesc,zorig,eorig in RELICS:
   key='FROSTSWORN_RELIC_'+slug(cls)
   tables['relics'].update({key+'.title':ename if eng else zname,key+'.description':highlight(edesc+'\nDisables '+eorig+'.',True) if eng else highlight(zdesc+'\n使'+zorig+'无效化。',False),key+'.flavor':'Something familiar lives inside. Something else looks back.' if eng else '熟悉的轮廓里，栖息着陌生的东西。'})
  tables.setdefault('events',{})
  ev='FROSTSWORN_EVENT_VOID_CRADLE'
  text={
  'title':('Void Cradle','虚空摇篮'),
  'pages.INITIAL.description':('As you pass through a dim cave, you glimpse a mysterious deep-purple glow in the darkness.\n\nDrawing closer, you discover a [purple][jitter]Void Cradle[/jitter][/purple]!\n\nA black-hole-like orb floats above it. There seem to be [purple][jitter]rare treasures[/jitter][/purple] inside.\n\nBut you also notice [red][jitter]insects[/jitter][/red] sleeping within the cradle. You would rather not wake them.','在穿过一个幽暗的洞穴时，你瞥见暗处散发的深紫色神秘光芒。\n\n当你靠近时，发现这居然是一个[purple][jitter]虚空摇篮[/jitter][/purple]！\n\n你注意到其上方有个黑洞般的球体，里面似乎有些[purple][jitter]珍奇之物[/jitter][/purple]。\n\n但同时你也看到摇篮内部沉睡的[red][jitter]虫子[/jitter][/red]，你可不想唤醒它们。'),
  'pages.INITIAL.options.TAKE.title':('Touch','触碰'),
  'pages.INITIAL.options.TAKE.description':('Obtain [red]Putrefaction[/red]. Obtain a random [color=#B780B0]Void Relic[/color].','获得一张[red]朽败[/red]。获得一件随机的[color=#B780B0]虚空遗物[/color]。'),
  'pages.INITIAL.options.CORRUPT.title':('Offer a Relic','献出遗物'),
  'pages.INITIAL.options.CORRUPT.description':('Obtain [red]Putrefaction[/red]. Corrupt a random eligible relic you own into its corresponding [color=#B780B0]Void Relic[/color]. Requires an eligible relic.','获得一张[red]朽败[/red]。随机选择一件已拥有且有对应虚空版本的遗物，将其腐化为对应的[color=#B780B0]虚空遗物[/color]。'),
  'pages.INITIAL.options.LEAVE.title':('Ignore and Leave','无视并离开'),
  'pages.INITIAL.options.LEAVE.description':('Leave the cradle untouched.','不触碰摇篮。'),
  'pages.TAKE.description':('You reach into the orb and pull out [purple][jitter]something[/jitter][/purple]!\n\n[red]Ouch[/red]! It feels like insects have crawled onto you.','你将手伸进球体，拿出了[purple][jitter]什么东西[/jitter][/purple]！\n\n[red]好痛[/red]！似乎有虫子钻到你的身上了。'),
  'pages.CORRUPT.description':('The orb swallows your relic. Moments later, a [purple]brand-new relic[/purple] falls to the ground.\n\nAs you reach for it, the [red][jitter]insects hiding underneath[/jitter][/red] immediately crawl onto you!','球体吞噬了你的遗物。片刻之后[purple]崭新的遗物[/purple]掉在地上。\n\n你伸手去拿，躲在底下的[red][jitter]虫子[/jitter][/red]立刻爬到了你的身上！'),
  'pages.LEAVE.description':('You quietly walk away. You recognize those [red]insects[/red]. They are not easy to deal with.','你悄悄离开了。你认识这些[red]虫子[/red]，它们可不好处理。')}
  tables['events'].update({ev+'.'+k:v[0 if eng else 1] for k,v in text.items()})
  card='FROSTSWORN_CARD_VOID_INFECTION'
  tables['cards'].update({card+'.title':'Putrefaction' if eng else '朽败',card+'.description':'When this card is [gold]Exhausted[/gold], lose 5 HP.' if eng else '这张牌被[gold]消耗[/gold]时，失去5点生命。'})
  tips=tables['static_hover_tips'];tips['FROSTSWORN_VOID_RARITY.title']='Void Relic' if eng else '虚空遗物';tips['FROSTSWORN_VOID_COLLECTION.title']='[color=#B780B0][font_size=28][b]Void:[/b][/font_size][/color] Corrupted relics obtainable only through special means.' if eng else '[color=#B780B0][font_size=28][b]虚空：[/b][/font_size][/color]只能通过特殊途径获得的腐化遗物。';tips['FROSTSWORN_VOID_PICK.prompt']='Choose a card.' if eng else '选择一张牌。'
  for power,stat in [('STRENGTH','力量'),('DEXTERITY','敏捷')]:
   k='FROSTSWORN_POWER_VOID_FADING_'+power+'_POWER'
   tables['powers'][k+'.title']=('Fading '+power.title()) if eng else '衰减'+stat
   tables['powers'][k+'.description']=('At the end of your turn, lose 1 '+power.title()+' from this bonus, then reduce this by 1.') if eng else '你的回合结束时，失去此加成提供的1点'+stat+'，然后层数减少1。'
