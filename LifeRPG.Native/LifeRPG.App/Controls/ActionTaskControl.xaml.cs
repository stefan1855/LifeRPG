using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using LifeRPG.App.ViewModels;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;
using LifeRPG.Data.Services;
using LifeRPG.Storage.Services;
using LifeActionTask = LifeRPG.Core.Models.ActionTask;
using MessageBox = System.Windows.MessageBox;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfButton = System.Windows.Controls.Button;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace LifeRPG.App.Controls;

public partial class ActionTaskControl : System.Windows.Controls.UserControl
{
    private ActionTaskRepository _actionTaskRepository = null!;
    private ActionTaskImageRepository _actionTaskImageRepository = null!;
    private LifeRecordRepository _lifeRecordRepository = null!;
    private ActionTaskImageStorageService _actionTaskImageStorageService = null!;
    private RpgProgressionService _rpgProgressionService = null!;
    private MarkdownExportService _markdownExportService = null!;
    private readonly List<string> _pendingImagePaths = [];
    private ActionTaskView _currentView = ActionTaskView.All;
    private bool _hasLoaded;
    private string? _editingTaskId;
    private ActionTaskImageListItem? _selectedActionTaskImage;
    private string? _selectedActionTaskImagePath;

    public event Action<string>? LifeRecordSelectionRequested;
    public event Action? LifeRecordLinkChanged;
    public event Action? RpgProgressUpdated;

    public ActionTaskControl()
    {
        InitializeComponent();
    }

    public void Initialize(
        ActionTaskRepository actionTaskRepository,
        ActionTaskImageRepository actionTaskImageRepository,
        LifeRecordRepository lifeRecordRepository,
        ActionTaskImageStorageService actionTaskImageStorageService,
        RpgProgressionService rpgProgressionService,
        MarkdownExportService markdownExportService)
    {
        _actionTaskRepository = actionTaskRepository;
        _actionTaskImageRepository = actionTaskImageRepository;
        _lifeRecordRepository = lifeRecordRepository;
        _actionTaskImageStorageService = actionTaskImageStorageService;
        _rpgProgressionService = rpgProgressionService;
        _markdownExportService = markdownExportService;
    }

