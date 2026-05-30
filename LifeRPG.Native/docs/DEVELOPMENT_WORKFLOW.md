# Life RPG Native 开发流程规则

## 1. 项目定位

- 当前项目是 Windows 原生 WPF 软件
- 不依赖 Obsidian
- 不使用 Tauri
- 不使用 Electron
- 不使用 WebView
- 不使用 React
- 当前主构建入口为 `LifeRPG.Native.sln`

## 2. 推荐开发目录

项目根目录：

`D:\Projects\LifeRPG\LifeRPG.Native`

## 3. 推荐构建命令

主构建命令：

`dotnet build .\LifeRPG.Native.sln -v:minimal -p:GenerateFullPaths=true -p:UseSharedCompilation=false`

主程序启动命令：

`dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`

## 4. 构建结果判断规则

- Codex 的 `dotnet build` 结果只作为参考
- 最终构建结果以用户本机 PowerShell 执行 `dotnet build` 后的 exit code 为准
- 当 Codex 出现“生成失败。0 个警告，0 个错误”时，不直接判定为代码失败
- 必须由用户本机 PowerShell 重新执行构建命令确认
- 如果 build exit code = 0，则视为构建通过
- 如果 build exit code 非 0，则需要根据完整 PowerShell 原文继续排查

## 5. 标准验证脚本

```powershell
cd "D:\Projects\LifeRPG\LifeRPG.Native"

dotnet build-server shutdown

dotnet build .\LifeRPG.Native.sln -v:minimal -p:GenerateFullPaths=true -p:UseSharedCompilation=false

Write-Host "build exit code: $LASTEXITCODE"

if ($LASTEXITCODE -eq 0) {
    dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj
}
```

## 6. Codex 报错处理规则

如果 Codex 反馈：

```text
生成失败。
0 个警告
0 个错误
```

不要让 Codex 立即修改代码。

正确处理流程是：

1. 用户在本机 PowerShell 执行标准验证脚本
2. 查看 build exit code
3. 如果 exit code = 0，忽略 Codex 的失败反馈
4. 如果 exit code 非 0，把 PowerShell 原始错误文本发给 ChatGPT 或 Codex 继续定位
5. 不要根据 Codex 的无明细失败进行盲修

## 7. Git 分支规则

- `main` 分支只保留已验证稳定版本
- 每个小任务新建独立分支
- 分支命名示例：
  - `v0.2-mainwindow-refactor`
  - `v0.2-dev-workflow-doc`
  - `v0.2-export-assets-copy`
- 小任务完成后，build 通过、功能验证通过，再合并回 `main`
- 合并后可以删除临时分支

## 8. 当前已知开发环境注意事项

- Codex 在当前项目中多次出现 `dotnet build` 反馈不可靠的情况
- 用户本机 PowerShell 构建结果更可信
- WPF 项目涉及 XAML 标记编译，错误排查必须以真实 build 日志和 exit code 为准
- 不要直接调用 `MarkupCompilePass1` 等 WPF 内部构建任务
