# LifeRPG Native Architecture

LifeRPG.Native 当前是一个完全独立的 Windows 原生 WPF 桌面软件。

## 当前技术路线

- 当前项目是 Windows 原生 WPF 软件
- UI：C# + WPF
- 数据库：SQLite 本地存储
- 文件存储：本地文件系统
- 项目目标：不依赖 Obsidian，不依赖浏览器内核，不使用 Tauri、Electron、WebView、React
- 不使用 Node.js
- 使用 SQLite 做本地数据存储
- 使用 LocalAppData 做运行数据目录
- 使用 WPF ResourceDictionary 管理暗黑亡灵术士主题
- 当前主题是原生 WPF 主题，不是网页 CSS
- 当前已具备 `v0.1.0-native-dev` 可用原型基础

## 构建入口说明

- 当前主程序为 `LifeRPG.App`
- 推荐开发验证命令：
  - `dotnet build .\LifeRPG.App\LifeRPG.App.csproj`
  - `dotnet run --project .\LifeRPG.App\LifeRPG.App.csproj`
- 当前 `LifeRPG.Native.slnx` 构建入口在本机存在异常，暂不作为功能验证依据。
- 后续可以考虑重建传统 `.sln` 解决方案文件，替代当前 `.slnx`。
- `v0.2-sln-entry-fix` 分支已新增传统解决方案文件 `LifeRPG.Native.sln`，作为重建稳定解决方案入口的处理起点。

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
- 软件支持本地 zip 备份
- 备份内容包括 SQLite 数据库、`assets` 图片资源、`manifest.json`
- 默认备份目录为 `%LocalAppData%\LifeRPG.Native\backups`
- 软件支持 Markdown 本地导出
- Markdown 导出支持全量导出
- Markdown 导出支持当前筛选结果导出
- 导出文件默认保存在 `%LocalAppData%\LifeRPG.Native\exports`
- Markdown 导出读取 SQLite 中的 `LifeRecords` 和 `LifeRecordImages`
- Markdown 中图片证据使用本地图片路径引用
- 筛选结果导出基于当前界面列表，不影响数据库原始数据
- 筛选结果为空时仍可生成空结果 Markdown
- Markdown 导出只是归档能力，不作为软件运行依赖
- 软件运行不依赖 Obsidian
- 当前支持本地 SQLite 记录筛选
- 支持关键词搜索
- 支持分类筛选
- 支持创建日期范围筛选
- 筛选只影响界面列表显示，不影响数据库原始数据
- 当前未引入全文搜索索引
- 当前支持基础统计看板
- 统计数据来自本地 SQLite
- 支持整体统计、分类统计、最近 7 天统计
- 当前未引入第三方图表库
- 当前统计只影响界面展示，不影响原始数据
- 当前 UI 使用 WPF 原生 `ResourceDictionary` 管理主题
- 当前主题为暗黑亡灵术士风格雏形
- 主题不依赖 CSS、不依赖 WebView、不依赖前端框架
- 当前尚未支持多主题切换
- 当前尚未支持皮肤包导入
- 恢复备份会覆盖当前数据库和 `assets`
- 恢复前会自动创建恢复前安全备份
- 备份与恢复均为本地功能，不依赖云端
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
- 运行时不使用 Tauri
- 运行时不使用 Electron
- 运行时不使用 WebView
- 运行时不使用 React
- 运行时不使用 Node.js
- 运行时不使用任何浏览器套壳
- 当前阶段不引入 Entity Framework，数据库初始化采用轻量 SQL
- 当前阶段尚未支持自定义图片目录
- 当前阶段尚未支持图片压缩
- 当前阶段尚未支持 PDF 导出
- 当前阶段尚未支持图标素材与插画系统

## 后续扩展方向

- 后续可以在设置中支持用户自定义图片目录，例如 D 盘目录
- 后续可以支持 Markdown、PDF、JSON 等导出能力，但这些能力只作为导出格式，不作为应用运行依赖
