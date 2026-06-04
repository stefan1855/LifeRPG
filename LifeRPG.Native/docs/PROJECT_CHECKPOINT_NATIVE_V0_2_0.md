# LifeRPG Native v0.2.0 Checkpoint

- `v0.3-action-task-life-record-link` 分支已开始推进行动任务与行为记录关联能力
- 当前目标是支持从已完成任务生成行为记录、关联已有行为记录，以及在记录详情区反向查看来源任务
- 不影响 `v0.2` 已完成功能和 `v0.2` 检查点封存状态

- 分支：`v0.2-action-task-image-preview`
- 本次任务：行动任务图片证据预览功能
- 已支持删除选中任务图片
- 已支持删除本地图片文件
- 已支持删除 `ActionTaskImages` 表记录
- 已支持删除空图片目录
- 已支持任务图片列表选中后显示图片预览
- 已支持显示当前预览图片文件名
- 已支持“打开原图”
- 已支持“清空预览”
- 已支持删除当前正在预览的图片前释放预览引用
- 已支持切换任务后清空旧任务图片预览
- 当前尚未支持删除任务时联动删除全部任务图片
- 当前尚未支持任务生成行为记录
- 已移除 `scripts/verify-build.ps1`
- 已恢复直接使用 `dotnet build .\LifeRPG.Native.sln` 进行构建验证
- 已恢复直接使用 `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj` 启动验证
- 构建判断以用户本机 PowerShell 的 `direct build exit code` 为准
- 验证命令：
  - `dotnet build .\LifeRPG.Native.sln -v:normal -p:GenerateFullPaths=true -p:UseSharedCompilation=false`
  - `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`
