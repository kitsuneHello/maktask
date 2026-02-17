using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using maktask.Models;
using maktask.Services;

namespace maktask.ViewModels;

public partial class ProjectCreateViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly TabService _tabService;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private string _themeColor = "#0078D4";

    public event EventHandler? Created;

    public ProjectCreateViewModel()
    {
        _dataService = DataService.Instance;
        _tabService = TabService.Instance;
    }

    [RelayCommand]
    private async Task Create()
    {
        if (string.IsNullOrWhiteSpace(ProjectName)) return;

        var project = new Project
        {
            Name = ProjectName,
            ThemeColor = ThemeColor
        };

        await _dataService.AddProjectAsync(project);
        Created?.Invoke(this, EventArgs.Empty);
    }

    public bool CanCreate => !string.IsNullOrWhiteSpace(ProjectName);
}
