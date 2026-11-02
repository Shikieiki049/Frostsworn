# 0.9.14：记忆蜕片选牌修复

## 问题与改动

其他效果（报告中为 Donquixote 流血）使用 ThrowingPlayerChoiceContext 造成生命损失时，记忆蜕片直接调用选牌，触发 NotImplementedException。

- 保留已经支持选择的原上下文。
- 对 ThrowingPlayerChoiceContext：玩家回合使用原版 HookPlayerChoiceContext，将选择及后续结算放在所属玩家的同步动作中；敌方回合使用原版 BlockingPlayerChoiceContext，等待选择完成再继续。
- 仍然只在每场战斗第一次损失生命时触发，查看抽牌堆顶三张、选一张加入手牌、消耗其余牌。
- 空/取消选择、战斗结束、角色死亡时不消耗候选牌；已被其他效果移走的牌不被重新拉回。
- 没有修改随机数协议、日食规则、原版动作取消机制或任何外部模组。

## 验证

先用 0.9.13 基线及真实原版选牌信号在隔离 Godot 引擎中复现异常，再应用修复。

正式版 0.107.1 与测试版 0.111.0 分开编译，使用各自游戏 DLL 和对应 RitsuLib 参考程序集。八组定向检查共 **423 条断言**通过，未记录引擎错误：

| 目标 | 选择修复/手动冰晶 | 虚空遗物 | 冰晶与报告回归 | 既有网络兼容 |
|---|---:|---:|---:|---:|
| 0.107.1 | 23 | 106 | 50 | 32 |
| 0.111.0 | 27 | 103 | 50 | 32 |

新增测试不再用全局选择器跳过选牌信号：测试版仅替代本地界面，正式版通过原版远端选择回放完成 ReserveChoiceId / Signal / WaitForRemoteChoice。覆盖敌方回合阻塞、原有上下文、玩家钩子动作 GatheringPlayerChoice → Finished、远端选择编号与牌堆、重复受伤、取消、候选牌被移走及战斗结束后才返回选择。

这属于引擎内动作队列与远端结果回放测试，并非两台电脑联机实测。引擎资源包仍使用本地 0.111.0 资源，但正式版 API 编译/运行引用为独立 0.107.1 快照。

## 未宣称修复的报告

“冰晶动作已取消但任务仍完成”未在干净环境中复现。两套版本均用原版 PlayCardAction 连续出牌四次，动作完成且牌进入消耗堆。新增这些检查防止普通出牌回归，不能据此宣称外部模组组合下的取消故障已经解决。

冻结时刻 / RankUpCards2 的适用条件兼容问题尚未复现，没有强制绕过原版附魔合法性检查。

## 构建与发布边界

版本为 0.9.14；正式版和测试版的 DLL 不同，build-target.txt 分别为 0.107.1、0.111.0。内容校验与哈希见 choice-fix-0914.json。PCK 内容未变，不重新生成美术资源。独立安装包与源码已同步，2026-11-02 已提交 Steam 创意工坊，见下方核验记录。

## 可用于后续工坊发布的改动说明

中文：0.9.14：修复记忆蜕片在流血等间接失去生命效果下选牌报错；使用原版同步选牌动作；补充取消选择、战斗结束及候选牌移动保护。正式版与测试版分别验证。冰晶的特定动作取消问题仍待复现。

English: 0.9.14: Fixed Memory Slough failing to request a card choice after secondary HP loss such as Bleed. Uses native synchronized choice actions and guards cancelled selections, ended combat, and cards moved during selection. Stable and beta builds verified separately. The reported Ice Crystal action-cancellation issue remains under investigation.

## 工坊发布核验（2026-11-02）

测试版16:32、正式版16:33中英改动说明公开，分别关联public-beta与public。Steam范围记录15条，双语标题及Ritsu依赖保留。两次SDK提交返回Timeout，未重复上传。当前public-beta实际下载返回OK，四个内容文件哈希与测试版0.9.14完全一致，build-target.txt为0.111.0。正式版本次未切分支下载，双机实测和双向自动分发不据此宣称通过。
