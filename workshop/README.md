# 工坊双版本发布

工坊条目固定为 3816906546，不新建第二个条目。两个内容目录分别暂存并核对 DLL、PCK、manifest 的 SHA256，配置分别使用本目录的 JSON。

| 游戏 | Steam 分支 | 模组文件 |
| --- | --- | --- |
| 0.107.1 正式版 | public → public | 0.9.11 正式版 DLL |
| 0.111.0 测试版 | public-beta → public-beta | 0.9.11 测试版 DLL |

上传配置的 minBranch 与 maxBranch 必须同时填写同一目标分支。不可为空，不可跨 public 与 public-beta，不要把两个 DLL 放在同一 content。Steam 按玩家游戏分支下载该分支范围内最新的兼容内容。

SetRequiredGameVersions 只在实际内容改变时记录；首次为旧版建立范围时附带 build-target.txt 与 release-target.txt，使这次上传有内容变更，同时保持旧版 DLL/PCK 字节不变。已有未限定范围的历史条目不作为最新发布流程使用。

先确认测试版范围已保存，再上传正式版。版本 API 设置失败应立即中止，不能仅警告后继续上传。使用 SteamUGC.GetNumSupportedGameVersions 和 GetSupportedGameVersionData 读回范围，并打开工坊改动说明，确认正式版和测试版各自条目及版本标记。若 SubmitItemUpdate 超时，先只读查询服务器与公开页面，不立即重复上传。

每次上传填写对应版本中英改动说明，保留双语标题、首页描述、预览和 RitsuLib 依赖 3747602295。不要用旧的不含 minBranch/maxBranch 的工作区再次上传；不能把新正式版 DLL 覆盖到 beta 工作区。后续版本分别构建并更新对应范围。

官方机制：https://partner.steamgames.com/doc/features/workshop/itemversioning

发布前运行 `python tools/validate_workshop_content.py <content目录> --branch public`（测试版使用 public-beta），检查所有子目录，不允许额外 JSON 混入。

## 2026-10-19 分发关联复核

不能仅凭 GetNumSupportedGameVersions / GetSupportedGameVersionData 的历史范围列表宣称当前分发已生效。本次发现 public-beta 游戏下仍安装 public 的 DLL，且首页缺少“已知游戏版本支持”原生面板。重新提交测试版内容快照并设置范围后，首页面板恢复。

后续不要在版本化内容发布后直接使用不携带内容/范围的独立元数据更新流程。本次恢复与此前本地化元数据更新相关的假设尚未完全隔离证明，但发布流程应保守处理：任何标题/描述更新后都必须重新检查原生面板、最新改动说明和客户端下载文件。范围查询、面板存在和实际下载是三个独立验证项；未做实际切换下载测试时明确写“待验证”。不要重复提交已经成功的超时上传。

后续实测补充：正式版再次提交后面板消失；最后提交带英文语言的测试版后，固定1280px桌面布局下首页仍未显示该面板。因此不能将根因归为匿名访问、窄屏或元数据更新，面板显示问题仍未解决。停止重复发布。最后一次测试版 Steam DownloadItem 返回 OK，当前 public-beta 实际安装已恢复为0.9.8/目标0.111.0，DLL与原测试版SHA256相同。正式版双向切换下载尚未验证，不能称为双向自动分发修复完成。

用户批准实际切换后的结果：游戏已从0.111.0切到正式版0.107.1，GetCurrentBetaName为空（默认分支），DownloadItem返回OK，但模组仍为0.9.8/目标0.111.0。故分支自动选择明确未修好，不应继续用发布顺序或历史范围推断已解决。需要登录作者网页检查“关联到游戏版本”，尤其核查Timeout的内容提交是否完整完成版本关联。本机随后恢复原public-beta分支。


## 0.9.11 当前发布核验

两套内容已在用户要求下提交，各自双语改动说明12:51/12:52可见；SDK提交均超时。API范围增加到9条，双语标题及前置依赖保持。当前正式版0.107.1实际DownloadItem得到0.9.11正式版，DLL哈希与暂存包一致；这是一次成功的正式版下载证据。测试版0.9.11下载未切分支复测，不能称为双向分发全部修好。勿因Timeout重复上传。
