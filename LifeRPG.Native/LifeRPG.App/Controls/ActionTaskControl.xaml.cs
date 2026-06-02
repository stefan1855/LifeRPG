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
        UpdateSelectedTaskStatus(ActionTaskStatus.Completed);
    }

    private void MarkCancelledButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSelectedTaskStatus(ActionTaskStatus.Cancelled);
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

    private void RefreshCurrentView()
    {
        var tasks = _currentView switch
        {
            ActionTaskView.Today => _actionTaskRepository.GetTodayTasks(),
            ActionTaskView.Overdue => _actionTaskRepository.GetOverdueTasks(),
            ActionTaskView.Completed => _actionTaskRepository.GetCompletedTasks(),
            _ => _actionTaskRepository.GetAll()
        };

        TaskDataGrid.ItemsSource = tasks.Select(ToListItem).ToList();
        CurrentTaskViewTextBlock.Text = $"当前视图：{GetViewText(_currentView)}";
        TaskCountTextBlock.Text = $"当前显示 {tasks.Count} 条任务";
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

            if (status == ActionTaskStatus.Completed)
            {
                task.CompletedAt = DateTime.Now;
                if (task.Experience <= 0)
                {
                    task.Experience = LifeActionTask.CalculateExperience(task.Quadrant);
                }
            }
            else
            {
                task.CompletedAt = null;
            }

            _actionTaskRepository.Update(task);
            RefreshCurrentView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"切换任务状态失败：{ex.Message}", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool TryGetDueAt(out DateTime? dueAt)
    {
        dueAt = null;
        var timeText = TaskDueTimeTextBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(timeText) &&
            !TimeSpan.TryParseExact(timeText, "hh\\:mm", CultureInfo.InvariantCulture, out _))
        {
            MessageBox.Show("截止时间格式应为 HH:mm", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!TaskDueDatePicker.SelectedDate.HasValue)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(timeText))
        {
            dueAt = TaskDueDatePicker.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
            return true;
        }

        var dueTime = TimeSpan.ParseExact(timeText, "hh\\:mm", CultureInfo.InvariantCulture);
        dueAt = TaskDueDatePicker.SelectedDate.Value.Date.Add(dueTime);
        return true;
    }

    private ActionTaskQuadrant GetSelectedQuadrant()
    {
        if (TaskQuadrantComboBox.SelectedItem is ComboBoxItem { Tag: string tag } &&
            Enum.TryParse<ActionTaskQuadrant>(tag, out var quadrant))
        {
            return quadrant;
        }

        return ActionTaskQuadrant.ImportantNotUrgent;
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
