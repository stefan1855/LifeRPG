# LifeRPG Native v1.0.3 Checkpoint

- 分支：`v1.0.1-character-profile`（v1.0.3 打磨在同一分支）
- 本次任务：DI 收尾 + 行动任务导出 + 测试补齐

## DI 迁移收尾

- `MarkdownExportService` 构造函数注入 `LifeRecordRepository`、`LifeRecordImageRepository`、`ActionTaskRepository`、`ActionTaskImageRepository`
- 移除全部字段初始化 `new()`，改为 DI 容器自动解析

## 行动任务 Markdown 导出

- `MarkdownExportService` 新增 `ExportActionTasksToMarkdown()` 方法
- 导出内容：任务标题、状态、分类、四象限、成长属性、经验值、创建/更新/截止/完成时间、图片证据数量
- 每任务独立 section，包含任务内容、完成备注、图片证据列表
- `ActionTaskControl` 工具栏新增"导出任务 Markdown"按钮
- 导出基于当前视图（今日/逾期/已完成/全部），支持按视图筛选导出

## 测试补齐

### 新增文件
- `ActionTaskTests.cs`：4 个测试
  - 四象限经验值计算（Theory 4 组）
  - 默认值验证
  - 经验值联动
  - 状态切换
- `CharacterAttributeCatalogTests.cs`：6 个测试
  - 6 属性解析（Theory 6 组）
  - 空值返回 general
  - 未知属性自定义
  - NormalizeKey 边界（Theory 5 组）
  - GetDefaultDefinitions 返回 6 属性

### 扩展文件
- `RpgEngineTests.cs`：6→19 个测试（+13 个）
  - 负经验等级回归 Lv1
  - 等级公式参数化验证（Theory 8 组）
  - Lv5 跨级验证
  - 未知属性分配
  - 零经验分配
  - 空装备列表
  - 多装备累积
  - Lv1/Lv10 升级经验

总计：6→29 个测试用例

## 未完成（留给后续版本）
- 经验回滚机制未实现（需架构设计）
- 扩展属性与核心六属性分区显示（需 UI 重构）
- 多主题切换
- 任务图片压缩
- 集成测试（SQLite 读写验证）
