using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using maktask.Models;
using maktask.Services;

namespace maktask.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly TabService _tabService;

    public ObservableCollection<ProjectViewModel> Projects { get; } = new();
    public ObservableCollection<TaskItem> VisibleTasks { get; } = new();

    [ObservableProperty]
    private int _selectedViewIndex = 0;

    [ObservableProperty]
    private DateTime _currentMonth = DateTime.Today;

    public HomeViewModel()
    {
        _dataService = DataService.Instance;
        _tabService = TabService.Instance;
        _dataService.DataChanged += (s, e) => RefreshData();
    }

    public void RefreshData()
    {
        Projects.Clear();
        foreach (var project in _dataService.GetProjects())
        {
            var vm = new ProjectViewModel(project);
            vm.VisibilityChanged += OnProjectVisibilityChanged;
            Projects.Add(vm);
        }
        RefreshTasks();
    }

    private void OnProjectVisibilityChanged(object? sender, bool isVisible)
    {
        RefreshTasks();
    }

    private void RefreshTasks()
    {
        VisibleTasks.Clear();
        var visibleProjectIds = Projects.Where(p => p.IsVisible).Select(p => p.Project.Id);
        foreach (var task in _dataService.GetTasksForProjects(visibleProjectIds).OrderBy(t => t.StartDateTime))
        {
            VisibleTasks.Add(task);
        }
    }

    [RelayCommand]
    private void CreateProject()
    {
        _tabService.OpenProjectCreateTab();
    }

    [RelayCommand]
    private void OpenProjectDetail(ProjectViewModel projectVm)
    {
        _tabService.OpenProjectDetailTab(projectVm.Project);
    }

    [RelayCommand]
    private async Task DeleteProject(ProjectViewModel projectVm)
    {
        await _dataService.DeleteProjectAsync(projectVm.Project.Id);
    }

    [RelayCommand]
    private void CreateTask()
    {
        _tabService.OpenTaskCreateTab();
    }

    [RelayCommand]
    private void CreateTaskForDate(DateTime date)
    {
        _tabService.OpenTaskCreateTab(null, date);
    }

    [RelayCommand]
    private void CreateTaskForProject(Guid projectId)
    {
        _tabService.OpenTaskCreateTab(projectId);
    }

    [RelayCommand]
    private void OpenTaskDetail(TaskItem task)
    {
        _tabService.OpenTaskDetailTab(task);
    }

    [RelayCommand]
    private async Task DeleteTask(TaskItem task)
    {
        await _dataService.DeleteTaskAsync(task.Id);
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
    }

    [RelayCommand]
    private void NextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
    }

    [RelayCommand]
    private void GoToToday()
    {
        CurrentMonth = DateTime.Today;
    }

    public Project? GetProjectForTask(TaskItem task)
    {
        return _dataService.GetProject(task.ProjectId);
    }
}

public partial class ProjectViewModel : ObservableObject
{
    public Project Project { get; }

    [ObservableProperty]
    private bool _isVisible;

    public event EventHandler<bool>? VisibilityChanged;

    public ProjectViewModel(Project project)
    {
        Project = project;
        _isVisible = project.IsVisible;
    }

    partial void OnIsVisibleChanged(bool value)
    {
        Project.IsVisible = value;
        _ = DataService.Instance.UpdateProjectAsync(Project);
        VisibilityChanged?.Invoke(this, value);
    }
}
