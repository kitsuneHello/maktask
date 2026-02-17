using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using maktask.Models;
using maktask.Services;

namespace maktask.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DataService _dataService;
    private readonly TabService _tabService;

    public ObservableCollection<TabItem> Tabs => _tabService.Tabs;

    [ObservableProperty]
    private TabItem? _selectedTab;

    public MainViewModel()
    {
        _dataService = DataService.Instance;
        _tabService = TabService.Instance;
        _tabService.TabAdded += OnTabAdded;
    }

    public async Task InitializeAsync()
    {
        await _dataService.LoadDataAsync();
        _tabService.Initialize();
        SelectedTab = Tabs.FirstOrDefault();
    }

    private void OnTabAdded(object? sender, TabItem tab)
    {
        SelectedTab = tab;
    }

    [RelayCommand]
    private void CloseTab(TabItem tab)
    {
        if (tab.CanClose)
        {
            _tabService.CloseTab(tab.Id);
            if (SelectedTab == tab)
            {
                SelectedTab = Tabs.FirstOrDefault();
            }
        }
    }
}
