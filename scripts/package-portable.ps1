# LifeRPG 便携包出包脚本（固定入口，别再手敲 Compress-Archive）
#
# 为什么需要它：
#   构建发生在 D:\Projects 下，该目录链带「低完整性(Low)」强制完整性标签，
#   产物会继承 Low。低完整性进程写普通目录会被系统拒绝（os error 5），
#   表现为「无法创建目录 xxx：拒绝访问」。
#   本脚本在打包前把整棵树的完整性拉回 Medium，并在出包前做门禁校验。
#
# 用法：
#   pwsh -File scripts\package-portable.ps1 -Version 2.0
#   可选 -ExePath 指定 exe（默认取 CARGO_TARGET_DIR\release\liferpg.exe）

param(
  [Parameter(Mandatory = $true)][string]$Version,
  [string]$ExePath = ""
)

$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+(\.\d+)?(-[a-zA-Z0-9.-]+)?$') { throw '版本号格式不正确' }


$RepoRoot = Split-Path -Parent $PSScriptRoot
$DistRoot = Join-Path $RepoRoot 'dist'
$OutDir   = Join-Path $DistRoot "LifeRPG-portable-v$Version"
$ZipPath  = Join-Path $DistRoot "LifeRPG-portable-v$Version.zip"
$resolvedDist = [System.IO.Path]::GetFullPath($DistRoot).TrimEnd('\') + '\'
$resolvedOut = [System.IO.Path]::GetFullPath($OutDir)
if (-not $resolvedOut.StartsWith($resolvedDist, [System.StringComparison]::OrdinalIgnoreCase)) { throw '输出目录超出 dist 范围' }


if (-not $ExePath) {
  $target = if ($env:CARGO_TARGET_DIR) { $env:CARGO_TARGET_DIR } else { Join-Path $RepoRoot '.tmp\cargo-target' }
  $ExePath = Join-Path $target 'release\liferpg.exe'
}

if (-not (Test-Path $ExePath)) {
  throw "找不到已构建的 exe：$ExePath（先跑 npx tauri build --no-bundle）"
}

Write-Host "[1/6] 清理输出目录：$OutDir" -ForegroundColor Cyan
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

Write-Host "[2/6] 拷贝 exe" -ForegroundColor Cyan
Copy-Item $ExePath (Join-Path $OutDir 'LifeRPG.exe') -Force

Write-Host "[3/6] 生成 使用说明.txt 与 解除锁定.bat" -ForegroundColor Cyan

$readme = @"
LifeRPG · 绿火毁灭术  v$Version 绿色便携版
=====================================

【怎么用】
1. 把整个 LifeRPG 文件夹拷到你想放的位置（建议放至非系统盘，例如 D:\LifeRPG）。
2. 双击 LifeRPG.exe。
3. 首次启动会让你选一个「数据目录」（同样建议放至非系统盘）。
4. 开始使用。

【第一次打开被 SmartScreen 拦住怎么办】
exe 未做代码签名，Windows 会提示「已保护你的电脑」。
放行方法（任选其一）：
  A. 点提示框里的「更多信息」→「仍要运行」。
  B. 先双击本文件夹里的「解除锁定.bat」，再双击 LifeRPG.exe。
这是系统对未签名程序的例行提示，不是病毒告警。

【如果提示「拒绝访问 / os error 5」】
1) 先双击「解除锁定.bat」（它会给本文件夹解除网络来源锁定）；
2) 仍失败时，把整个 LifeRPG 文件夹拷到 D:\ 根目录下再运行；
3) 数据目录改成程序目录里的 LifeRPG-Data（应用会给出这个推荐路径）。

【数据在哪】
<你选的目录>\
├─ data\liferpg.db   数据库
├─ images\           图片证据
└─ backups\          备份压缩包

配置在同级 liferpg.config.json，拷走整个文件夹即可换机器。

【快捷键】
默认 Ctrl+Shift+L 呼出/隐藏，可在设置里改。

