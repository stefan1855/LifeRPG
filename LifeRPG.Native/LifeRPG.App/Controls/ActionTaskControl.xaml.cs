using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using LifeRPG.App.ViewModels;
using LifeRPG.Core.Models;
using LifeRPG.Data.Repositories;
using LifeActionTask = LifeRPG.Core.Models.ActionTask;
using MessageBox = System.Windows.MessageBox;

namespace LifeRPG.App.Controls;

public partial class ActionTaskControl : System.Windows.Controls.UserControl
{
    private readonly ActionTaskRepository _actionTaskRepository = new();
    private ActionTaskView _currentView = ActionTaskView.All;
    private bool _hasLoaded;
    private string? _editingTaskId;

    public ActionTaskControl()
    {
        InitializeComponent();
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
                GrowthAttribute = TaskGrowthAttributeTextBox.Text?.Trim() ?? string.Empty,
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
            task.GrowthAttribute = EditTaskGrowthAttributeTextBox.Text?.Trim() ?? string.Empty;
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

    private void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        CompleteSelectedTask();
    }

    private void ClearCompletionNoteButton_Click(object sender, RoutedEventArgs e)
    {
        CompletionNoteTextBox.Clear();
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (TaskDataGrid.SelectedItem is not ActionTaskListItem selectedTask)
        {
            MessageBox.Show("请先选择一个行动任务。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            "确定删除这个行动任务吗？",
            "LifeRPG.Native",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _actionTaskRepository.Delete(selectedTask.Id);
            if (string.Equals(_editingTaskId, selectedTask.Id, StringComparison.Ordinal))
            {
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

            task.Status = status;
            task.UpdatedAt = DateTime.Now;

            task.CompletedAt = null;

            _actionTaskRepository.Update(task);
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
        EditTaskGrowthAttributeTextBox.Text = task.GrowthAttribute;
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
        LoadTaskForEdit(taskId);
        LoadTaskForCompletion(taskId);
    }

    private void ClearEditTaskForm()
    {
        _editingTaskId = null;
        EditTaskStatusTextBlock.Text = "请选择一个任务进行编辑";
        EditTaskTitleTextBox.Clear();
        EditTaskContentTextBox.Clear();
        EditTaskCategoryTextBox.Clear();
        EditTaskGrowthAttributeTextBox.Clear();
        EditTaskQuadrantComboBox.SelectedIndex = 1;
        EditTaskDueDatePicker.SelectedDate = null;
        EditTaskDueTimeTextBox.Clear();
    }

    private void ClearCompletionTaskForm()
    {
        CompleteTaskStatusTextBlock.Text = "请选择一个任务进行完成";
        CompletionNoteTextBox.Clear();
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
        TaskGrowthAttributeTextBox.Clear();
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

            task.Status = ActionTaskStatus.Completed;
            task.CompletedAt = DateTime.Now;
            task.CompletionNote = CompletionNoteTextBox.Text ?? string.Empty;
            task.Experience = LifeActionTask.CalculateExperience(task.Quadrant);
            task.UpdatedAt = DateTime.Now;

            _actionTaskRepository.Update(task);
            RefreshCurrentView(task.Id);
            LoadTaskForSelectedState(task.Id);

            MessageBox.Show("任务已完成", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"完成任务失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            CreatedAtText = task.CreatedAt.ToString("yyyy-MM-dd HH:mm")
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
}
