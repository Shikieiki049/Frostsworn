# 霜誓者维护规则

- 用户要求每次更新 Steam 创意工坊时，同时填写该版本的改动说明（changeNote）：标明版本号并简述实际变更，不能留空；上传后确认成功。不要为填写改动说明而覆盖工坊首页介绍。
- 工坊上传器只上传工作目录下的 `content` 子目录。发布前将 dist 中的 DLL、PCK、manifest 复制到 `content`，核对版本号及三个文件的哈希；不能只复制到工作目录根部。上传成功后必须打开工坊改动说明页面，确认新版本条目可见，再向用户报告发布成功。
- 用户已要求后续每次更新同步 GitHub。实现变更、运行与变更相关的检查后，提交并推送当前仓库已配置的 origin；没有配置远端或尚未登录时明确报告，不声称已同步。不强制推送，不改写既有远端历史。
- 可以调用 `tools/publish.ps1 -Message '说明'` 验证源码、提交并推送。游戏相关变更在调用前另运行对应引擎测试；该脚本的静态检查不代替游戏测试。
- 版本号以 `mod_manifest.json` 为准，运行 `tools/version_info.py` 同步生成源码常量。卡牌定义编辑 `tools/cards_data.py`，能力描述编辑 `tools/powers_data.py`，不要只改生成文件。
- 优先使用原版 CharacterModel、EventModel、CreatureCmd、CardCmd、原版 UI 节点以及 RitsuLib 已有接口。参考 https://github.com/ptrlrd/spire-codex ，最终签名以目标版本游戏 DLL 为准。
- 不提交游戏 DLL、原版解包结果、测试存档、账号凭据、构建缓存和版本 ZIP。构建产物放 Releases 或本地 dist，不放源码历史。
- 只运行与变更有关的检查；不为了整理源码改变牌效、日食规则或联机协议。

- 工坊发布配置必须显式保留完整非空标题“霜誓者 Frostsworn｜冰霜法师与日食挑战”，默认语言及每次本地化更新都携带标题；不能依赖省略 title 自动保留。发布后同时检查页面标题与改动说明。

- 当前工坊标题按语言保留：默认/简中“【虚空遗物+事件更新】霜誓者 Frostsworn｜冰霜法师与日食挑战”；英文“[Void Relics + Event Update] Frostsworn | Ice Mage & Eclipse Challenges”。这是用户要求的新标题，优先于前面的旧标题文字；后续发布不要还原旧标题。

- 用户要求工坊正式版/测试版隔离：同一条目使用 Steam 工坊多版本范围。正式版 minBranch=maxBranch=public，测试版 minBranch=maxBranch=public-beta；每套 content 使用各自目标 DLL 和 manifest。不可省略范围，不能把单个版本伪装成兼容全部分支。上传前后核对哈希、Steam 已保存范围及公开改动说明。配置见 workshop/README.md。
