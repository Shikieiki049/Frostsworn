# 虚空摇篮（0.9.0）

- 事件加入全角色共享事件集合，各玩家独立选择；随机使用事件自身的原版确定性随机数。
- 探入摇篮：获得1张朽败和1件尚未持有的随机虚空遗物。
- 献出遗物：获得1张朽败，随机把自己持有且存在对应关系的1件原遗物替换为虚空版本。无候选时锁定。
- 离开：无代价。
- 朽败：诅咒，1费；被任何方式消耗时失去5点生命；消耗、永恒。不能升级。
- 虚空为独立动态稀有度，字体 #B780B0。所有虚空遗物拒绝普通奖励、商店和原版奖励抓取。
- 获取虚空版本时，从全队及共享奖励袋移除原遗物，并把排除记录写入本局附加存档；之后失去虚空版本也不会重新掉落原遗物。
- 同一玩家同时持有原版与虚空版本时，仅禁用对应原版的战斗和非战斗钩子；原遗物保留在栏内并显示失效。其他玩家持有的原版仍正常生效。
- 所有角色都可获得。联机双方均需更新到0.9.0；测试使用双玩家RunState及原版/RitsuLib存档流程，不等同于完整真人联机实测。
- 衰减力量/敏捷在自己的回合结束时各减少1，直至相应临时加成耗尽。
- 发霉曲奇按获取之后的战斗胜利计数，第2、4、6…场的卡牌奖励升级；不会因进入房间或SL反复累加。

|虚空遗物|对应原遗物|效果|
|---|---|---|
|无血脐带|小血瓶|每攀爬一层楼层，回复1点生命。|
|溺者浮囊|锚|每场战斗中，第一次将对你造成生命损失的敌方攻击伤害降低15。|
|反刍胃袋|准备背包|每场战斗第一次洗牌后，抽3张牌。|
|镜壳寄生虫|铜质鳞片|每回合第一次有敌方攻击消耗你的格挡时，对攻击者造成等于该次消耗格挡的伤害，最多11点。|
|记忆蜕片|百年积木|每场战斗第一次损失生命时，查看抽牌堆顶3张牌。选择1张加入手牌，消耗其余牌。|
|食光蛾茧|灯笼|每场战斗开始时，选择1张手牌。其本回合耗能降低2，并在本回合获得消耗。|
|蜷缩息肉|奥利哈钢|敌人回合结束时，回复4点生命。受到来自敌人的生命伤害后，本场战斗此遗物失效。|
|三瞳蜡瘤|烛台|在你的第2回合开始时，随机3张手牌在本回合耗能降低1。|
|借颜唇膜|闪亮口红|每场战斗开始时，获得3点力量和3点敏捷。这些加成在你的每个回合结束时分别减少1点。|
|分生凿齿|碎石钻|每场战斗开始时，选择抽牌堆中的2张牌，在本场战斗中升级。然后选择其中1张置入弃牌堆。|
|倒悬星囊|吊灯|在你的第4回合开始时，所有手牌在本回合耗能变为0。|
|寄髓之触|带骨肉|战斗胜利时，若你的生命值至少为最大生命值的50%，获得5点最大生命值。|
|发霉曲奇|吃不完的糖|每两场战斗，该场战斗的所有卡牌奖励都获得升级。|
|裸子植物吊坠|爬行动物饰品|使用药水时，获得2点敏捷。这些加成在你的每个回合结束时减少1点。|
|超变速箱|音叉|每打出7张牌，获得等于当前自身回合数两倍的活力，最多20点。|
|灯塔水母|蜥蜴尾巴|当你即将死亡时，若最大生命值大于10，回复最大生命值的15%，获得16点格挡，并失去11点最大生命值。|

## 验证

Tests/Void090.cs 在独立Godot测试宿主验证全部16件遗物注册、图标64/256px、牌效、事件双诅咒代价、奖励排除、共存失效、存档恢复，以及实际原版图鉴分类和逐件详情展示。

## 美术

使用用户提供的事件图、诅咒图和两张遗物图集。内置图像编辑工具只用于透明背景处理；tools/prepare_void_art.py 按从左到右、从上到下切片，统一透明画布和图标尺寸。


## 0.9.1 display fixes

- Slice whole alpha components before centering; do not clip icons at regular grid edges. Regenerate with `tools/prepare_void_art.py`.
- Native 28px bold collection header, explanatory text, purple rarity only, default name color, native stat/keyword hover tips, separate counterpart suppression line.
- Putrefaction keeps automatic Exhaust/Eternal keyword lines; the rules sentence highlights Exhaust.
- Event artwork occupies the left panel; it retains its aspect ratio and cannot overlap the native text/options column.
- Moldy Cookie displays completed combats modulo 2 (0 → 1 → 0), preserving saved combat totals and every-second-combat rewards.
- Focused engine tests cover all 16 inspection entries, final native Curse text, keyword tips, repeat counter cycles and native event visual-node layout. Rendered screens were inspected; this is not a live multiplayer playthrough.


## 0.9.3 event presentation and relic numeric colors

Removes the incorrect 0.9.1 portrait-panel override. The native full-screen event portrait dimensions, scaling and text/option layout are preserved; the backdrop is recomposed with the cradle/orb on the left and a dark continuous cave on the right. Uses the requested rewritten Chinese narrative, matching English, and native purple/red jitter tags. Three choices retain their existing costs/effects.

`tools/relic_numbers.py` applies native blue numeric tags to all 25 relic descriptions after localization generation; healing and Max HP amounts/thresholds use native green, including mixed green healing and blue Block in Lighthouse Jellyfish. BBCode parameter digits and localization placeholders remain untouched. Native UI render checks include all three option buttons and result pages; network vote display is mocked only within the test host.
