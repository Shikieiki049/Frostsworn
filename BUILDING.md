# 构建与同步

源码分别支持游戏 0.107.1（正式版）与 0.111.0（测试版）、.NET 9、Godot 4.5.1 Mono、RitsuLib 0.6.7。构建读取安装目录的 release_info.json，选择对应接口分支；其他版本立即中止。两个目标的 DLL 不通用，输出 manifest 和 build-target.txt 会标记实际目标，发布时不能混装。

## 一次性配置

安装 Python 3.10 或更高版本、.NET 9 SDK、Godot Mono，以及游戏与 RitsuLib。安装 Python 资源生成依赖：

```powershell
python -m pip install -r requirements.txt
```

将路径设为环境变量，或在仓库根目录建立被 Git 忽略的 `build.local.ps1`：

```powershell
$GamePath = 'D:/Steam/steamapps/common/Slay the Spire 2'
$RitsuPath = 'D:/Dependencies/RitsuLib/lib/net9.0'
$GodotPath = 'D:/Tools/Godot/Godot_v4.5.1-stable_mono_win64_console.exe'
$PythonPath = 'python'
```

`RitsuPath` 指向含 RitsuLib 运行时/共享组件 DLL 的目录，并使用对应游戏版本的兼容程序集。也可分别设置 `STS2_GAME_PATH`、`STS2_RITSU_PATH`、`GODOT_PATH`。需要时用 `-ReferencePath` 指定 .NET 9 参考程序集目录。首次构建不需要此仓库外原先的工作目录；本机存在旧参考目录时仍可复用。

```powershell
./build.ps1          # 只编译
./rebuild-local.ps1  # 生成定义、编译、导入资源、生成PCK
```

产物位于 `dist/Frostsworn`。游戏 DLL、依赖 DLL、构建缓存不会提交到 GitHub。

## 源码验证与引擎测试

```powershell
python tools/validate_source.py
```

GitHub Actions 运行源码与生成文件一致性检查，不在公共运行器下载或重分发游戏。
`Tests` 为使用游戏程序集的引擎回归测试，需要本地游戏、参考程序集、Godot 源码生成器和测试运行时；已有测试工作目录的维护者可使用 `Tests/build-tests.ps1` 与 `Tests/run-tests.ps1`。这部分脚本仍为开发测试环境工具，不是 GitHub Actions 的无游戏测试。

## 后续更新

1. 修改卡牌数据/源码；发布新版本时修改 `mod_manifest.json` 的版本号。
2. 构建并运行与改动相关的游戏检查。
3. 执行 `./tools/publish.ps1 -Message '修改说明'`，验证源码、提交并推送到 origin。

上传使用 Git Credential Manager 或 GitHub CLI 的正常登录，不把密码或令牌写入仓库。
首次登录可运行 `git credential-manager github login --username Shikieiki049 --device`。
源码同步与发布游戏 ZIP 是两件事：源码每次更新提交；ZIP 可另放 GitHub Releases。

## 正式版历史回归

为本地引擎测试准备独立的 Ritsu 运行时目录：放入 RitsuLib 的 `shared/*.dll`、`compat/0.107.1/*.dll` 以及根目录 `assets.zip`。不要把引导器或 0.111 的兼容 DLL 混入。依赖只留在本机，不分发。

```powershell
./build.ps1 -RitsuPath 'D:/Dependencies/ritsu-107'
./Tests/run-stable-regression.ps1 -Build -RitsuPath 'D:/Dependencies/ritsu-107' -GodotPath 'D:/Tools/Godot/Godot_v4.5.1-stable_mono_win64_console.exe'
```

测试需先准备上文的本地开发宿主与 PCK。汇总及每组日志写入仓库旁的 `work/stable-regression`。任一断言失败或出现引擎 ERROR 都算失败。使用隔离测试档案，不使用玩家的实际存档；详见验证报告。测试脚本仍包含维护者开发环境路径，换电脑需要配置运行时。
