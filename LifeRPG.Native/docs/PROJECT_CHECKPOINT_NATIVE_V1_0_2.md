# LifeRPG Native v1.0.2 Checkpoint

- 分支：`v1.0.1-character-profile`（v1.0.2 打磨在同一分支）
- 本次任务：代码卫生 + Bug修复 + 体验打磨

## DI 迁移

- `CharacterRepository` 构造函数新增 `EquipmentRepository`、`SkillRepository`、`NpcRepository`、`MapRepository` 参数
- 移除无参构造函数，所有依赖通过 DI 容器自动解析
- `GetDashboardSnapshot()` 不再内部 `new` 子仓储
- `DatabaseInitializer` 手动构造改为显式传入所有依赖（bootstrap 场景）

## Bug 修复

### 删除行动任务时自动清理关联图片
- `DeleteTaskButton_Click` 增加图片清理流程
- 先获取任务关联图片 → 删除物理文件 → 删除 DB 记录 → 清理空目录 → 删除任务
- 如果正在编辑该任务，同时清理待选图片和预览状态
- 确认对话框文案更新为"关联的图片证据也会一并删除"

### GrowthAttribute 收敛为受控选择
- 新增任务和编辑任务的"成长属性"从 `TextBox` 改为 `ComboBox`
- 7 个选项：未设置（通用经验）、体魄、心智、纪律、表达、创造、资源
- 新增辅助方法 `GetGrowthAttributeFromComboBox` 和 `SelectGrowthAttribute`
- 编辑已有任务时自动匹配现有属性值，匹配不上则回退到"未设置"

### 数据库迁移 CreatedAt 回填
- `20260605_004_CharacterProfile` 迁移新增 `CreatedAt` 回填逻辑
- 已有角色的 `CreatedAt` 为 NULL 时，使用 `UpdatedAt` 回填
- 如果都为空则使用当前时间

## 体验打磨

- 行动任务删除确认对话框提示更完整
- 删除任务后自动清理编辑区的图片预览和待选图片

## 文档更新

- `NATIVE_ARCHITECTURE.md` 更新行动任务模块说明
- 移除"当前仍不支持删除任务时自动删除全部任务图片"标记
- 新增 v1.0.2 改动记录

## 未完成（留给后续版本）
- `MarkdownExportService` 内部手动 new 仍未迁移
- 行动任务 Markdown 导出仍未接入
- 测试覆盖仍然偏低（仅 RpgEngine 6 例）
- 经验回滚机制未实现
- 多主题切换未实现
- 任务图片压缩未实现
