# LifeRPG Native v1.0.1 Checkpoint

- 分支：`v1.0.1-character-profile`
- 本次任务：完善角色资料系统
- 已通过迁移 `20260605_004_CharacterProfile` 为 `Characters` 追加资料字段
- 新增字段：`AvatarImagePath`、`Description`、`LastGrowthAt`
- `CreatedAt` 继续沿用已有角色创建时间字段
- 默认角色名称调整为“人生主角”
- 默认角色简介为“这是我的人生RPG角色。”
- `CharacterRepository` 新增角色资料读取和更新方法
- 角色仪表盘新增头像、名称、简介、创建时间、最后成长时间展示
- 头像支持选择、预览和清除
- 头像只保存本地路径，不保存二进制
- 头像加载使用 `BitmapCacheOption.OnLoad`，避免文件锁
- 成功发放 RPG 经验时会更新 `LastGrowthAt`
- 本轮没有修改 `RpgEngine`
- 本轮没有开发装备、技能、NPC、地图玩法
- 本轮没有修改行为记录、统计、Markdown 导出、备份恢复系统
