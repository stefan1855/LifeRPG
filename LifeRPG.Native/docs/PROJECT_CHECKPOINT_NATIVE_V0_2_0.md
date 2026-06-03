# LifeRPG Native v0.2.0 Checkpoint

- 分支：`v0.2-action-task-image-delete`
- 本次任务：行动任务图片证据删除功能
- 已支持删除选中任务图片
- 已支持删除本地图片文件
- 已支持删除 `ActionTaskImages` 表记录
- 已支持删除空图片目录
- 当前尚未支持任务图片预览
- 当前尚未支持删除任务时联动删除全部任务图片
- 当前尚未支持任务生成行为记录
- 已移除 `scripts/verify-build.ps1`
- 已恢复直接使用 `dotnet build .\LifeRPG.Native.sln` 进行构建验证
- 已恢复直接使用 `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj` 启动验证
- 构建判断以用户本机 PowerShell 的 `direct build exit code` 为准
- 验证命令：
  - `dotnet build .\LifeRPG.Native.sln -v:normal -p:GenerateFullPaths=true -p:UseSharedCompilation=false`
  - `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`
