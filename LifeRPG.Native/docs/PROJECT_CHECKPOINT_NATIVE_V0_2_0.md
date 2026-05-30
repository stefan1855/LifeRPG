# LifeRPG Native v0.2.0 Checkpoint

- 分支：`v0.2-extract-system-status-control`
- 本次任务：抽离系统状态区为 `SystemStatusControl`
- 不改数据库结构
- 不改存储逻辑
- 不改业务功能
- 验证命令：
  - `dotnet build .\LifeRPG.Native.sln`
  - `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`
