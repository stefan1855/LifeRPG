using System.Text;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;

namespace LifeRPG.Storage.Services;

public sealed class MarkdownExportService
{
    private readonly LifeRecordRepository _lifeRecordRepository;
    private readonly LifeRecordImageRepository _lifeRecordImageRepository;
    private readonly ActionTaskRepository _actionTaskRepository;
    private readonly ActionTaskImageRepository _actionTaskImageRepository;

    public MarkdownExportService(
        LifeRecordRepository lifeRecordRepository,
        LifeRecordImageRepository lifeRecordImageRepository,
        ActionTaskRepository actionTaskRepository,
        ActionTaskImageRepository actionTaskImageRepository)
    {
        _lifeRecordRepository = lifeRecordRepository;
        _lifeRecordImageRepository = lifeRecordImageRepository;
        _actionTaskRepository = actionTaskRepository;
        _actionTaskImageRepository = actionTaskImageRepository;
    }

    public string ExportAllRecordsToMarkdown()
    {
        return ExportAllRecordsToMarkdown(AppStoragePaths.ExportsDirectory);
    }

    public string ExportAllRecordsToMarkdown(string targetDirectory)
    {
        var records = _lifeRecordRepository.GetAll();
        return ExportRecordsToMarkdown(records, targetDirectory, "Life RPG Native 行为记录导出");
    }

    public string ExportRecordsToMarkdown(IEnumerable<LifeRecord> records)
    {
        return ExportRecordsToMarkdown(records, AppStoragePaths.ExportsDirectory);
    }

    public string ExportRecordsToMarkdown(IEnumerable<LifeRecord> records, string targetDirectory)
    {
        return ExportRecordsToMarkdown(records, targetDirectory, "Life RPG Native 筛选行为记录导出");
    }

    public string ExportRecordsToMarkdown(IEnumerable<LifeRecord> records, string targetDirectory, string exportTitle)
    {
        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new ArgumentException("导出目标目录不能为空。", nameof(targetDirectory));
        }

        var recordList = records.ToList();
        var isFilteredExport = string.Equals(exportTitle, "Life RPG Native 筛选行为记录导出", StringComparison.Ordinal);
        var filePrefix = isFilteredExport ? "LifeRPG.Native_FilteredRecords" : "LifeRPG.Native_Records";

