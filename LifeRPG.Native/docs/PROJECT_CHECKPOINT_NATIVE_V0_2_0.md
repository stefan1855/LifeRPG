# LifeRPG Native v0.2.0 Checkpoint

- 分支：`v0.2-sln-entry-fix`
- 目标：重建传统 `.sln` 解决方案入口
- 不改业务代码
- 不改 UI
- 不改数据库
- 验证命令：
  - `dotnet build .\LifeRPG.Native.sln`
  - `dotnet build .\LifeRPG.App\LifeRPG.App.csproj`
  - `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`
