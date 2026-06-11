# RPG Engine Architecture

## 目标

`RpgEngine` 是 Native 版 RPG 系统的纯计算层。

本层只负责：

- 总经验到等级的换算
- 行动任务经验分配到属性
- 到下一级所需经验计算
- 装备属性加成合并

本层明确不负责：

- WPF UI
- SQLite 读写
- 文件读写
- 图片逻辑
- 弹窗

## 当前结构

核心文件位于：

- `LifeRPG.Core/Rpg/RpgEngine.cs`
- `LifeRPG.Core/Rpg/RpgLevelResult.cs`
- `LifeRPG.Core/Rpg/RpgExperienceResult.cs`
- `LifeRPG.Core/Rpg/RpgAttributeGain.cs`
- `LifeRPG.Core/Rpg/RpgAttributeDefinition.cs`
- `LifeRPG.Core/Rpg/RpgEquipmentBonus.cs`
- `LifeRPG.Core/Rpg/CharacterAttributeCatalog.cs`

## 等级规则 v1

- 角色等级从 1 开始
- 每级所需经验 = `100 * 当前等级`
- 总经验累积升级

示例：

- `0 - 99 EXP = Level 1`
- `100 - 299 EXP = Level 2`
- `300 - 599 EXP = Level 3`

## 属性经验规则 v1

- 如果行动任务存在 `GrowthAttribute`，该任务经验 100% 发放到该属性
- 如果 `GrowthAttribute` 为空，则发放到 `general / 未设置`
- 如果任务填写了默认六属性之外的名称，系统会自动生成扩展属性行，不破坏默认六属性底座

## 装备加成规则 v1

- 装备加成只做纯函数合并
- 输入为基础属性字典和装备加成列表
- 输出为合并后的新字典
- 不修改输入字典实例

## 任务完成接线

当前接线链路：

1. `ActionTaskControl` 将任务标记为完成
2. `RpgProgressionService` 调用 `RpgEngine.AllocateTaskExperience`
3. `CharacterRepository` 在事务中写入：
   - `Characters`
   - `CharacterAttributes`
   - `AttributeLogs`
   - `RpgExperienceEvents`
4. `CharacterDashboardControl` 刷新显示

## 防重复发经验

使用 `RpgExperienceEvents` 作为幂等事件表。

任务完成时检查：

- `SourceType = ActionTask`
- `SourceId = 当前任务 Id`

如果事件已存在：

- 不重复发放总经验
- 不重复写属性经验
- 不重复写属性日志

## 当前限制

- 暂未做经验回滚
- 暂未做技能经验接入
- 暂未做 NPC 关系动态演化
- 暂未做地图、状态、流派、腐化结算
- 暂未做装备对属性面板的最终值渲染
