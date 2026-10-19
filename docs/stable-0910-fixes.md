# 0.9.10 正式版联机与出牌故障

目标为 public / 游戏 0.107.1、RitsuLib 0.6.7。测试版 public-beta / 0.111.0 保留已发布的 0.9.8 DLL/PCK，只有发布目录清理。

## 实际诊断

副机日志在涅奥前收到 SyncRngMessage 时，SerializableRelicGrabBag.Deserialize 经 ReadModelIdAssumingType 报模型编号越界：读取 2386，本地表有 2108 项。后续停留开局同步。日志能确定出错路径，不能仅凭两个数字确定双方完整表差异或原始位流错位原因。此前 0.9.9 原生快照测试仅使用相同编号表，确实漏掉此类问题。

正式版快照改用 FSR2：显式固定宽度计数、全部 RNG 计数器、可空种子、完整遗物分类/名称、稀有度、顺序。未知实际遗物、错误标记、异常数量、重复键均拒绝，成功完成后才赋值消息；没有丢弃遗物或绕过原版内容校验。实际存储的 ModelId hash 在 Init 所有 postfix 后加入协议标记，直接读取与内联读取一致。新旧协议不允许混联，所有玩家更新后完全重启。

另一次主机日志显示 Loadout 调试召唤 Crusher/Rocket 时 AfterAddedToRoom 失败却遗留半初始化怪物，随后卡牌伤害经 Rocket.AfterCurrentHpChanged 报空引用，PlayCardAction 未正常结束，卡牌留在中央。两种怪物依赖 Kaiser Crab 专用背景。新增正式版保护只在 Loadout 的这一召唤路径且缺少背景时，于 CreatureCmd.Add 修改战斗前拒绝；正常 Boss 战不变。没有吞掉一般出牌异常或强制丢牌。已经损坏的战斗需要重新进入有效战斗。

YuYuYu 的 WithFilter 缺失接口异常是另一个奖励构建问题，不是本次涅奥前黑屏路径，本模组未替其他模组改写该接口。

## 回归

- 实际 PacketWriter/Reader 和 Harmony 路径保存快照后，替换接收表内容、数量与位宽（11、12、13 位），完整保留 RNG 状态及所有原生/虚空遗物顺序；原生旧格式作为负对照确实失败或改变结果。
- 真实 Init 重复执行、直接 hash 存储读取、重复 Apply 不反复异或；异常包及未知遗物原子拒绝。
- 测试专用 Loadout 命名调用者经真实 CreatureCmd.Add 验证两种调试召唤被阻止且战斗未变；随后真实自动打出打击、冻结射线、冰晶均完成并离开出牌堆。未加载第三方 Loadout DLL 完成 UI 操作。
- 正式版与测试版发布目录递归扫描，所有 JSON 必须只有唯一合法 mod_manifest.json，使用原生清单解析器检查；移除误放的 release-target.json，改 TXT。
- 运行既有历史回归，结果另附 stable-0910-results.json。

这是单机引擎回归，尚不能代替两台电脑在原模组组合下的 Steam 联机复测；也不宣称支持双方不同玩法模组或版本。本次未重编译测试版 DLL。

最终本地回归：28 组、5849 项断言全部通过，引擎 ERROR 为 0。
