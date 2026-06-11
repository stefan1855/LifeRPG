# RPG Foundation Roadmap

## 本轮已完成

### v0.5 DI 基础

- 引入 `Microsoft.Extensions.DependencyInjection`
- 新增 `AppServiceProvider`
- 新增 `ServiceRegistration`
- `App.xaml.cs` 在启动时初始化 DI
- `MainWindow` 改为从 DI 解析创建

### v0.5.5 数据库迁移

- 新增 `DatabaseMigrationService`
- 新增 `SchemaMigrations`
- 新增 RPG 系统相关基础表

### v0.6 RpgEngine

- 新增纯计算 `RpgEngine`
- 新增等级计算
- 新增任务经验分配
- 新增装备加成合并
- 新增基础单元测试

### v0.7 Character 数据底座

- 新增 `Character`
- 新增 `CharacterAttribute`
- 新增 `AttributeLog`
- 新增 `CharacterRepository`
- 启动时自动初始化默认角色和默认六属性

### v0.8 角色仪表盘

- 新增 `CharacterDashboardControl`
- 显示角色名称、等级、总经验、升级进度
- 显示属性经验
- 显示最近属性变化
- 显示装备、技能、NPC、地图占位区

### v0.9 任务完成接入 RPG

- 行动任务完成后可触发经验发放
- 写入角色总经验
- 写入属性经验
- 写入属性日志
- 通过 `RpgExperienceEvents` 防重复

### v0.9.5 扩展系统骨架

- 装备：基础表与占位数据
- 技能：基础表与占位数据
- NPC：基础表与占位数据
- 地图：基础表与占位数据

## 当前仍保留的手动 new

- 部分 Repository 内部仍直接创建 SQLite 连接
- `MarkdownExportService` 仍保留内部手动 new Repository
- `CharacterRepository` 内部组合读取装备 / 技能 / NPC / 地图仓储时仍保留手动 new

说明：

- 本轮目标是先打通骨架
- 不做一次性全量 MVVM / IOC / UnitOfWork 重构

## 已知风险

- Codex 环境下 WPF 项目可能出现无明细 build 失败，需用户本机验证
- `GrowthAttribute` 当前仍是自由文本，可能生成默认六属性之外的扩展属性
- 角色面板当前为骨架版，尚未做复杂交互
- 装备、技能、NPC、地图目前仅做数据底座和占位展示
- 经验回滚机制尚未实现

## v1.0 修 bug 清单

- 本机验证 DI 启动链路和 WPF XAML 编译
- 本机验证任务完成后角色面板实时刷新
- 验证重复完成、重复打开、恢复备份后的 RPG 数据一致性
- 验证角色面板在空数据和已有旧库上的展示
- 评估是否要把 `GrowthAttribute` 从自由文本收敛为受控选择
- 评估是否新增 `ExperienceTransaction` 支持回滚
- 评估是否把扩展属性与核心六属性分区显示
