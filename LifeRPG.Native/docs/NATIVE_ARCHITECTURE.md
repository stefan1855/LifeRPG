# LifeRPG Native Architecture

LifeRPG.Native 当前是一个完全独立的 Windows 原生 WPF 桌面软件。

## 当前技术路线

- UI：C# + WPF
- 数据库：SQLite 本地存储
- 文件存储：本地文件系统
- 项目目标：不依赖 Obsidian，不依赖浏览器内核，不使用 Tauri、Electron、WebView、React

## 存储设计

- 应用数据根目录：`%AppData%\LifeRPG.Native`
- SQLite 数据库目录：`%AppData%\LifeRPG.Native\data`
- 图片目录：`%AppData%\LifeRPG.Native\assets`
- 备份目录：`%AppData%\LifeRPG.Native\backups`
- 日志目录：`%AppData%\LifeRPG.Native\logs`

数据库文件当前使用 `liferpg.db`，保存在 `data` 目录下。

图片证据文件当前设计为直接存储在本地 `assets` 文件夹中，数据库只保存记录与文件路径信息。

## 架构边界

- 运行时不依赖 Obsidian
- 运行时不使用任何浏览器套壳
- 当前阶段不引入 Entity Framework，数据库初始化采用轻量 SQL

## 后续扩展方向

后续可以支持 Markdown、PDF、JSON 等导出能力，但这些能力只作为导出格式，不作为应用运行依赖。
