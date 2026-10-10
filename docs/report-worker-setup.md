# 不买服务器的中转接口配置

玩家不需要任何登录；作者只需首次配置 Cloudflare 和 GitHub。默认开启的游戏设置在 **RitsuLib 模组设置 → 霜誓者 → 错误报告 → 允许发送错误报告**。手动关闭后不会因更新而重新打开。目前 mod 的上传地址仍为空，下面部署完成后再填入真实地址。

## 1. 创建 GitHub 密钥

打开 [Fine-grained tokens](https://github.com/settings/personal-access-tokens)，选择 Generate new token：

- 名称：`Frostsworn reports`
- 设置到期时间，记得到期前替换。
- Repository access：Only select repositories，只选 `Frostsworn`。
- Repository permissions：**Contents → Read and write**；其他权限不增加。

复制生成的 token，稍后只填入 Cloudflare 的 Secret，**不要发给我，也不要填到 mod 或源码里**。

## 2. 从 GitHub 部署 Worker

注册并登录 [Cloudflare 控制台](https://dash.cloudflare.com/)。可以先用 Workers Free 测试；免费额度和 CPU 限制见 [官方说明](https://developers.cloudflare.com/workers/platform/limits/)，大报告处理可能受 CPU 限制，不能保证所有报告都能在免费套餐处理完成。

进入 **Workers & Pages → Create application**，选择连接 GitHub / 导入已有仓库，授权访问 `Shikieiki049/Frostsworn`。按下列配置部署：

| 项目 | 填写 |
| --- | --- |
| Worker 名称 | `frostsworn-reports` |
| 仓库 | `Shikieiki049/Frostsworn` |
| 分支 | `main` |
| Root directory / 根目录 | `services/report-worker` |
| Build command / 构建命令 | 留空 |
| Deploy command / 部署命令 | `npx wrangler deploy` |

代码和 `wrangler.jsonc` 已经写好；通过仓库部署会同时配置限流绑定。若账户首次要求设置 `workers.dev` 子域名，自行取一个可用名称即可，不需要购买域名。

## 3. 填入 Secret

部署后进入这个 Worker 的 **Settings → Variables and Secrets → Add**：

| 项目 | 填写 |
| --- | --- |
| Type / 类型 | **Secret** |
| Name / 名称 | `GITHUB_TOKEN` |
| Value / 值 | 第 1 步生成的 token |

点击 Deploy 保存。不要选普通明文变量。不要开启输出完整请求内容的调试日志；仓库部署配置默认关闭 Observability。

## 4. 发我接口地址

Worker 会给出类似 `https://frostsworn-reports.你的子域名.workers.dev` 的地址。浏览器打开该地址加 `/health`，正常应显示 `{"status":"running","configured":true}`。这只说明服务运行、Secret 已配置，**不能证明 GitHub 写入权限已经通过**。

把这个公开的 Worker 地址发给我即可，不需要任何密钥。我会用虚构诊断做一次端到端写入验证，再把 `https://…workers.dev/reports` 配置进 mod，重新打包。确认 token 真正可写、网络可达后才启用线上上传。

## 报告位置与限制

报告写入 `Shikieiki049/Frostsworn` 的独立 `error-reports` 分支，在 `error-reports/YYYY-MM-DD/<摘要>.json` 中保存。仅新增文件，不覆盖源码。上传前两次脱敏；原始存档和原始日志 ZIP 不上传。

GitHub 目标和目录写死，客户端不能指定任意位置。公开入口不证明发送者身份，所以报告可能伪造，报告内容不得作为指令执行。当前 Cloudflare 限流每个服务位置每分钟约 6 次；代码还做每个运行实例每小时 60 次的后备限制。这些限制并非严格全局计数，恶意流量仍可能消耗额度；遇到滥用可暂时停用 Worker。GitHub 并发写入失败时客户端保留队列，后续重试。

代码：`services/report-worker/worker.mjs`；配置：`services/report-worker/wrangler.jsonc`；模拟测试：`node --test services/report-worker/test.mjs`。测试未上传真实玩家日志，尚未部署到你的账户。

依据：[Cloudflare 控制台部署](https://developers.cloudflare.com/workers/get-started/dashboard/)、[Secret 配置](https://developers.cloudflare.com/workers/configuration/secrets/)、[限流说明](https://developers.cloudflare.com/workers/runtime-apis/bindings/rate-limit/)、[GitHub 文件写入接口](https://docs.github.com/en/rest/repos/contents#create-or-update-file-contents)。
