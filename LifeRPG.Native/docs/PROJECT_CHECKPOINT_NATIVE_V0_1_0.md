# LifeRPG Native v0.1.0 Checkpoint

## 当前已完成的基础能力

- 建立 `LifeRPG.App`、`LifeRPG.Core`、`LifeRPG.Data`、`LifeRPG.Storage`、`LifeRPG.Tests` 五个项目
- 建立基础领域模型：`LifeRecord`、`LifeRecordImage`、`GrowthAttribute`
- 建立本地存储路径管理
- 启动时自动创建 AppData 下的应用目录
- 启动时自动初始化 SQLite 数据库
- 建立 `LifeRecords`、`LifeRecordImages`、`GrowthAttributes` 三张基础表
- 建立原生 WPF 主窗口基础布局，并展示当前版本与路径信息

## 当前项目结构

- `LifeRPG.App`：WPF 桌面应用入口与主窗口
- `LifeRPG.Core`：核心模型定义
- `LifeRPG.Data`：SQLite 初始化与数据层基础设施
- `LifeRPG.Storage`：本地文件与目录路径管理
- `LifeRPG.Tests`：测试项目
- `docs`：原生版本架构与阶段文档

## 当前存储路径

- 应用根目录：`%AppData%\LifeRPG.Native`
- 数据库目录：`%AppData%\LifeRPG.Native\data`
- 图片目录：`%AppData%\LifeRPG.Native\assets`
- 备份目录：`%AppData%\LifeRPG.Native\backups`
- 日志目录：`%AppData%\LifeRPG.Native\logs`

## 当前数据库文件位置

- `"%AppData%\LifeRPG.Native\data\liferpg.db"`

## 当前尚未完成的功能

- 记录创建、编辑、删除
- 图片导入与预览
- 成长属性编辑界面
- 复盘归档流程
- 搜索、筛选、排序
- Markdown/PDF/JSON 导出
- 备份恢复 UI
- 日志写入与异常跟踪
- 自动化测试覆盖

## 下一阶段建议

- 建立记录列表与详情页
- 增加 SQLite 的基础读写仓储
- 增加图片导入与文件复制策略
- 增加成长属性编辑与聚合展示
- 增加设置页与备份功能入口
