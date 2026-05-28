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
- 已完成图片证据上传最小闭环
- 已支持新增行为记录时选择多张图片
- 已支持复制图片到 LocalAppData assets 目录
- 已支持将图片信息写入 LifeRecordImages 表
- 已支持记录列表显示图片数量
- 已支持选中记录后预览图片证据
- 已优化图片证据本地命名规则
- 已避免无图片记录创建空图片文件夹
- 旧 Guid 命名图片未迁移，作为历史测试数据保留
- 新增图片从本次修改后开始使用可读命名规则
- 已完成记录详情查看
- 已支持编辑行为记录
- 已支持删除单张图片证据
- 已支持删除空图片文件夹
- 已完成删除整条行为记录功能
- 删除记录时会清理关联图片文件
- 删除记录时会清理 `LifeRecordImages` 表关联数据
- 删除记录后会刷新列表并清空详情区域
- 已完成本地数据备份功能
- 已支持备份 `liferpg.db`
- 已支持备份 `assets` 图片目录
- 已支持生成 `manifest.json`
- 已支持从 zip 备份恢复
- 恢复前会自动创建安全备份
- 已完成 Markdown 导出功能
- 已支持导出全部行为记录
- 已支持导出图片证据路径
- 已支持默认导出目录
- 已支持选择目录导出
- 已支持按当前筛选结果导出 Markdown
- 已支持筛选结果为空时导出空结果说明
- 已保留全量 Markdown 导出
- 已完成记录筛选与搜索功能
- 已支持关键词搜索
- 已支持分类筛选
- 已支持日期范围筛选
- 已支持筛选结果数量显示
- 已完成基础统计看板
- 已支持记录总数统计
- 已支持总经验值统计
- 已支持图片证据数量统计
- 已支持分类维度统计
- 已支持最近 7 天记录统计

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

- 成长属性编辑界面
- 复盘归档流程
- 搜索、筛选、排序
- Markdown/PDF/JSON 导出
- 日志写入与异常跟踪
- 自动化测试覆盖
- 当前尚未支持自定义图片目录
- 当前尚未支持图片压缩
- 当前尚未支持高级统计图表
- 当前尚未支持图表可视化
- 当前尚未支持 PDF 导出
- 当前尚未支持导出时复制 assets 图片副本
- 当前尚未支持按日期范围筛选导出

## 下一阶段建议

- 实现基础主题与暗黑亡灵术士风格 UI 雏形
- 增加成长属性编辑与聚合展示
- 增加设置页与备份功能入口
