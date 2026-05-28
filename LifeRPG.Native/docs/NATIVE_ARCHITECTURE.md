# LifeRPG Native Architecture

LifeRPG.Native 当前是一个完全独立的 Windows 原生 WPF 桌面软件。

## 当前技术路线

- UI：C# + WPF
- 数据库：SQLite 本地存储
- 文件存储：本地文件系统
- 项目目标：不依赖 Obsidian，不依赖浏览器内核，不使用 Tauri、Electron、WebView、React

## 存储设计

- 应用数据根目录：`%LocalAppData%\LifeRPG.Native`
- SQLite 数据库目录：`%LocalAppData%\LifeRPG.Native\data`
- 图片目录：`%LocalAppData%\LifeRPG.Native\assets`
- 备份目录：`%LocalAppData%\LifeRPG.Native\backups`
- 日志目录：`%LocalAppData%\LifeRPG.Native\logs`

数据库文件当前使用 `liferpg.db`，保存在 `data` 目录下。

图片证据文件当前设计为直接存储在本地 `assets` 文件夹中。

- 图片证据保存在 `%LocalAppData%\LifeRPG.Native\assets`
- 每条带图片的记录创建一个可读子文件夹
- 文件夹命名格式：`yyyyMMdd_HHmmss_记录标题_LifeRecordId前8位`
- 图片文件命名格式：`序号_原始文件名_图片Id前8位.扩展名`
- SQLite 保存图片元数据和完整本地路径
- 原始图片不会被删除
- 没有图片的记录不会创建空 `assets` 子文件夹
- 行为记录支持新增和编辑
- 行为记录支持删除
- 图片证据支持本地删除
- 删除图片时会删除本地文件和 `LifeRecordImages` 表记录
- 如果图片所在记录文件夹为空，则删除空文件夹
- 删除行为记录时，会同时删除该记录关联的图片文件和 `LifeRecordImages` 记录
- 删除行为记录不会影响其他记录
- `assets` 根目录不会被删除

## 架构边界

- 运行时不依赖 Obsidian
- 运行时不使用任何浏览器套壳
- 当前阶段不引入 Entity Framework，数据库初始化采用轻量 SQL
- 当前阶段尚未支持自定义图片目录
- 当前阶段尚未支持图片压缩
- 当前阶段尚未支持图片导出

## 后续扩展方向

- 后续可以在设置中支持用户自定义图片目录，例如 D 盘目录
- 后续可以支持 Markdown、PDF、JSON 等导出能力，但这些能力只作为导出格式，不作为应用运行依赖
