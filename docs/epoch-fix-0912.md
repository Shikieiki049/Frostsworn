# 0.9.12：时间线肖像与协议诊断

修复已确认的0.9.11资源错误：FrostEpoch此前只指定ID，时间线详情回退寻找不存在的原版placeholder图片。使用Ritsu的CustomPackedPortraitPath及CustomBigPortraitPath，分别指向已有角色头像与选人背景，不增加或替换其他角色美术。

Ritsu 0.6.7只覆盖图片路径；原生HasRealPortrait仍探测原版目录，正式版还会缓存IsArtPlaceholder。因此针对FrostEpoch单独覆盖该判断；其他epoch返回原生流程。兼容正式版私有getter和测试版公开getter，epoch ID、故事注册和存档解锁标识均不变。

针对协议报告，只补充错误中的实际包头、预期包头、本机模组版本、目标游戏版本和统一构建后重启指引。同步数据格式、协议盐、校验、异常拒绝和状态提交方式保持不变；不能称为协议不匹配根因修复。冰晶Canceled记录无有效现场堆栈，本次未修改牌效或吞掉取消异常。

验证：两套实际目标游戏程序集分别构建；正式版使用本地保存的0.107.1核心引用和对应Ritsu，公共Steam/音频运行库与资源PCK使用本机现有安装，没有切换游戏分支。原生EpochModel的Portrait、RealPortrait、ResolvedPortraitPath及占位判断验证，重复加载、等比渲染截图、解锁标识保留。错误协议包测试与原有FSR1/FSR2完整快照回归通过，4组105项断言、0引擎错误。未进行两台电脑实机联机，未声称完整时间线交互流程全部验证。

本版本已生成独立正式版/测试版压缩包，源码同步GitHub；2026-10-20按用户要求提交工坊；正式版09:47、测试版09:48双语改动说明可见。两次SDK返回Timeout，未重复上传。当前public-beta实际下载的四个文件哈希与已测试包一致，build-target.txt及描述目标0.111.0；正式版0.9.12下载未切分支复测。报告入口：error-reports/2026-10-19/85e89948b2451b57024bddde6b5ad5ff12be5b0b91e41bd5cc474bee7eef549a.json。