    private void ActionTaskControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        ShowAllTasks();
    }

    public void RefreshFromExternalChange()
    {
        RefreshCurrentView(_editingTaskId);
    }

    private void ActionTaskControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsDropDownOpen(e.OriginalSource as DependencyObject))
        {
            return;
        }

        var scrollViewer = FindParentScrollViewer(this);
        if (scrollViewer is null)
        {
            return;
        }

        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (e.Delta / 3.0));
        e.Handled = true;
    }

    private void CreateTaskButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var title = TaskTitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("任务标题不能为空。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryGetDueAt(out var dueAt))
            {
                return;
            }

            var quadrant = GetSelectedQuadrant();
            var now = DateTime.Now;
            var task = new LifeActionTask
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Content = TaskContentTextBox.Text ?? string.Empty,
                Category = string.IsNullOrWhiteSpace(TaskCategoryTextBox.Text) ? "未分类" : TaskCategoryTextBox.Text.Trim(),
                GrowthAttribute = GetGrowthAttributeFromComboBox(TaskGrowthAttributeComboBox),
                Quadrant = quadrant,
                Status = ActionTaskStatus.NotStarted,
                Experience = LifeActionTask.CalculateExperience(quadrant),
                DueAt = dueAt,
                CreatedAt = now,
                UpdatedAt = now
            };

            _actionTaskRepository.Create(task);
            ClearCreateTaskForm();
            RefreshCurrentView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"新增行动任务失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowTodayTasksButton_Click(object sender, RoutedEventArgs e)
    {
        _currentView = ActionTaskView.Today;
        RefreshCurrentView();
    }

    private void ShowOverdueTasksButton_Click(object sender, RoutedEventArgs e)
    {
        _currentView = ActionTaskView.Overdue;
        RefreshCurrentView();
    }

    private void ShowAllTasksButton_Click(object sender, RoutedEventArgs e)
    {
        ShowAllTasks();
    }

    private void ShowCompletedTasksButton_Click(object sender, RoutedEventArgs e)
    {
        _currentView = ActionTaskView.Completed;
        RefreshCurrentView();
    }

    private void MarkNotStartedButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSelectedTaskStatus(ActionTaskStatus.NotStarted);
    }

    private void MarkInProgressButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSelectedTaskStatus(ActionTaskStatus.InProgress);
    }

    private void MarkCompletedButton_Click(object sender, RoutedEventArgs e)
    {
        CompleteSelectedTask();
    }

    private void MarkCancelledButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSelectedTaskStatus(ActionTaskStatus.Cancelled);
    }

    private void TaskDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is ActionTaskListItem selectedTask)
        {
            LoadTaskForSelectedState(selectedTask.Id);
        }
    }

    private void SaveEditTaskButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_editingTaskId))
            {
                MessageBox.Show("请先选择一个任务", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var task = _actionTaskRepository.GetById(_editingTaskId);
            if (task is null)
            {
                MessageBox.Show("当前行动任务不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearEditTaskForm();
                RefreshCurrentView();
                return;
            }

            var title = EditTaskTitleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("任务标题不能为空。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryGetDueAt(EditTaskDueDatePicker, EditTaskDueTimeTextBox, requireDateForTime: true, out var dueAt))
            {
                return;
            }

            var quadrant = GetSelectedQuadrant(EditTaskQuadrantComboBox);
            task.Title = title;
            task.Content = EditTaskContentTextBox.Text ?? string.Empty;
            task.Category = string.IsNullOrWhiteSpace(EditTaskCategoryTextBox.Text) ? "未分类" : EditTaskCategoryTextBox.Text.Trim();
            task.GrowthAttribute = GetGrowthAttributeFromComboBox(EditTaskGrowthAttributeComboBox);
            task.Quadrant = quadrant;
            task.Experience = LifeActionTask.CalculateExperience(quadrant);
            task.DueAt = dueAt;
            task.UpdatedAt = DateTime.Now;

            _actionTaskRepository.Update(task);
            RefreshCurrentView(task.Id);
            LoadTaskForSelectedState(task.Id);

            MessageBox.Show("行动任务已保存。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存行动任务失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelEditTaskButton_Click(object sender, RoutedEventArgs e)
    {
        ClearEditTaskForm();
    }

    private void GenerateLifeRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个行动任务", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var task = _actionTaskRepository.GetById(selectedTask.Id);
            if (task is null)
            {
                MessageBox.Show("当前行动任务不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshCurrentView();
                ClearSelectedTaskState();
                return;
            }

            if (task.Status != ActionTaskStatus.Completed)
            {
                MessageBox.Show("只有已完成任务可以生成行为记录", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(task.LinkedLifeRecordId))
            {
                MessageBox.Show("当前任务已关联行为记录", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadTaskForSelectedState(task.Id);
                return;
            }

            var createdAt = task.CompletedAt ?? DateTime.Now;
            var record = new LifeRecord
            {
                Id = Guid.NewGuid().ToString(),
                Title = task.Title,
                Content = BuildLifeRecordContentFromTask(task),
                Category = string.IsNullOrWhiteSpace(task.Category) ? "未分类" : task.Category,
                Experience = task.Experience,
                CreatedAt = createdAt,
                UpdatedAt = DateTime.Now
            };

            _lifeRecordRepository.Create(record);
            _actionTaskRepository.LinkLifeRecord(task.Id, record.Id);

            RefreshCurrentView(task.Id);
            LoadTaskForSelectedState(task.Id);
            LifeRecordLinkChanged?.Invoke();

            MessageBox.Show("行为记录生成成功", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"生成行为记录失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LinkExistingLifeRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个行动任务", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var task = _actionTaskRepository.GetById(selectedTask.Id);
            if (task is null)
            {
                MessageBox.Show("当前行动任务不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshCurrentView();
                ClearSelectedTaskState();
                return;
            }

            var selectedRecord = ShowLifeRecordSelectionWindow();
            if (selectedRecord is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(task.LinkedLifeRecordId))
            {
                var overwrite = MessageBox.Show(
                    "当前任务已关联行为记录，继续操作会覆盖原有关联。是否继续？",
                    "LifeRPG.Native",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (overwrite != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var existingOwnerTask = _actionTaskRepository.GetByLinkedLifeRecordId(selectedRecord.Id);
            if (existingOwnerTask is not null &&
                !string.Equals(existingOwnerTask.Id, task.Id, StringComparison.Ordinal))
            {
                var overwriteExistingOwner = MessageBox.Show(
                    $"该行为记录已被任务“{existingOwnerTask.Title}”关联，是否改为关联到当前任务？",
                    "LifeRPG.Native",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (overwriteExistingOwner != MessageBoxResult.Yes)
                {
                    return;
                }

                _actionTaskRepository.UnlinkLifeRecord(existingOwnerTask.Id);
            }

            _actionTaskRepository.LinkLifeRecord(task.Id, selectedRecord.Id);
            RefreshCurrentView(task.Id);
            LoadTaskForSelectedState(task.Id);
            LifeRecordLinkChanged?.Invoke();

            MessageBox.Show("行为记录关联成功", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"关联行为记录失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UnlinkLifeRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个行动任务", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var task = _actionTaskRepository.GetById(selectedTask.Id);
            if (task is null)
            {
                MessageBox.Show("当前行动任务不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshCurrentView();
                ClearSelectedTaskState();
                return;
            }

            if (string.IsNullOrWhiteSpace(task.LinkedLifeRecordId))
            {
                MessageBox.Show("当前任务没有关联行为记录。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                "取消关联只会解除任务和行为记录的关系，不会删除行为记录。是否继续？",
                "LifeRPG.Native",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _actionTaskRepository.UnlinkLifeRecord(task.Id);
            RefreshCurrentView(task.Id);
            LoadTaskForSelectedState(task.Id);
            LifeRecordLinkChanged?.Invoke();

            MessageBox.Show("已取消关联。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"取消关联失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewLinkedLifeRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个行动任务", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var task = _actionTaskRepository.GetById(selectedTask.Id);
        if (task is null)
        {
            MessageBox.Show("当前行动任务不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshCurrentView();
            ClearSelectedTaskState();
            return;
        }

        if (string.IsNullOrWhiteSpace(task.LinkedLifeRecordId))
        {
            MessageBox.Show("当前任务没有关联行为记录。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var record = _lifeRecordRepository.GetById(task.LinkedLifeRecordId);
        if (record is null)
        {
            MessageBox.Show("关联的行为记录不存在或已被删除", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadTaskForSelectedState(task.Id);
            return;
        }

        LifeRecordSelectionRequested?.Invoke(record.Id);
    }

    private void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        CompleteSelectedTask();
    }

    private void ClearCompletionNoteButton_Click(object sender, RoutedEventArgs e)
    {
        CompletionNoteTextBox.Clear();
    }

    private void SelectCompletionImagesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.webp;*.bmp"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        foreach (var fileName in dialog.FileNames)
        {
            if (_pendingImagePaths.Any(path => string.Equals(path, fileName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            _pendingImagePaths.Add(fileName);
        }

        RefreshPendingImageList();
    }

    private void ClearPendingImagesButton_Click(object sender, RoutedEventArgs e)
    {
        _pendingImagePaths.Clear();
        RefreshPendingImageList();
    }

    private void OpenTaskImageFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var images = TaskImageEvidenceDataGrid.ItemsSource as IEnumerable<ActionTaskImageListItem>;
        var firstImage = images?.FirstOrDefault();
        if (firstImage is null)
        {
            MessageBox.Show("当前任务还没有图片证据", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var directoryPath = Path.GetDirectoryName(firstImage.FilePath);
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            MessageBox.Show("图片目录不存在或已被移动。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开图片目录失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenSelectedActionTaskImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedActionTaskImage is null)
        {
            MessageBox.Show("请先选择一张任务图片", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedActionTaskImagePath))
        {
            MessageBox.Show("未选择可打开的图片", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!File.Exists(_selectedActionTaskImagePath))
        {
            MessageBox.Show("图片文件不存在", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            ClearActionTaskImagePreview();
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _selectedActionTaskImagePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开原图失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearActionTaskImagePreviewButton_Click(object sender, RoutedEventArgs e)
    {
        ClearActionTaskImagePreview();
    }

    private void TaskImageEvidenceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        PreviewSelectedActionTaskImage(TaskImageEvidenceDataGrid.SelectedItem as ActionTaskImageListItem);
    }

    private void DeleteSelectedTaskImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个任务", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (TaskImageEvidenceDataGrid.SelectedItem is not ActionTaskImageListItem selectedImage)
        {
            MessageBox.Show("请先选择一张图片", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            "确认删除这张任务图片证据吗？",
            "LifeRPG.Native",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var image = _actionTaskImageRepository.GetById(selectedImage.Id);
            if (image is null)
            {
                MessageBox.Show("图片记录不存在或已被删除", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadTaskImages(selectedTask.Id);
                return;
            }

            TaskImageEvidenceDataGrid.SelectedItem = null;

            if (!string.IsNullOrWhiteSpace(_selectedActionTaskImagePath) &&
                string.Equals(_selectedActionTaskImagePath, image.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                ClearActionTaskImagePreview(clearGridSelection: false);
            }

            var directoryPath = Path.GetDirectoryName(image.FilePath);
            var fileDeleted = _actionTaskImageStorageService.DeleteImageFile(image.FilePath);

            _actionTaskImageRepository.Delete(image.Id);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                _actionTaskImageStorageService.TryDeleteEmptyDirectory(directoryPath);
            }

            LoadTaskImages(selectedTask.Id);

            if (fileDeleted)
            {
                MessageBox.Show("任务图片已删除", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("图片文件不存在，已清理数据库记录", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (IOException)
        {
            MessageBox.Show("图片文件删除失败，请确认文件是否被占用", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("图片文件删除失败，请确认文件是否被占用", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"数据库记录删除失败，请重新刷新或从备份恢复。{Environment.NewLine}{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个行动任务。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            "确定删除这个行动任务吗？关联的图片证据也会一并删除。",
            "LifeRPG.Native",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var images = _actionTaskImageRepository.GetByActionTaskId(selectedTask.Id);
            foreach (var image in images)
            {
                _actionTaskImageStorageService.DeleteImageFile(image.FilePath);
            }

            _actionTaskImageRepository.DeleteByActionTaskId(selectedTask.Id);
            _actionTaskImageStorageService.DeleteTaskImageFolderIfEmpty(selectedTask.Id);
            _actionTaskRepository.Delete(selectedTask.Id);

            if (string.Equals(_editingTaskId, selectedTask.Id, StringComparison.Ordinal))
            {
                _pendingImagePaths.Clear();
                RefreshPendingImageList();
                ClearActionTaskImagePreview(clearGridSelection: false);
                ClearSelectedTaskState();
            }

            RefreshCurrentView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除行动任务失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowAllTasks()
    {
        _currentView = ActionTaskView.All;
        RefreshCurrentView();
    }

    private void ExportActionTasksButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var tasks = _currentView switch
            {
                ActionTaskView.Today => _actionTaskRepository.GetTodayTasks(),
                ActionTaskView.Overdue => _actionTaskRepository.GetOverdueTasks(),
                ActionTaskView.Completed => _actionTaskRepository.GetCompletedTasks(),
                _ => _actionTaskRepository.GetAll()
            };

            var viewLabel = _currentView switch
            {
                ActionTaskView.Today => "今日",
                ActionTaskView.Overdue => "逾期",
                ActionTaskView.Completed => "已完成",
                _ => "全部"
            };

            var exportPath = _markdownExportService.ExportActionTasksToMarkdown(
                tasks, AppStoragePaths.ExportsDirectory);

            MessageBox.Show(
                $"行动任务 Markdown 导出成功（{viewLabel}视图，{tasks.Count} 条）\n{exportPath}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"导出行动任务 Markdown 失败：{ex.Message}",
                "LifeRPG.Native",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RefreshCurrentView(string? preserveSelectedTaskId = null)
    {
        var tasks = _currentView switch
        {
            ActionTaskView.Today => _actionTaskRepository.GetTodayTasks(),
            ActionTaskView.Overdue => _actionTaskRepository.GetOverdueTasks(),
            ActionTaskView.Completed => _actionTaskRepository.GetCompletedTasks(),
            _ => _actionTaskRepository.GetAll()
        };

        var items = tasks.Select(ToListItem).ToList();
        TaskDataGrid.ItemsSource = items;
        CurrentTaskViewTextBlock.Text = $"当前视图：{GetViewText(_currentView)}";
        TaskCountTextBlock.Text = $"当前显示 {tasks.Count} 条任务";
        RefreshActionTaskStatistics();

        var targetTaskId = preserveSelectedTaskId ?? _editingTaskId;
        if (string.IsNullOrWhiteSpace(targetTaskId))
        {
            return;
        }

        var selectedItem = items.FirstOrDefault(item => string.Equals(item.Id, targetTaskId, StringComparison.Ordinal));
            if (selectedItem is null)
        {
            if (string.Equals(_editingTaskId, targetTaskId, StringComparison.Ordinal))
            {
                ClearSelectedTaskState();
            }

            return;
        }

        TaskDataGrid.SelectedItem = selectedItem;
        TaskDataGrid.ScrollIntoView(selectedItem);
    }

    private void UpdateSelectedTaskStatus(ActionTaskStatus status)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个行动任务。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var task = _actionTaskRepository.GetById(selectedTask.Id);
            if (task is null)
            {
                MessageBox.Show("当前行动任务不存在，可能已被删除。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshCurrentView();
                return;
            }

            var wasCompleted = task.Status == ActionTaskStatus.Completed;

            task.Status = status;
            task.UpdatedAt = DateTime.Now;

            if (status != ActionTaskStatus.Completed)
            {
                task.CompletedAt = null;
            }

            _actionTaskRepository.Update(task);

            // Rollback RPG experience if un-completing a previously completed task
            if (task.Status != ActionTaskStatus.Completed && wasCompleted)
            {
                _rpgProgressionService.RollbackActionTaskCompletion(task);
                RpgProgressUpdated?.Invoke();
            }
            RefreshCurrentView(task.Id);
            if (string.Equals(_editingTaskId, task.Id, StringComparison.Ordinal))
            {
                LoadTaskForSelectedState(task.Id);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"切换任务状态失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool TryGetDueAt(out DateTime? dueAt)
    {
        return TryGetDueAt(TaskDueDatePicker, TaskDueTimeTextBox, requireDateForTime: false, out dueAt);
    }

    private static bool TryGetDueAt(DatePicker datePicker, System.Windows.Controls.TextBox timeTextBox, bool requireDateForTime, out DateTime? dueAt)
    {
        dueAt = null;
        var timeText = timeTextBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(timeText) &&
            !TimeSpan.TryParseExact(timeText, "hh\\:mm", CultureInfo.InvariantCulture, out _))
        {
            MessageBox.Show("截止时间格式应为 HH:mm", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!datePicker.SelectedDate.HasValue)
        {
            if (requireDateForTime && !string.IsNullOrWhiteSpace(timeText))
            {
                MessageBox.Show("请先选择截止日期", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(timeText))
        {
            dueAt = datePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
            return true;
        }

        var dueTime = TimeSpan.ParseExact(timeText, "hh\\:mm", CultureInfo.InvariantCulture);
        dueAt = datePicker.SelectedDate.Value.Date.Add(dueTime);
        return true;
    }

    private ActionTaskQuadrant GetSelectedQuadrant()
    {
        return GetSelectedQuadrant(TaskQuadrantComboBox);
    }

    private static ActionTaskQuadrant GetSelectedQuadrant(System.Windows.Controls.ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem { Tag: string tag } &&
            Enum.TryParse<ActionTaskQuadrant>(tag, out var quadrant))
        {
            return quadrant;
        }

        return ActionTaskQuadrant.ImportantNotUrgent;
    }

    private void LoadTaskForEdit(string taskId)
    {
        var task = _actionTaskRepository.GetById(taskId);
        if (task is null)
        {
            ClearSelectedTaskState();
            return;
        }

        _editingTaskId = task.Id;
        EditTaskStatusTextBlock.Text = $"正在编辑：{task.Title}";
        EditTaskTitleTextBox.Text = task.Title;
        EditTaskContentTextBox.Text = task.Content;
        EditTaskCategoryTextBox.Text = task.Category;
        SelectGrowthAttribute(EditTaskGrowthAttributeComboBox, task.GrowthAttribute);
        SelectQuadrant(EditTaskQuadrantComboBox, task.Quadrant);

        if (task.DueAt.HasValue)
        {
            EditTaskDueDatePicker.SelectedDate = task.DueAt.Value.Date;
            EditTaskDueTimeTextBox.Text = task.DueAt.Value.TimeOfDay == TimeSpan.FromTicks(TimeSpan.TicksPerDay - 1)
                ? string.Empty
                : task.DueAt.Value.ToString("HH:mm");
        }
        else
        {
            EditTaskDueDatePicker.SelectedDate = null;
            EditTaskDueTimeTextBox.Clear();
        }

        UpdateLinkedLifeRecordDisplay(task);
    }

    private void LoadTaskForCompletion(string taskId)
    {
        var task = _actionTaskRepository.GetById(taskId);
        if (task is null)
        {
            ClearCompletionTaskForm();
            return;
        }

        CompleteTaskStatusTextBlock.Text = $"准备完成：{task.Title}";
        CompletionNoteTextBox.Text = task.CompletionNote;
    }

    private void LoadTaskForSelectedState(string taskId)
    {
        if (!string.Equals(_editingTaskId, taskId, StringComparison.Ordinal))
        {
            _pendingImagePaths.Clear();
            RefreshPendingImageList();
        }

        ClearActionTaskImagePreview(clearGridSelection: false);
        LoadTaskForEdit(taskId);
        LoadTaskForCompletion(taskId);
        LoadTaskImages(taskId);
    }

    private void ClearEditTaskForm()
    {
        _editingTaskId = null;
        EditTaskStatusTextBlock.Text = "请选择一个任务进行编辑";
        EditTaskTitleTextBox.Clear();
        EditTaskContentTextBox.Clear();
        EditTaskCategoryTextBox.Clear();
        EditTaskGrowthAttributeComboBox.SelectedIndex = 0;
        EditTaskQuadrantComboBox.SelectedIndex = 1;
        EditTaskDueDatePicker.SelectedDate = null;
        EditTaskDueTimeTextBox.Clear();
        LinkedLifeRecordStatusTextBlock.Text = "关联行为记录：未关联";
        GenerateLifeRecordButton.IsEnabled = false;
        LinkExistingLifeRecordButton.IsEnabled = false;
        UnlinkLifeRecordButton.IsEnabled = false;
        ViewLinkedLifeRecordButton.IsEnabled = false;
    }

    private void ClearCompletionTaskForm()
    {
        CompleteTaskStatusTextBlock.Text = "请选择一个任务进行完成";
        CompletionNoteTextBox.Clear();
        _pendingImagePaths.Clear();
        RefreshPendingImageList();
        ClearActionTaskImagePreview(clearGridSelection: false);
        ClearAssociatedTaskImages();
    }

    private void ClearSelectedTaskState()
    {
        ClearEditTaskForm();
        ClearCompletionTaskForm();
    }

    private static void SelectQuadrant(System.Windows.Controls.ComboBox comboBox, ActionTaskQuadrant quadrant)
    {
        var targetTag = quadrant.ToString();
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, targetTag, StringComparison.Ordinal))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        comboBox.SelectedIndex = 1;
    }

    private void ClearCreateTaskForm()
    {
        TaskTitleTextBox.Clear();
        TaskContentTextBox.Clear();
        TaskCategoryTextBox.Clear();
        TaskGrowthAttributeComboBox.SelectedIndex = 0;
        TaskQuadrantComboBox.SelectedIndex = 1;
        TaskDueDatePicker.SelectedDate = null;
        TaskDueTimeTextBox.Clear();
    }

    private void CompleteSelectedTask()
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个任务", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var task = _actionTaskRepository.GetById(selectedTask.Id);
            if (task is null)
            {
                MessageBox.Show("任务不存在或已被删除", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshCurrentView();
                ClearSelectedTaskState();
                return;
            }

            if (task.Status == ActionTaskStatus.Completed)
            {
                MessageBox.Show("当前任务已经完成，不能重复发放成长经验。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadTaskForSelectedState(task.Id);
                return;
            }

            task.Status = ActionTaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
            task.CompletionNote = CompletionNoteTextBox.Text ?? string.Empty;
            task.Experience = LifeActionTask.CalculateExperience(task.Quadrant);
            task.UpdatedAt = DateTime.Now;

            _actionTaskRepository.Update(task);
            var imageSaveWarning = SavePendingImagesForTask(task);
            var experienceResult = _rpgProgressionService.ApplyActionTaskCompletion(task);
            RefreshCurrentView(task.Id);
            LoadTaskForSelectedState(task.Id);
            RpgProgressUpdated?.Invoke();

            if (string.IsNullOrWhiteSpace(imageSaveWarning))
            {
                MessageBox.Show(
                    experienceResult.IsDuplicate
                        ? "任务已完成，成长经验已存在，未重复发放。"
                        : "任务已完成，并已结算 RPG 成长经验。",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    experienceResult.IsDuplicate
                        ? $"任务已完成，但{imageSaveWarning}，且成长经验未重复发放。"
                        : $"任务已完成，并已结算 RPG 成长经验，但{imageSaveWarning}",
                    "LifeRPG.Native",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"完成任务失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string? SavePendingImagesForTask(LifeActionTask task)
    {
        if (_pendingImagePaths.Count == 0)
        {
            return null;
        }

        try
        {
            var copiedImages = _actionTaskImageStorageService.CopyImagesForTask(
                task.Id,
                task.Title,
                task.CreatedAt,
                _pendingImagePaths);

            foreach (var copiedImage in copiedImages)
            {
                _actionTaskImageRepository.Create(copiedImage);
            }

            _pendingImagePaths.Clear();
            RefreshPendingImageList();
            return null;
        }
        catch
        {
            return "部分图片可能未成功保存";
        }
    }

    private void UpdateLinkedLifeRecordDisplay(LifeActionTask task)
    {
        LinkExistingLifeRecordButton.IsEnabled = true;

        if (string.IsNullOrWhiteSpace(task.LinkedLifeRecordId))
        {
            LinkedLifeRecordStatusTextBlock.Text = "关联行为记录：未关联";
            GenerateLifeRecordButton.IsEnabled = task.Status == ActionTaskStatus.Completed;
            UnlinkLifeRecordButton.IsEnabled = false;
            ViewLinkedLifeRecordButton.IsEnabled = false;
            return;
        }

        var linkedRecord = _lifeRecordRepository.GetById(task.LinkedLifeRecordId);
        GenerateLifeRecordButton.IsEnabled = false;
        UnlinkLifeRecordButton.IsEnabled = true;

        if (linkedRecord is null)
        {
            LinkedLifeRecordStatusTextBlock.Text = "关联行为记录：关联的行为记录不存在或已被删除";
            ViewLinkedLifeRecordButton.IsEnabled = false;
            return;
        }

        LinkedLifeRecordStatusTextBlock.Text = $"关联行为记录：{linkedRecord.Title}";
        ViewLinkedLifeRecordButton.IsEnabled = true;
    }

    private static string BuildLifeRecordContentFromTask(LifeActionTask task)
    {
        var completionNote = task.CompletionNote?.Trim() ?? string.Empty;
        var content = task.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(completionNote) && string.IsNullOrWhiteSpace(content))
        {
            return $"来源行动任务：{task.Title}{Environment.NewLine}{Environment.NewLine}由行动任务生成，原任务未填写完成备注或任务内容。";
        }

        var sections = new List<string>
        {
            $"来源行动任务：{task.Title}"
        };

        if (!string.IsNullOrWhiteSpace(completionNote))
        {
            sections.Add($"完成备注：{Environment.NewLine}{completionNote}");
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            sections.Add($"原任务内容：{Environment.NewLine}{content}");
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", sections);
    }

    private LifeRecord? ShowLifeRecordSelectionWindow()
    {
        var records = _lifeRecordRepository.GetAll();
        var items = records.Select(record => new LifeRecordSelectionItem(record)).ToList();
        if (items.Count == 0)
        {
            MessageBox.Show("请先选择一条行为记录", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        var searchTextBox = new WpfTextBox
        {
            Margin = new Thickness(0, 8, 0, 0),
            MinWidth = 420
        };

        var listBox = new WpfListBox
        {
            Margin = new Thickness(0, 12, 0, 0),
            MinHeight = 260,
            Background = new MediaSolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#140C1E")!),
            BorderBrush = new MediaSolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#3A2A4A")!),
            Foreground = new MediaSolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#E8E1F2")!),
            DisplayMemberPath = nameof(LifeRecordSelectionItem.DisplayText),
            ItemsSource = items
        };

        if (items.Count > 0)
        {
            listBox.SelectedIndex = 0;
        }

        searchTextBox.TextChanged += (_, _) =>
        {
            var keyword = searchTextBox.Text.Trim();
            var filteredItems = string.IsNullOrWhiteSpace(keyword)
                ? items
                : items.Where(item => item.Record.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                      || item.Record.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                      || item.Record.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            listBox.ItemsSource = filteredItems;
            if (filteredItems.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }
        };

        var confirmButton = new WpfButton
        {
            Content = "确认关联",
            Width = 100,
            Height = 36,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };

        var cancelButton = new WpfButton
        {
            Content = "取消",
            Width = 90,
            Height = 36,
            IsCancel = true
        };

        var buttons = new WrapPanel
        {
            Margin = new Thickness(0, 14, 0, 0)
        };
        buttons.Children.Add(confirmButton);
        buttons.Children.Add(cancelButton);

        var panel = new StackPanel
        {
            Margin = new Thickness(18)
        };
        panel.Children.Add(new TextBlock
        {
            Text = "选择要关联的行为记录",
            FontWeight = FontWeights.Bold,
            Foreground = new MediaSolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#7CFF6B")!)
        });
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 8, 0, 0),
            Text = "可按标题、内容或分类搜索。",
            Foreground = new MediaSolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#B7A8C8")!)
        });
        panel.Children.Add(searchTextBox);
        panel.Children.Add(listBox);
        panel.Children.Add(buttons);

        var window = new Window
        {
            Title = "关联已有行为记录",
            Width = 560,
            Height = 460,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            Background = new MediaSolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#21162C")!),
            Content = panel,
            ResizeMode = ResizeMode.NoResize
        };

        LifeRecord? selectedRecord = null;
        confirmButton.Click += (_, _) =>
        {
            if (listBox.SelectedItem is not LifeRecordSelectionItem selectedItem)
            {
                MessageBox.Show("请先选择一条行为记录", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            selectedRecord = selectedItem.Record;
            window.DialogResult = true;
            window.Close();
        };

        return window.ShowDialog() == true ? selectedRecord : null;
    }

    private void RefreshActionTaskStatistics()
    {
        try
        {
            var statistics = _actionTaskRepository.GetStatistics();

            TotalTasksTextBlock.Text = statistics.TotalTasks.ToString();
            NotStartedTasksTextBlock.Text = statistics.NotStartedTasks.ToString();
            InProgressTasksTextBlock.Text = statistics.InProgressTasks.ToString();
            CompletedTasksTextBlock.Text = statistics.CompletedTasks.ToString();
            CancelledTasksTextBlock.Text = statistics.CancelledTasks.ToString();
            OverdueTasksTextBlock.Text = statistics.OverdueTasks.ToString();
            DueTodayTasksTextBlock.Text = statistics.DueTodayTasks.ToString();
            DueThisWeekTasksTextBlock.Text = statistics.DueThisWeekTasks.ToString();
            TotalTaskExperienceTextBlock.Text = statistics.TotalExperience.ToString();
            CompletedTaskExperienceTextBlock.Text = statistics.CompletedExperience.ToString();

            QuadrantStatisticsItemsControl.ItemsSource = statistics.QuadrantStatistics
                .Select(item => new StatisticDisplayItem($"{item.QuadrantDisplayName}：{item.Count} 个 / {item.Experience} EXP"))
                .ToList();

            AttributeStatisticsItemsControl.ItemsSource = statistics.AttributeStatistics
                .Select(item => new StatisticDisplayItem($"{item.GrowthAttribute}：{item.Count} 个 / {item.Experience} EXP"))
                .ToList();

            ActionTaskStatisticsEmptyTextBlock.Visibility = statistics.TotalTasks == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        catch
        {
            TotalTasksTextBlock.Text = "0";
            NotStartedTasksTextBlock.Text = "0";
            InProgressTasksTextBlock.Text = "0";
            CompletedTasksTextBlock.Text = "0";
            CancelledTasksTextBlock.Text = "0";
            OverdueTasksTextBlock.Text = "0";
            DueTodayTasksTextBlock.Text = "0";
            DueThisWeekTasksTextBlock.Text = "0";
            TotalTaskExperienceTextBlock.Text = "0";
            CompletedTaskExperienceTextBlock.Text = "0";
            QuadrantStatisticsItemsControl.ItemsSource = null;
            AttributeStatisticsItemsControl.ItemsSource = null;
            ActionTaskStatisticsEmptyTextBlock.Visibility = Visibility.Visible;
        }
    }

    private void LoadTaskImages(string taskId)
    {
        ClearActionTaskImagePreview(clearGridSelection: false);
        var images = _actionTaskImageRepository.GetByActionTaskId(taskId)
            .Select(image => new ActionTaskImageListItem
            {
                Id = image.Id,
                FileName = image.FileName,
                OriginalFileName = image.OriginalFileName,
                FilePath = image.FilePath,
                RelativePath = image.RelativePath,
                FileSizeText = FormatFileSize(image.FileSizeBytes),
                CreatedAtText = image.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();

        TaskImageEvidenceDataGrid.ItemsSource = images;
        AssociatedTaskImagesCountTextBlock.Text = $"已关联图片：{images.Count} 张";
    }

    public void FocusTask(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return;
        }

        _currentView = ActionTaskView.All;
        RefreshCurrentView(taskId);
        LoadTaskForSelectedState(taskId);
    }

    private void RefreshPendingImageList()
    {
        PendingTaskImagesListBox.ItemsSource = null;
        PendingTaskImagesListBox.ItemsSource = _pendingImagePaths.Select(Path.GetFileName).ToList();
        PendingTaskImagesCountTextBlock.Text = $"待上传图片：{_pendingImagePaths.Count} 张";
    }

    private void ClearAssociatedTaskImages()
    {
        ClearActionTaskImagePreview(clearGridSelection: false);
        TaskImageEvidenceDataGrid.ItemsSource = null;
        TaskImageEvidenceDataGrid.SelectedItem = null;
        AssociatedTaskImagesCountTextBlock.Text = "已关联图片：0 张";
    }

    private void PreviewSelectedActionTaskImage(ActionTaskImageListItem? imageItem)
    {
        if (imageItem is null)
        {
            ClearActionTaskImagePreview(clearGridSelection: false);
            return;
        }

        _selectedActionTaskImage = imageItem;
        _selectedActionTaskImagePath = imageItem.FilePath;

        if (string.IsNullOrWhiteSpace(imageItem.FilePath) || !File.Exists(imageItem.FilePath))
        {
            MessageBox.Show("图片文件不存在", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            ClearActionTaskImagePreview(clearGridSelection: false);
            return;
        }

        try
        {
            ActionTaskImagePreview.Source = LoadBitmapWithoutLock(imageItem.FilePath);
            SelectedActionTaskImageNameText.Text = string.IsNullOrWhiteSpace(imageItem.OriginalFileName)
                ? imageItem.FileName
                : imageItem.OriginalFileName;
        }
        catch
        {
            MessageBox.Show("图片预览失败", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            ClearActionTaskImagePreview(clearGridSelection: false);
        }
    }

    private void ClearActionTaskImagePreview(bool clearGridSelection = true)
    {
        ActionTaskImagePreview.Source = null;
        SelectedActionTaskImageNameText.Text = "未选择图片";
        _selectedActionTaskImage = null;
        _selectedActionTaskImagePath = null;

        if (clearGridSelection)
        {
            TaskImageEvidenceDataGrid.SelectedItem = null;
        }
    }

    private static string FormatFileSize(long fileSizeBytes)
    {
        const double kb = 1024;
        const double mb = kb * 1024;

        if (fileSizeBytes >= mb)
        {
            return $"{fileSizeBytes / mb:F1} MB";
        }

        if (fileSizeBytes >= kb)
        {
            return $"{fileSizeBytes / kb:F1} KB";
        }

        return $"{fileSizeBytes} B";
    }

    private static BitmapImage LoadBitmapWithoutLock(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }

    private static ActionTaskListItem ToListItem(LifeActionTask task)
    {
        return new ActionTaskListItem
        {
            Id = task.Id,
            Title = task.Title,
            Category = task.Category,
            GrowthAttribute = string.IsNullOrWhiteSpace(task.GrowthAttribute) ? "无" : task.GrowthAttribute,
            StatusText = GetStatusText(task.Status),
            QuadrantText = GetQuadrantText(task.Quadrant),
            Experience = task.Experience,
            DueAtText = task.DueAt?.ToString("yyyy-MM-dd HH:mm") ?? "无",
            CreatedAtText = task.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            IsOverdue = task.DueAt.HasValue && task.DueAt.Value < DateTime.Now
                && task.Status != ActionTaskStatus.Completed
                && task.Status != ActionTaskStatus.Cancelled
        };
    }

    private static string GetStatusText(ActionTaskStatus status)
    {
        return status switch
        {
            ActionTaskStatus.NotStarted => "待开始",
            ActionTaskStatus.InProgress => "进行中",
            ActionTaskStatus.Completed => "已完成",
            ActionTaskStatus.Cancelled => "已取消",
            _ => "待开始"
        };
    }

    private static string GetQuadrantText(ActionTaskQuadrant quadrant)
    {
        return quadrant switch
        {
            ActionTaskQuadrant.ImportantUrgent => "重要且紧急",
            ActionTaskQuadrant.ImportantNotUrgent => "重要不紧急",
            ActionTaskQuadrant.NotImportantUrgent => "不重要但紧急",
            ActionTaskQuadrant.NotImportantNotUrgent => "不重要不紧急",
            _ => "重要不紧急"
        };
    }

    private static string GetViewText(ActionTaskView view)
    {
        return view switch
        {
            ActionTaskView.Today => "今日任务",
            ActionTaskView.Overdue => "逾期任务",
            ActionTaskView.Completed => "已完成任务",
            _ => "全部任务"
        };
    }

    private enum ActionTaskView
    {
        Today,
        Overdue,
        All,
        Completed
    }

    private sealed class LifeRecordSelectionItem
    {
        public LifeRecordSelectionItem(LifeRecord record)
        {
            Record = record;
            var title = string.IsNullOrWhiteSpace(record.Title) ? "未命名记录" : record.Title.Trim();
            var category = string.IsNullOrWhiteSpace(record.Category) ? "未分类" : record.Category.Trim();
            DisplayText = $"{title} | 分类：{category} | 经验：{record.Experience} | 创建：{record.CreatedAt:yyyy-MM-dd HH:mm}";
        }

        public LifeRecord Record { get; }

        public string DisplayText { get; }
    }

    private sealed class StatisticDisplayItem
    {
        public StatisticDisplayItem(string displayText)
        {
            DisplayText = displayText;
        }

        public string DisplayText { get; }
    }

    private static System.Windows.Controls.ScrollViewer? FindParentScrollViewer(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is System.Windows.Controls.ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            current = GetParentObject(current);
        }

        return null;
    }

    private static bool IsDropDownOpen(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is System.Windows.Controls.ComboBox comboBox && comboBox.IsDropDownOpen)
            {
                return true;
            }

            if (current is System.Windows.Controls.DatePicker datePicker && datePicker.IsDropDownOpen)
            {
                return true;
            }

            current = GetParentObject(current);
        }

        return false;
    }

    private static DependencyObject? GetParentObject(DependencyObject? source)
    {
        if (source is null)
        {
            return null;
        }

        if (source is Visual || source is Visual3D)
        {
            return VisualTreeHelper.GetParent(source);
        }

        return source switch
        {
            FrameworkElement frameworkElement => frameworkElement.Parent,
            FrameworkContentElement frameworkContentElement => frameworkContentElement.Parent,
            _ => null
        };
    }

    private static string GetGrowthAttributeFromComboBox(System.Windows.Controls.ComboBox comboBox)
    {
        var item = comboBox.SelectedItem as ComboBoxItem;
        if (item is null)
        {
            return string.Empty;
        }

        var tag = item.Tag?.ToString();
        return tag ?? item.Content?.ToString() ?? string.Empty;
    }

    private static void SelectGrowthAttribute(System.Windows.Controls.ComboBox comboBox, string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        foreach (ComboBoxItem item in comboBox.Items)
        {
            var content = item.Content?.ToString() ?? string.Empty;
            var tag = item.Tag?.ToString();

            if (string.IsNullOrEmpty(normalized) && (string.IsNullOrEmpty(tag) || content == "未设置（通用经验）"))
            {
                item.IsSelected = true;
                return;
            }

            if (string.Equals(content, normalized, StringComparison.OrdinalIgnoreCase))
            {
                item.IsSelected = true;
                return;
            }
        }

        comboBox.SelectedIndex = 0;
    }
}
