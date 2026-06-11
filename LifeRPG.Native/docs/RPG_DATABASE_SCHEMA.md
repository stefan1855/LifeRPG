# RPG Database Schema

## 迁移机制

迁移入口：

- `LifeRPG.Data/Services/DatabaseMigrationService.cs`

迁移记录表：

- `SchemaMigrations`

规则：

- 每个迁移 Id 唯一
- 已执行迁移不重复执行
- 所有新增表使用 `CREATE TABLE IF NOT EXISTS`
- 不删除旧表
- 不重建旧表
- 不清空旧数据

## 新增迁移

### 20260605_001_RpgFoundation

新增：

- `SchemaMigrations`
- `RpgExperienceEvents`

### 20260605_002_Character

新增：

- `Characters`
- `CharacterAttributes`
- `AttributeLogs`

### 20260605_003_EquipmentSkillNpcMap

新增：

- `EquipmentDefinitions`
- `CharacterEquipment`
- `EquipmentAttributeBonuses`
- `SkillDefinitions`
- `CharacterSkills`
- `SkillUnlockConditions`
- `NpcDefinitions`
- `NpcRelationships`
- `MapLocations`

## 角色相关表

### Characters

- `Id`
- `Name`
- `Level`
- `TotalExperience`
- `CreatedAt`
- `UpdatedAt`

### CharacterAttributes

- `Id`
- `CharacterId`
- `AttributeKey`
- `DisplayName`
- `Level`
- `Experience`
- `BaseValue`
- `CreatedAt`
- `UpdatedAt`

### AttributeLogs

- `Id`
- `CharacterId`
- `AttributeKey`
- `ExperienceDelta`
- `SourceType`
- `SourceId`
- `Reason`
- `CreatedAt`

### RpgExperienceEvents

- `Id`
- `SourceType`
- `SourceId`
- `CharacterId`
- `TotalExperience`
- `Note`
- `CreatedAt`

说明：

- `SourceType + SourceId` 建唯一索引
- 用于防止任务重复发经验

## 默认六属性

当前 Native 默认种子属性：

- `physique / 体魄`
- `mind / 心智`
- `discipline / 纪律`
- `expression / 表达`
- `creativity / 创造`
- `resources / 资源`

补充：

- 保留 `general / 未设置` 作为空成长属性兜底
- 如任务写入默认六属性之外的自由文本，系统会创建扩展属性

## 旧表状态

本轮未删除、未重建、未破坏的既有表：

- `LifeRecords`
- `LifeRecordImages`
- `GrowthAttributes`
- `ActionTasks`
- `ActionTaskImages`
