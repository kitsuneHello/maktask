using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using maktask.Models;
using maktask.Services;

namespace maktask.ViewModels;

public partial class TaskCreateViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly TabService _tabService;

    public ObservableCollection<Project> Projects { get; } = new();

    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    private string _taskName = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _startDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset _endDate = DateTimeOffset.Now;

    [ObservableProperty]
    private TimeSpan _startTime = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan _endTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59));

    [ObservableProperty]
    private bool _isAllDay = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndDateLabel))]
    [NotifyPropertyChangedFor(nameof(EndTimeLabel))]
    private bool _isDeadlineMode = false;

    [ObservableProperty]
    private string _memo = string.Empty;

    public string EndDateLabel => IsDeadlineMode ? "期限日" : "終了日";
    public string EndTimeLabel => IsDeadlineMode ? "期限時刻" : "終了時刻";

    public ObservableCollection<SubtaskViewModel> Subtasks { get; } = new();

    public event EventHandler<TaskItem>? Created;

    public TaskCreateViewModel()
    {
        _dataService = DataService.Instance;
        _tabService = TabService.Instance;
    }

    public void Initialize(TaskCreateParameter? param)
    {
        Projects.Clear();
        foreach (var project in _dataService.GetProjects())
        {
            Projects.Add(project);
        }

        if (param != null)
        {
            StartDate = new DateTimeOffset(param.InitialDate);
            EndDate = StartDate;

            if (param.ProjectId.HasValue)
            {
                SelectedProject = Projects.FirstOrDefault(p => p.Id == param.ProjectId.Value);
            }
        }
    }

    [RelayCommand]
    private void AddSubtask()
    {
        Subtasks.Add(new SubtaskViewModel
        {
            StartDate = StartDate,
            EndDate = EndDate
        });
    }

    [RelayCommand]
    private void RemoveSubtask(SubtaskViewModel subtask)
    {
        Subtasks.Remove(subtask);
    }

    [RelayCommand]
    private async Task Create()
    {
        if (SelectedProject == null || string.IsNullOrWhiteSpace(TaskName)) return;

        var task = new TaskItem
        {
            ProjectId = SelectedProject.Id,
            Name = TaskName,
            StartDateTime = IsAllDay ? StartDate.Date : StartDate.Date.Add(StartTime),
            EndDateTime = IsAllDay ? EndDate.Date : EndDate.Date.Add(EndTime),
            IsAllDay = IsAllDay,
            IsDeadlineMode = IsDeadlineMode,
            Deadline = IsDeadlineMode ? EndDate.Date : null,
            Memo = Memo
        };

        foreach (var subtaskVm in Subtasks)
        {
            task.Subtasks.Add(new Subtask
            {
                TaskId = task.Id,
                Name = subtaskVm.Name,
                StartDateTime = subtaskVm.IsAllDay ? subtaskVm.StartDate.Date : subtaskVm.StartDate.DateTime,
                EndDateTime = subtaskVm.IsAllDay ? subtaskVm.EndDate.Date : subtaskVm.EndDate.DateTime,
                IsAllDay = subtaskVm.IsAllDay,
                IsDeadlineMode = subtaskVm.IsDeadlineMode
            });
        }

        await _dataService.AddTaskAsync(task);
        Created?.Invoke(this, task);
    }

    public bool CanCreate => SelectedProject != null && !string.IsNullOrWhiteSpace(TaskName);
}

public partial class SubtaskViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _startDate = DateTimeOffset.Now;

    [ObservableProperty]
    private DateTimeOffset _endDate = DateTimeOffset.Now;

    [ObservableProperty]
    private bool _isAllDay = true;

    [ObservableProperty]
    private bool _isDeadlineMode = false;
}
