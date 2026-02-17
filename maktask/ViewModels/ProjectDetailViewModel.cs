using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using maktask.Models;
using maktask.Services;

namespace maktask.ViewModels;

public partial class ProjectDetailViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly TabService _tabService;

    [ObservableProperty]
    private Project? _project;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _themeColor = "#0078D4";

    [ObservableProperty]
    private bool _isEditing;

    public ObservableCollection<TaskItem> Tasks { get; } = new();

    public ProjectDetailViewModel()
    {
        _dataService = DataService.Instance;
        _tabService = TabService.Instance;
        _dataService.DataChanged += (s, e) => LoadTasks();
    }

    public void Load(Guid projectId)
    {
        Project = _dataService.GetProject(projectId);
        if (Project != null)
        {
            ProjectName = Project.Name;
            ThemeColor = Project.ThemeColor;
            LoadTasks();
        }
    }

    private void LoadTasks()
    {
        if (Project == null) return;
        Tasks.Clear();
        foreach (var task in _dataService.GetTasksForProject(Project.Id).OrderBy(t => t.StartDateTime))
        {
            Tasks.Add(task);
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
        if (Project == null || string.IsNullOrWhiteSpace(ProjectName)) return;

        Project.Name = ProjectName;
        Project.ThemeColor = ThemeColor;
        await _dataService.UpdateProjectAsync(Project);
        IsEditing = false;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (Project != null)
        {
            ProjectName = Project.Name;
            ThemeColor = Project.ThemeColor;
        }
        IsEditing = false;
    }

    [RelayCommand]
    private void CreateTask()
    {
        if (Project != null)
        {
            _tabService.OpenTaskCreateTab(Project.Id);
        }
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
}