        AppStoragePaths.EnsureCreated();
        Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(
            targetDirectory,
            $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.md");

        var builder = new StringBuilder();
        var exportTime = DateTime.Now;

        builder.AppendLine($"# {exportTitle}");
        builder.AppendLine();
        builder.AppendLine($"导出时间：{exportTime:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine("数据来源：本地 SQLite");
        builder.AppendLine();

        if (isFilteredExport)
        {
            builder.AppendLine("导出范围：当前筛选结果");
            builder.AppendLine();
            builder.AppendLine($"记录数量：{recordList.Count}");
            builder.AppendLine();
        }

        builder.AppendLine("---");
        builder.AppendLine();

        if (recordList.Count == 0)
        {
            builder.AppendLine(isFilteredExport ? "当前筛选条件下暂无行为记录" : "当前暂无行为记录");
        }
        else
        {
            foreach (var record in recordList)
            {
                AppendRecordSection(builder, record);
            }
        }

        File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return filePath;
    }

    private void AppendRecordSection(StringBuilder builder, LifeRecord record)
    {
        var title = string.IsNullOrWhiteSpace(record.Title) ? "未命名记录" : record.Title.Trim();
        var category = string.IsNullOrWhiteSpace(record.Category) ? "未分类" : record.Category.Trim();
        var content = string.IsNullOrWhiteSpace(record.Content) ? "无" : record.Content;
        var images = _lifeRecordImageRepository.GetByLifeRecordId(record.Id);
        var sourceTask = _actionTaskRepository.GetByLinkedLifeRecordId(record.Id);

        builder.AppendLine($"## {title}");
        builder.AppendLine();
        builder.AppendLine($"- 分类：{category}");
        builder.AppendLine($"- 经验值：{record.Experience}");
        builder.AppendLine($"- 创建时间：{record.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"- 更新时间：{record.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"- 图片数量：{images.Count}");
        if (sourceTask is not null && !string.IsNullOrWhiteSpace(sourceTask.Title))
        {
            builder.AppendLine($"- 来源行动任务：{sourceTask.Title.Trim()}");
        }
        builder.AppendLine();
        builder.AppendLine("### 内容");
        builder.AppendLine();
        builder.AppendLine(content);
        builder.AppendLine();
        builder.AppendLine("### 图片证据");
        builder.AppendLine();

        if (images.Count == 0)
        {
            builder.AppendLine("无");
        }
        else
        {
            foreach (var image in images)
            {
                var fileName = string.IsNullOrWhiteSpace(image.FileName) ? "未命名图片" : image.FileName.Trim();
                var filePathText = image.FilePath ?? string.Empty;
                var fileMissingSuffix = File.Exists(filePathText) ? string.Empty : " 文件不存在";
                builder.AppendLine($"![{fileName}](<{filePathText}>)" + fileMissingSuffix);
            }
        }

        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
    }

    public string ExportAllActionTasksToMarkdown()
    {
        var tasks = _actionTaskRepository.GetAll();
        return ExportActionTasksToMarkdown(tasks, AppStoragePaths.ExportsDirectory);
    }

    public string ExportActionTasksToMarkdown(IEnumerable<ActionTask> tasks, string targetDirectory)
    {
        if (tasks is null)
        {
            throw new ArgumentNullException(nameof(tasks));
        }

        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new ArgumentException("导出目标目录不能为空。", nameof(targetDirectory));
        }

        var taskList = tasks.ToList();
        AppStoragePaths.EnsureCreated();
        Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(
            targetDirectory,
            $"LifeRPG.Native_ActionTasks_{DateTime.Now:yyyyMMdd_HHmmss}.md");

        var builder = new StringBuilder();
        var exportTime = DateTime.Now;

        builder.AppendLine("# Life RPG Native 行动任务导出");
        builder.AppendLine();
        builder.AppendLine($"导出时间：{exportTime:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine("数据来源：本地 SQLite");
        builder.AppendLine();
        builder.AppendLine($"任务数量：{taskList.Count}");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();

        if (taskList.Count == 0)
        {
            builder.AppendLine("当前暂无行动任务。");
        }
        else
        {
            foreach (var task in taskList)
            {
                AppendActionTaskSection(builder, task);
            }
        }

        File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return filePath;
    }

    private void AppendActionTaskSection(StringBuilder builder, ActionTask task)
    {
        var title = string.IsNullOrWhiteSpace(task.Title) ? "未命名任务" : task.Title.Trim();
        var content = string.IsNullOrWhiteSpace(task.Content) ? "无" : task.Content;
        var completionNote = string.IsNullOrWhiteSpace(task.CompletionNote) ? string.Empty : task.CompletionNote.Trim();
        var images = _actionTaskImageRepository?.GetByActionTaskId(task.Id) ?? [];

        var quadrantDisplay = task.Quadrant switch
        {
            ActionTaskQuadrant.ImportantUrgent => "重要且紧急",
            ActionTaskQuadrant.ImportantNotUrgent => "重要不紧急",
            ActionTaskQuadrant.NotImportantUrgent => "不重要但紧急",
            ActionTaskQuadrant.NotImportantNotUrgent => "不重要不紧急",
            _ => task.Quadrant.ToString()
        };

        var statusDisplay = task.Status switch
        {
            ActionTaskStatus.NotStarted => "未开始",
            ActionTaskStatus.InProgress => "进行中",
            ActionTaskStatus.Completed => "已完成",
            ActionTaskStatus.Cancelled => "已取消",
            _ => task.Status.ToString()
        };

        builder.AppendLine($"## {title}");
        builder.AppendLine();
        builder.AppendLine($"- 状态：{statusDisplay}");
        builder.AppendLine($"- 分类：{task.Category}");
        builder.AppendLine($"- 四象限：{quadrantDisplay}");
        builder.AppendLine($"- 成长属性：{(string.IsNullOrWhiteSpace(task.GrowthAttribute) ? "未设置" : task.GrowthAttribute)}");
        builder.AppendLine($"- 经验值：{task.Experience}");
        builder.AppendLine($"- 创建时间：{task.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"- 更新时间：{task.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        if (task.DueAt.HasValue)
        {
            builder.AppendLine($"- 截止时间：{task.DueAt.Value:yyyy-MM-dd HH:mm:ss}");
        }
        if (task.CompletedAt.HasValue)
        {
            builder.AppendLine($"- 完成时间：{task.CompletedAt.Value:yyyy-MM-dd HH:mm:ss}");
        }
        builder.AppendLine($"- 图片证据：{images.Count} 张");
        builder.AppendLine();

        builder.AppendLine("### 任务内容");
        builder.AppendLine();
        builder.AppendLine(content);
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(completionNote))
        {
            builder.AppendLine("### 完成备注");
            builder.AppendLine();
            builder.AppendLine(completionNote);
            builder.AppendLine();
        }

        if (images.Count > 0)
        {
            builder.AppendLine("### 图片证据");
            builder.AppendLine();
            foreach (var image in images)
            {
                var fileName = string.IsNullOrWhiteSpace(image.FileName) ? "未命名图片" : image.FileName.Trim();
                var filePathText = image.FilePath ?? string.Empty;
                var fileMissingSuffix = File.Exists(filePathText) ? string.Empty : " 文件不存在";
                builder.AppendLine($"![{fileName}](<{filePathText}>)" + fileMissingSuffix);
            }
            builder.AppendLine();
        }

        builder.AppendLine("---");
        builder.AppendLine();
    }
}
