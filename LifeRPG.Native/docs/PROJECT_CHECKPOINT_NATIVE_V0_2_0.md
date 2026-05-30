# LifeRPG Native v0.2.0 Checkpoint

- 分支：`v0.2-dev-workflow-doc`
- 本次任务：新增开发流程规则文档
- 不改业务功能
- 不改数据库结构
- 不改存储逻辑
- 不改主题系统
- 已新增 `DEVELOPMENT_WORKFLOW.md`
- 已记录 Codex 构建反馈不稳定时的判断规则
- 已明确主构建入口为 `LifeRPG.Native.sln`
- 已明确最终构建判断以用户本机 PowerShell exit code 为准
- 验证命令：
  - `dotnet build .\LifeRPG.Native.sln -v:minimal -p:GenerateFullPaths=true -p:UseSharedCompilation=false`
  - `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`
