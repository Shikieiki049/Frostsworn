# 0.9.13：定向联机与预览兼容修复

## 已复现的错误

0.9.12的SyncRngMessage.Serialize小方法可被JIT内联进闭合泛型发送/包写入入口。测试在Entry.Initialize之前预编译真实NetMessageBus.SerializeMessage<SyncRngMessage>，再初始化模组：旧代码仍输出原生的12字节种子长度头0x0000000C，与报告相同，接收端期待FSR1而拒绝。开启优化、关闭分层编译时，原通用包接口空快照测试也暴露同类绕过路径。

修复针对SyncRngMessage的闭合NetMessageBus.SerializeMessage及PacketWriter.Write/PacketReader.Read入口，仍使用同一FrostRngWire097编码。其他消息不拦截，原生消息ID、发送者封包格式不变。正式版FSR2与测试版FSR1格式仍分离，不丢失RNG状态、未知流、空列表、自定义稀有度或遗物顺序，不接受未知遗物，不放宽错误包校验。升级双方协议hash盐，明确要求所有玩家0.9.13并重启，拒绝旧发送实现的混用。

使用工坊Acheron 0.4.11真实程序集复现其MyriadVoidDescriptionPatch.Postfix：对霜誓者canonical模板读取Owner会抛CanonicalModelException。可选兼容仅拦截这个已确认的Postfix，且仅当卡为FrostCard只读模板时跳过；mutable真实牌和其他角色保留原行为。原生卡牌描述照常生成。无硬依赖；扫描已加载程序集并监听Acheron后加载，验证签名并幂等安装。不运行对方模组初始化，不改对方文件。

## 验证与限制

两套核心游戏程序集及对应Ritsu分别构建；共享Steam等运行库与资源PCK使用本机当前安装，没有切游戏分支。8组233项断言、0引擎错误。包括：原失败预编译发送入口、真实网络信封往返、11/12/13位编号配置的完整状态、无关心跳消息、空/损坏/未知遗物包、真实Acheron补丁关闭保护时失败/打开时成功、Acheron前后两种加载顺序、实际原生GetDescriptionForPile链、mutable及其他角色不跳过、0.9.12肖像回归。

本地测试不能替代两台电脑联机；不能声称修复其他来源的黑屏。未修改冰晶Canceled问题或卡牌牌效。发布状态见workshop/published-state.json，不以提交超时自动推断成功或双向分发生效。

## 工坊核验

2026-10-21正式版10:14、测试版10:16双语记录可见，API范围13条，双语标题和Ritsu依赖保留。两次SDK提交仍Timeout，没有重复上传。当前public-beta DownloadItem返回OK，实际四文件哈希与已测试包一致，build-target.txt为0.111.0。正式版0.9.13下载没有切分支复测，不声称双向自动分发完全正常。
