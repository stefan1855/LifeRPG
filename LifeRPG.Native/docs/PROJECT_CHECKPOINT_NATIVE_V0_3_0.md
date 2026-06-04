# LifeRPG Native v0.3.0 Checkpoint

- 分支：`v0.3-action-task-life-record-link`
- 本次任务：行动任务与行为记录关联
- 已支持已完成任务生成行为记录
- 已支持关联已有行为记录
- 已支持取消任务与行为记录关联
- 已支持从任务侧查看关联行为记录
- 已支持在行为记录详情区显示来源行动任务
- 已支持从行为记录详情区反向定位到来源任务
- 关联关系由 `ActionTasks.LinkedLifeRecordId` 维护
- 删除行为记录时会先解除关联关系，再删除记录本身
- 不改数据库表结构
- 不改图片存储逻辑
- 不改备份恢复逻辑
- 不改 Markdown 导出逻辑
- 验证命令：
  - `dotnet restore .\LifeRPG.Native.sln -v:normal -p:GenerateFullPaths=true -p:UseSharedCompilation=false`
  - `dotnet build .\LifeRPG.Native.sln -v:normal -p:GenerateFullPaths=true -p:UseSharedCompilation=false`
  - `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`
