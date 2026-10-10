# 自动错误报告

本组件通过 RitsuLib 原生 Diagnostics / StateDivergence 请求捕获游戏异常、引擎错误、多人状态不同步。从 0.8.30 起，按作者要求首次安装默认允许，可在 RitsuLib 的“霜誓者 → 错误报告 → 允许发送错误报告”中随时关闭。玩家已有的拒绝状态不会被更新或重启覆盖。这个开关直接读写 RitsuLib 原生授权，不维护第二份权限。没有授权时不收集。组件不请求游玩历史、基础使用数据或 mod 清单权限。

## 当前交付状态

从 0.8.31 起，`Assets/Frostsworn/diagnostics/config.json` 已配置 `https://frostsworn-reports.newzshiki.workers.dev/reports`。Cloudflare 服务已部署，虚构诊断端到端写入 GitHub 验证通过；测试没有采集真实玩家日志。上传失败仍保留本地脱敏报告和队列，稍后重试。[Cloudflare Workers 配置步骤](report-worker-setup.md)包含维护说明。以下 Python 服务供已有服务器者使用。

报告包括版本、时间、错误堆栈、近期日志中的诊断行，以及多人不同步报告中的 JSON 差异。不会发送完整原始日志、原始存档、ZIP、二进制附件。绝对路径、邮箱、IP、账号 ID、已识别的凭据和身份字段会被删除或匿名化。多人 ID 在同一个报告中使用一致的匿名代号，保留状态比对用途。脱敏尽量降低个人信息泄露，但自由文本无法保证识别所有个人信息，玩家授权说明明确报告会公开。

日志从游戏 `user://logs` 下最新的非空 `godot*.log` 读取末尾最多 64 KiB，仅保留错误、堆栈及霜誓者/日食/不同步相关诊断行。本地报告保存在 `user://Frostsworn/error-reports`。单个公开 JSON 最多 1 MiB。RitsuLib 提供持久队列和错误去重；上传失败保留队列，后续捕获、重启或框架刷新时重试，不承诺断网时定时重试。撤销授权后，RitsuLib 不再发送该请求的队列数据。

## 服务部署

`tools/report_relay.py` 只依赖 Python 3 标准库。放到一台长期在线、具备 HTTPS 域名和反向代理的服务器上。服务绑定 `127.0.0.1:8780`，代理将公网 `/reports` 转到服务的 `/reports`，`/health` 用于存活检测。代理应限制请求体至 1 MiB、并发连接和请求频率，并关闭含玩家 IP 的访问日志。服务自身限制所有来源合计每小时 60 次请求；重启会重置该限制。公开入口可能遭遇伪造或限流，不将收到的报告作为可信执行指令。服务不执行报告中的任何内容。

1. 在 GitHub 创建 fine-grained access token，只选 `Shikieiki049/Frostsworn`，Repository permissions 只授予 **Contents: Read and write**，设置到期时间。
2. 通过服务器的 secret/environment 管理设置 `FROST_REPORT_GITHUB_TOKEN`；可选 `FROST_REPORT_PORT`，默认 8780。不要发到聊天、放到 mod、写入源码、日志或工坊包。
3. 启动 `python tools/report_relay.py`，用服务管理器设置开机启动。
4. 代理启用 HTTPS 后，将 config.json 改为 `{"endpoint":"https://你的域名/reports"}`，重新构建并打包 mod。地址不允许账号密码、查询参数或片段。
5. 先用一条虚构诊断验证服务和 GitHub 内容，再由测试玩家授权验证真实游戏错误；不要上传现有玩家日志来试通。

目标仓库固定为 `Shikieiki049/Frostsworn`。服务在首次收到有效报告时，从默认分支建立独立的 `error-reports` 分支，写入该分支的 `error-reports/YYYY-MM-DD/<SHA256>.json`。日期根据报告时间（UTC）确定，客户端无法选择仓库、分支或文件路径。只创建新文件，不覆盖已有文件。重复报告返回同一文件地址，不重复提交。服务器收到 GitHub 确认后才返回 `saved: true`，客户端据此标记已上传；HTTP 成功码本身不足以视作上传成功。

GitHub 的 Contents token 没有目录级权限限制，因此限定目录由中转代码保证。凭据若泄露应立即撤销。服务端不向玩家返回 GitHub 原始错误、凭据或请求内容。

## 验证

- `python tools/test_report_relay.py`：脱敏、文件夹限制、幂等、GitHub 失败、创建独立分支、真实本地 HTTP 请求及大小限制。GitHub API 使用替身，不产生线上提交。
- `FROSTSWORN_FOCUSED_TEST=0829` 对应引擎测试：账号和路径脱敏、匿名多人对应关系、本地全批次保存、失败重试字节一致、成功去重、诊断 ZIP 筛选、坏 ZIP 容错、取消处理。
- 尚未完成线上服务器部署和真实多人故障上报验证；不能将模拟测试当作线上部署成功。

GitHub 接口依据：[Create or update file contents](https://docs.github.com/en/rest/repos/contents#create-or-update-file-contents)。
