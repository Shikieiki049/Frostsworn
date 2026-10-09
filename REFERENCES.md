# 原版接口与资料

- [Spire Codex](https://github.com/ptrlrd/spire-codex)：解包流程、卡牌与事件数据结构的参考；项目说明涵盖动态变量和多页事件，不假设所有页面都只使用 INITIAL 参数。
- 目标版本游戏 DLL：核对真实类型签名与 UI 节点行为。原版程序集和反编译产物不提交到本仓库。
- RitsuLib 0.6.7：注册卡牌、遗物、角色、冷藏牌堆，以及存档/多人通信。

已有复用点：

| 功能 | 原版接口 |
| --- | --- |
| 地图笔迹颜色 | `CharacterModel.MapDrawingColor` |
| 缩略卡组与稀有度横条 | `NTinyCard.SetCard` / `NTinyCard.Set` |
| 战斗能量及悬停说明 | 原版 energy counter 场景 |
| 事件生命数值预览 | `NEventOptionButton` 最终文本；使用实际日食计算函数 |
| 遗物触发特效 | `NRelicFlashVfx` / `NUiFlashVfx` |

参考网站的数据版本可能与安装游戏不同，最终以支持版本 0.111.0 的本地签名为准。本次未复制 Spire Codex 的实现代码或原版资源到仓库。