【三种形态】
大窗口（1460×920）→ 标题栏左侧按钮收起为挂件（340×600）→ 再收成胶囊条（260×44）
托盘左键唤回，右键可切形态或退出。

【AI 军师（可选，默认关闭）】
设置 → AI 军师，填网关地址（默认 http://127.0.0.1:8317/v1）、模型、密钥，点「测试连接」。
开启后有四个入口：
- 复盘归档页「AI 复盘」：生成周复盘，可存为行为记录
- 行动任务页顶部输入框：回车直接添加任务；点「AI 解析」才调用 AI 并打开确认表单
- 装备编辑弹窗「AI 起名」：现实物品 → 哥特装备名 + 属性加成
- 仪表盘右上「AI 教练」：给一条成长建议
关掉它或断网，其他功能完全不受影响。

【外接神域 MCP（可选）】
设置 → 展开「外接工具 · MCP」，可自行添加任意本地 MCP Server（命令 + 参数 + 环境变量），
点「测试握手」能探明该服务暴露的工具。腾讯会议、抖音脚本都能这样挂进来。

【卸载】
如开启过开机自启，先在设置中关闭，再删除程序目录。配置、数据与缓存的位置见上文。

【注意】
不要把程序放在带「低完整性」标记的目录里（例如某些沙箱工作区），
否则程序会被系统降权，无法写入数据目录。
"@
Set-Content -Path (Join-Path $OutDir '使用说明.txt') -Value $readme -Encoding UTF8

$unlock = @"
@echo off
REM Remove the Mark-of-the-Web (Zone.Identifier) from every file in this folder,
REM so Windows SmartScreen stops blocking LifeRPG.exe.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -LiteralPath '%~dp0' -Recurse -File | Unblock-File"
echo.
echo [OK] Files in this folder have been unblocked.
echo      You can now run LifeRPG.exe
echo.
pause
"@
Set-Content -Path (Join-Path $OutDir '解除锁定.bat') -Value $unlock -Encoding ASCII

Write-Host "[4/6] 剥离「低完整性」标签 -> Medium" -ForegroundColor Cyan
$icacls = & icacls "$OutDir" /setintegritylevel "(OI)(CI)Medium" /T /C 2>&1
$failed = ($icacls | Select-String -Pattern 'Failed processing' | ForEach-Object { $_.ToString() })
if ($failed) { Write-Host "  icacls: $failed" -ForegroundColor DarkYellow }
& icacls "$OutDir\LifeRPG.exe" /setintegritylevel Medium | Out-Null

Write-Host "[5/6] 门禁：校验目录内无 Low 标签" -ForegroundColor Cyan
$lowFiles = @()
Get-ChildItem $OutDir -Recurse -Force | ForEach-Object {
  $info = & icacls $_.FullName 2>&1 | Out-String
  if ($info -match 'Low Mandatory Level') { $lowFiles += $_.FullName }
}
if ($lowFiles.Count -gt 0) {
  $lowFiles | ForEach-Object { Write-Host "  LOW: $_" -ForegroundColor Red }
  throw "出包中止：仍有 $($lowFiles.Count) 个文件带 Low 完整性标签，会导致运行期 os error 5"
}
Write-Host "  OK：0 个 Low 标签" -ForegroundColor Green

Write-Host "[6/6] 打 zip 并校验条目" -ForegroundColor Cyan
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path (Join-Path $OutDir '*') -DestinationPath $ZipPath -Force

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
$names = $zip.Entries | ForEach-Object { $_.FullName }
$zip.Dispose()

$expected = @('LifeRPG.exe', '使用说明.txt', '解除锁定.bat')
$unexpected = $names | Where-Object { $expected -notcontains $_ }
if ($unexpected) {
  throw "出包中止：zip 里出现预期外的条目 -> $($unexpected -join ', ')"
}

$sizeKb = [math]::Round((Get-Item $ZipPath).Length / 1KB, 1)
Write-Host ""
Write-Host "出包完成：$ZipPath（$sizeKb KB，$($names.Count) 个条目）" -ForegroundColor Green
