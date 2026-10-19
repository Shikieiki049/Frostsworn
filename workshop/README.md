# 工坊双版本发布

工坊条目固定为 3816906546，不新建第二个条目。两个内容目录分别暂存并核对 DLL、PCK、manifest 的 SHA256，配置分别使用本目录的 JSON。

| 游戏 | Steam 分支 | 模组文件 |
| --- | --- | --- |
| 0.107.1 正式版 | public → public | 0.9.10 正式版 DLL |
| 0.111.0 测试版 | public-beta → public-beta | 已发布的 0.9.8 DLL |

上传配置的 minBranch 与 maxBranch 必须同时填写同一目标分支。不可为空，不可跨 public 与 public-beta，不要把两个 DLL 放在同一 content。Steam 按玩家游戏分支下载该分支范围内最新的兼容内容。

SetRequiredGameVersions 只在实际内容改变时记录；首次为旧版建立范围时附带 build-target.txt 与 release-target.txt，使这次上传有内容变更，同时保持旧版 DLL/PCK 字节不变。已有未限定范围的历史条目不作为最新发布流程使用。

先确认测试版范围已保存，再上传正式版。版本 API 设置失败应立即中止，不能仅警告后继续上传。使用 SteamUGC.GetNumSupportedGameVersions 和 GetSupportedGameVersionData 读回范围，并打开工坊改动说明，确认正式版和测试版各自条目及版本标记。若 SubmitItemUpdate 超时，先只读查询服务器与公开页面，不立即重复上传。

每次上传填写对应版本中英改动说明，保留双语标题、首页描述、预览和 RitsuLib 依赖 3747602295。不要用旧的不含 minBranch/maxBranch 的工作区再次上传；不能把新正式版 DLL 覆盖到 beta 工作区。后续版本分别构建并更新对应范围。

官方机制：https://partner.steamgames.com/doc/features/workshop/itemversioning

发布前运行 `python tools/validate_workshop_content.py <content目录> --branch public`（测试版使用 public-beta），检查所有子目录，不允许额外 JSON 混入。
