# 霜誓者 · 两版攻击动作

打开 `攻击动画演示.html` 即可离线播放。切换“魔法阵施法”和“挥杖敲击”，可重播、暂停、半速检查、拖动时间和导出 GIF。灰色圆盘只是预览中的接触标记。

- `attack_magic`（2.10 秒）：抬杖蓄能、展开冰系魔法阵、向前释放法术、后坐、收势。法术释放事件为 `spell_release`，时间 1.05 秒。
- `attack_melee`（1.85 秒）：向后蓄力、抬杖、加速挥击、短暂停顿、随势、收杖。命中事件为 `melee_hit`，时间 0.75 秒。

人物使用 19 根骨骼、18 个刚性附件和两组落脚 IK。肩与肘分别转动，手和法杖沿同一关节链运动；脸部、手部、服装纹样和法杖不做加权拉伸。原始尺寸不一致的独立素材在装配时固定等比缩放，动画过程保持其比例。胸口、腰带和下摆共用一块连续附件，避免腰部断开；法杖位于衣物前方、握拳手指后方，杖杆中心与握拳位置绑定。

GPT 图像编辑补齐了身体被头发及部件遮挡的部分，提示词保存在 `image-edit-prompt.txt`。头部与后发使用原立绘的 UV 多边形选区，双臂、手、鞋和法杖沿用用户提供的部件。腿部补图未成功，使用原立绘腿部选区配合关节重叠和 IK。所有 PNG 保持原有 alpha，没有通过 Python 修改图像。

## 文件与检查

- `frostsworn-attacks.json`、`frostsworn-attacks.atlas`、`assets/`：Spine 4.2 动画工程。
- `build_attack.py`：生成动画和自包含演示页，依赖 Python 与 Pillow（仅查询尺寸）。
- `preview-template.html`：最简播放器及法阵、冰系法术、挥击残影和命中特效源码。
- `validate_attack.cjs`：通过实际 Spine runtime 检查两段动作共 239 个采样帧。附件内部长度误差小于 0.002 像素、双鞋保持固定、动作结束回到起始姿势；IK 脚踝误差约 1.24 像素（低于预览中的一个像素）。

浏览器检查了施法准备/释放、挥击蓄力/接触以及收势。此版本是独立演示，尚未替换游戏内动作。人物动画已导出为 Spine JSON；演示特效由播放器绘制，游戏接入时需要按事件实现对应特效。

Spine 官方 runtime 为 `@esotericsoftware/spine-webgl@4.2.120`，GIF 编码为 `gifenc@1.0.3`，附各自许可证。Skeleton 版本为 4.2.43。


Revision: the neck alignment matches the original head; a shoulder mantle covers both rotating sleeve roots. The shaft is in front of clothing and behind gripping fingers. Chest, belt and hem share a continuous rigid attachment. Validation also checks draw order and the hand/staff grip at all 239 sampled frames.
