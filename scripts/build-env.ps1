# LifeRPG 构建环境引导
# 背景：本机用户名含中文，且 cargo/cc-rs 在非 ASCII 的 TEMP 下会触发 cl.exe D8050；
#       同时 PowerShell 会话默认没有 MSVC 的 INCLUDE/LIB，cl 找不到 stdarg.h。
# 用法：. .\scripts\build-env.ps1   然后正常执行 cargo / npm / npx tauri

$ErrorActionPreference = 'Stop'

$VcRoot  = 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Tools\MSVC\14.44.35207'
$SdkRoot = 'C:\Program Files (x86)\Windows Kits\10'

$env:INCLUDE = @(
  "$VcRoot\include",
  "$SdkRoot\include\10.0.26100.0\ucrt",
  "$SdkRoot\include\10.0.26100.0\shared",
  "$SdkRoot\include\10.0.26100.0\um"
) -join ';'

$env:LIB = @(
  "$VcRoot\lib\x64",
  "$SdkRoot\lib\10.0.26100.0\ucrt\x64",
  "$SdkRoot\lib\10.0.26100.0\um\x64"
) -join ';'

# 关键：把 TEMP/TMP 指到纯 ASCII 路径，避免 cl.exe 写调试记录失败
$safeTemp = 'D:\Projects\LifeRPG\.tmp\build_temp'
New-Item -ItemType Directory -Force -Path $safeTemp | Out-Null
$env:TEMP = $safeTemp
$env:TMP  = $safeTemp

# cargo 的 target 目录也放在 D 盘，避免 C 盘膨胀
if (-not $env:CARGO_TARGET_DIR) {
  $env:CARGO_TARGET_DIR = 'D:\Projects\LifeRPG\.tmp\cargo-target'
}

$env:PATH = "$VcRoot\bin\HostX64\x64;$env:USERPROFILE\.cargo\bin;$env:PATH"

Write-Host "[build-env] INCLUDE/LIB/TEMP ready, CARGO_TARGET_DIR=$env:CARGO_TARGET_DIR" -ForegroundColor DarkGreen
