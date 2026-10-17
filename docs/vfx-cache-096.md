# 0.9.6：特效资源生命周期

## 报告与定位

0.9.5 报告 `2026-10-17/3b1ac87f4ff603c1ae642b1ef90b17965d8a85d343d70eb7c4ed1ec9c10c9814.json` 的路径为 ResourceLoader.Load → FrostCardVfx.OptionalScene → CreateAt → Impact。Godot 的 SwapGCHandleForType 在内部捕获异常并记录，所以外层 try/catch 不能消除该内部记录。

代码检查确认：特效原先没有通过 CardModel.ExtraRunAssetPaths 纳入原版整局资源清单，每次命中直接 ResourceLoader.Load。目标游戏的 AssetCache.UnloadAssets / UnloadMissedCacheAssets 在房间/菜单切换时移除缓存并延迟 Dispose 资源；PreloadManager 保留当前整局资源集合。

## 修改

- FrostCard 合并基类资源清单，注册现存可选原版特效场景及粒子纹理，避免在正常房间切换时提前释放。
- 使用 PreloadManager.Cache 取得资源，避免每次出牌命中再次进入 ResourceLoader.Load。
- 同时把文字能量图标加入整局预加载清单。
- 保留日食空引用、缺失/错误类型特效的回退。原版缓存明确标记加载失败的可选资源同样安全跳过。
- 不改变牌效、数值、等待时长、随机数或联机协议。

## 验证与边界

旧版未通过新增的整局预加载清单断言。修复版在原版缓存中预加载真实特效，执行四次原版 LoadAssetSets 的卸载集合逻辑和垃圾回收，确认资源实例ID不变、句柄有效。负对照验证不在整局清单中的资源确实被同一流程释放。测试宿主未挂载后台 NAssetLoader，因此预先填充原版 AssetCache，测试不覆盖真实多人或完整后台加载线程。

拦截 ResourceLoader.Load 计数，确认已预加载情况下，90张牌的全部组合特效没有重新进入加载器。继续通过531个发布包图片/图集解码、90张牌特效播放/清理、缺失场景、图标异常等相关回归。

这是针对已确认资源管理缺口的修复；未在本地强行复现原报告的底层 SwapGCHandleForType 异常，是否完全消除玩家环境中的该异常仍需新版实际日志验证。
