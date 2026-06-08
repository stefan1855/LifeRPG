# LifeRPG Native v1.0-pre Checkpoint

- 分支目标：`v0.5-to-v1.0-rpg-foundation`
- 本轮定位：RPG 系统基础骨架搭建，不提交、不合并、不删分支
- 已引入 DI 容器，入口位于 `LifeRPG.App/Infrastructure`
- 已注册 Repository / Storage Service / RPG Service / MainWindow
- v0.5 仅完成 DI 基础接入，旧代码中的部分手动 `new` 仍保留，后续逐步迁移
- 已新增轻量数据库迁移机制 `SchemaMigrations`
- 已新增角色、属性、属性日志、RPG 经验事件、装备、技能、NPC、地图基础表
- 已保留旧表结构，不删除旧表、不重建旧表、不清空旧数据
- 已新增 `RpgEngine` 纯计算层，不依赖 UI / SQLite / 文件系统
- 已新增默认主角色与默认六属性：体魄、心智、纪律、表达、创造、资源
- 已新增角色仪表盘 `CharacterDashboardControl`
- 已接入行动任务完成后的 RPG 成长发放
- 已通过 `RpgExperienceEvents` 防止同一任务重复发经验
- 第一版成长只追加，不做经验回滚
- 已新增装备 / 技能 / NPC / 地图数据底座与 UI 占位展示
- 已读取到部分 Obsidian 规则文件，但 `Field_Definitions.md` 未给出可直接落表的六属性清单，因此当前仍使用 Native 默认六属性占位骨架
- 当前 Codex 环境仍可能出现无明细 Restore / WPF build 失败，不能据此判断代码必然错误
