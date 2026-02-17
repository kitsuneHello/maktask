using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using maktask.Models;
using maktask.Services;

namespace maktask.ViewModels;

public partial class TaskDetailViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly TabService _tabService;

    [ObservableProperty]
    private TaskItem? _task;

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private string _taskName = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _startDate;

    [ObservableProperty]
    private DateTimeOffset _endDate;

    [ObservableProperty]
    private TimeSpan _startTime;

    [ObservableProperty]
    private TimeSpan _endTime;

    [ObservableProperty]
    private bool _isAllDay;

    [ObservableProperty]
    private bool _isDeadlineMode;

    [ObservableProperty]
    private string _memo = string.Empty;

    [ObservableProperty]
    private bool _isEditing;

    public ObservableCollection<SubtaskItemViewModel> Subtasks { get; } = new();
    public ObservableCollection<ProgressLog> Logs { get; } = new();

    [ObservableProperty]
    private string _newLogTitle = string.Empty;

    [ObservableProperty]
    private string _newLogMemo = string.Empty;

    public TaskDetailViewModel()
    {
        _dataService = DataService.Instance;
        _tabService = TabService.Instance;
        _dataService.DataChanged += (s, e) => LoadLogs();
    }

    public void Load(Guid taskId)
    {
        Task = _dataService.GetTask(taskId);
        if (Task != null)
        {
            Project = _dataService.GetProject(Task.ProjectId);
            LoadTaskData();
            LoadSubtasks();
            LoadLogs();
        }
    }

    private void LoadTaskData()
    {
        if (Task == null) return;

        TaskName = Task.Name;
        StartDate = new DateTimeOffset(Task.StartDateTime.Date);
        EndDate = new DateTimeOffset(Task.EndDateTime.Date);
        StartTime = Task.StartDateTime.TimeOfDay;
        EndTime = Task.EndDateTime.TimeOfDay;
        IsAllDay = Task.IsAllDay;
        IsDeadlineMode = Task.IsDeadlineMode;
        Memo = Task.Memo;
    }

    private void LoadSubtasks()
    {
        if (Task == null) return;
        Subtasks.Clear();
        foreach (var subtask in Task.Subtasks)
        {
            var vm = new SubtaskItemViewModel(subtask);
            vm.CompletionChanged += OnSubtaskCompletionChanged;
            Subtasks.Add(vm);
        }
    }

    private void LoadLogs()
    {
        if (Task == null) return;
        Logs.Clear();
        foreach (var log in _dataService.GetLogsForTask(Task.Id))
        {
            Logs.Add(log);
        }
    }

    private async void OnSubtaskCompletionChanged(object? sender, bool isCompleted)
    {
        if (Task != null)
        {
            await _dataService.UpdateTaskAsync(Task);
        }
    }

    [RelayCommand]
    private void StartEdit()
    {
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveEdit()
    {
        if (Task == null || string.IsNullOrWhiteSpace(TaskName)) return;

        Task.Name = TaskName;
        Task.StartDateTime = IsAllDay ? StartDate.Date : StartDate.Date.Add(StartTime);
        Task.EndDateTime = IsAllDay ? EndDate.Date : EndDate.Date.Add(EndTime);
        Task.IsAllDay = IsAllDay;
        Task.IsDeadlineMode = IsDeadlineMode;
        Task.Deadline = IsDeadlineMode ? EndDate.Date : null;
        Task.Memo = Memo;

        await _dataService.UpdateTaskAsync(Task);
        IsEditing = false;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        LoadTaskData();
        IsEditing = false;
    }

    [RelayCommand]
    private async Task AddSubtask(Subtask subtask)
    {
        if (Task == null || subtask == null) return;

        Task.Subtasks.Add(subtask);
        await _dataService.UpdateTaskAsync(Task);

        var vm = new SubtaskItemViewModel(subtask);
        vm.CompletionChanged += OnSubtaskCompletionChanged;
        Subtasks.Add(vm);
    }

    [RelayCommand]
    private async Task DeleteSubtask(Subtask subtask)
    {
        if (Task == null || subtask == null) return;

        Task.Subtasks.Remove(subtask);
        await _dataService.UpdateTaskAsync(Task);

        var vm = Subtasks.FirstOrDefault(s => s.Subtask == subtask);
        if (vm != null)
        {
            Subtasks.Remove(vm);
        }
    }

    [RelayCommand]
    private async Task AddLog()
    {
        if (Task == null || string.IsNullOrWhiteSpace(NewLogTitle)) return;

        var log = new ProgressLog
        {
            TaskId = Task.Id,
            Title = NewLogTitle,
            Memo = NewLogMemo,
            LogDateTime = DateTime.Now
        };

        await _dataService.AddLogAsync(log);
        NewLogTitle = string.Empty;
        NewLogMemo = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteLog(ProgressLog log)
    {
        await _dataService.DeleteLogAsync(log.Id);
    }

    [RelayCommand]
    private async Task DeleteTask()
    {
        if (Task == null) return;

        var taskId = Task.Id;
        await _dataService.DeleteTaskAsync(taskId);

        // タブを閉じる
        var tab = _tabService.Tabs.FirstOrDefault(t => t.Type == TabType.TaskDetail && t.RelatedId == taskId);
        if (tab != null)
        {
            _tabService.CloseTab(tab.Id);
        }
    }
}

public partial class SubtaskItemViewModel : ObservableObject
{
    public Subtask Subtask { get; }

    [ObservableProperty]
    private bool _isCompleted;

    public event EventHandler<bool>? CompletionChanged;

    public SubtaskItemViewModel(Subtask subtask)
    {
        Subtask = subtask;
        _isCompleted = subtask.IsCompleted;
    }

    partial void OnIsCompletedChanged(bool value)
    {
        Subtask.IsCompleted = value;
        CompletionChanged?.Invoke(this, value);
    }
}
