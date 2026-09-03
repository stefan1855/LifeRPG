# LifeRPG Native v1.3.0 Checkpoint

- 分支：`main`
- 本次任务：设置与数据目录 + 复盘归档 + DI 改造 + 任务管理强化 + 属性页面 + 快捷键系统 + 警告清零

## 设置与数据目录

- 设置页面完善：显示应用信息、运行时版本、数据目录、数据库大小
- `AppConfig`（`LifeRPG.Storage/Services/AppConfig.cs`）：JSON 持久化配置，存储在 `%LocalAppData%\LifeRPG.Native\config.json`
- `AppStoragePaths.RootDirectory` 改为读取 `AppConfig.DataRootDirectory`，不再硬编码
- "修改目录"按钮：打开文件夹选择对话框 → 保存 → 自动创建子目录 → 提示重启
- "打开目录"按钮一键打开资源管理器

## 复盘归档

- 备份历史 `DataGrid`：`BackupService.ListBackups()` 扫描 `*.zip`，显示文件名、大小、创建时间
- "选中恢复"按钮：确认对话框 → 恢复备份 → 刷新所有页面
- "刷新列表"按钮重载 DataGrid

## DI 改造

- `CharacterDashboardControl` / `ActionTaskControl` 从 Service Locator 模式迁移到 `Initialize()` 注入
- `MainWindow` 构造函数接收 DI 容器注入的服务，再传递给 XAML 创建的 UserControl
- 消除所有 `AppServiceProvider.Services.GetService()` 调用

## 任务管理强化

- 行动任务独立子页面：导航栏加专用按钮，从首页分离
- 逾期行红色高亮：`IsOverdue` 属性 + `DataGrid.Resources` 中的 `DataTrigger`，逾期任务红色加粗
- 排序 ComboBox：默认/截止日期↑/截止日期↓/名称A-Z/名称Z-A，支持 ISO 日期字符串排序
- `ToListItem` 从 static 改为实例方法

## 属性页面

- 首页保留角色仪表盘，新增独立"成长属性"页面
- 6 属性卡片 3×2 `UniformGrid`，每张显示：属性名（绿色）、等级、`ProgressBar`、经验值
- 数据流：`CharacterRepository.GetDashboardSnapshot()` → `RpgEngine.CalculateLevel()`
- `System.Windows.Controls.ProgressBar` 全限定名，避免与 Forms 歧义

## 快捷键系统

- 新增 `ShortcutSettingsWindow`（纯 CS 代码窗口，无 XAML 依赖）
- Ctrl+N：快速新增任务，Ctrl+S：保存当前编辑
- 设置页"设置快捷键"按钮打开配置弹窗：点方框变绿 → 按键捕获 → 实时显示 → 保存
- 多次重写避坑历程：
  - StaticResource 在独立窗口不可用 → 内联样式
  - TextBox 按键捕获不可靠 → `Window.KeyDown` + `Border`
  - `System.Drawing` 与 `System.Windows.Media` 命名冲突 → 全限定名 + 移除 Drawing using
  - `System.Windows.Forms` 与 WPF 类型歧义 → 纯 CS 重写，完全限定所有类型

## 图片证据

- 图片证据画廊页面：全量展示 + 关键词搜索（文件名/记录标题）
- 性能限制：最多显示 100 张

## 导出

- 任务导出时显示筛选提示（当前视图范围）

## 警告清零（本会话）

- `LifeRPG.Storage.csproj` 添加 `<NoWarn>CA1416</NoWarn>`：消除 20 个 System.Drawing 平台警告
- 删除 `MainWindow._currentRecordDetail` 未使用字段（CS0414）
- `CharacterAttributeCatalogTests` 添加 `input!` 压制 nullable 警告（CS8604）

## 未完成（留给后续版本）

- Repository 连接字符串注入（集成测试 blocked，需重构 `CreateConnection()` 为可注入）
- 多主题切换
- 扩展属性与核心六属性分区显示
- 经验回滚 UI 交互优化
- 测试覆盖率提升（当前仅 Core 层有纯逻辑测试）
