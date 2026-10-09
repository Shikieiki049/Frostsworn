# 霜誓者维护规则

- 用户已要求后续每次更新同步 GitHub。实现变更、运行与变更相关的检查后，提交并推送当前仓库已配置的 origin；没有配置远端或尚未登录时明确报告，不声称已同步。不强制推送，不改写既有远端历史。
- 可以调用 `tools/publish.ps1 -Message '说明'` 验证源码、提交并推送。游戏相关变更在调用前另运行对应引擎测试；该脚本的静态检查不代替游戏测试。
- 版本号以 `mod_manifest.json` 为准，运行 `tools/version_info.py` 同步生成源码常量。卡牌定义编辑 `tools/cards_data.py`，能力描述编辑 `tools/powers_data.py`，不要只改生成文件。
- 优先使用原版 CharacterModel、EventModel、CreatureCmd、CardCmd、原版 UI 节点以及 RitsuLib 已有接口。参考 https://github.com/ptrlrd/spire-codex ，最终签名以目标版本游戏 DLL 为准。
- 不提交游戏 DLL、原版解包结果、测试存档、账号凭据、构建缓存和版本 ZIP。构建产物放 Releases 或本地 dist，不放源码历史。
- 只运行与变更有关的检查；不为了整理源码改变牌效、日食规则或联机协议。
