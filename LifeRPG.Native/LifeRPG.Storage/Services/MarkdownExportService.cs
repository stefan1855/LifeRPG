using System.Text;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;

namespace LifeRPG.Storage.Services;

public sealed class MarkdownExportService
{
    private readonly LifeRecordRepository _lifeRecordRepository = new();
    private readonly LifeRecordImageRepository _lifeRecordImageRepository = new();
    private readonly ActionTaskRepository _actionTaskRepository = new();

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
}
