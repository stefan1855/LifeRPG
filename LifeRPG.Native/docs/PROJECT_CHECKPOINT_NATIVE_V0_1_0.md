# LifeRPG Native v0.1.0 Checkpoint

## 当前已完成的基础能力

- 建立 `LifeRPG.App`、`LifeRPG.Core`、`LifeRPG.Data`、`LifeRPG.Storage`、`LifeRPG.Tests` 五个项目
- 建立基础领域模型：`LifeRecord`、`LifeRecordImage`、`GrowthAttribute`
- 建立本地存储路径管理
- 启动时自动创建 LocalAppData 下的应用目录
- 启动时自动初始化 SQLite 数据库
- 建立 `LifeRecords`、`LifeRecordImages`、`GrowthAttributes` 三张基础表
- 建立原生 WPF 主窗口基础布局，并展示当前版本与路径信息
- 已将运行数据目录从 AppData\Roaming 调整为 AppData\Local
- 已完成行为记录最小闭环
- 已支持新增记录
- 已支持从 SQLite 读取记录列表

## 当前项目结构

- `LifeRPG.App`：WPF 桌面应用入口与主窗口
- `LifeRPG.Core`：核心模型定义
- `LifeRPG.Data`：SQLite 初始化与数据层基础设施
- `LifeRPG.Storage`：本地文件与目录路径管理
- `LifeRPG.Tests`：测试项目
- `docs`：原生版本架构与阶段文档

## 当前存储路径

- 应用根目录：`%LocalAppData%\LifeRPG.Native`
- 数据库目录：`%LocalAppData%\LifeRPG.Native\data`
- 图片目录：`%LocalAppData%\LifeRPG.Native\assets`
- 备份目录：`%LocalAppData%\LifeRPG.Native\backups`
- 日志目录：`%LocalAppData%\LifeRPG.Native\logs`

## 当前数据库文件位置

- `"%LocalAppData%\LifeRPG.Native\data\liferpg.db"`

## 当前尚未完成的功能

- 图片导入与预览
- 成长属性编辑界面
- 复盘归档流程
- 搜索、筛选、排序
- Markdown/PDF/JSON 导出
- 备份恢复 UI
- 日志写入与异常跟踪
- 自动化测试覆盖
- 当前尚未支持图片上传
- 当前尚未支持自定义图片目录
- 当前尚未支持记录编辑
- 当前尚未支持记录详情页

## 下一阶段建议

- 接入图片证据上传
- 接入本地 `assets` 存储
- 为自定义图片目录预留设置配置
- 增加成长属性编辑与聚合展示
- 增加设置页与备份功能入口
