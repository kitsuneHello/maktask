using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI;
using System;
using System.Linq;
using maktask.Models;
using maktask.Services;
using maktask.ViewModels;
using maktask.Views;
using Windows.UI;

namespace maktask
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly TabService _tabService;
        private readonly DataService _dataService;
        private HomeViewModel? _homeViewModel;

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            _viewModel = new MainViewModel();
            _tabService = TabService.Instance;
            _dataService = DataService.Instance;

            _tabService.TabAdded += OnTabAdded;
            _tabService.Tabs.CollectionChanged += OnTabsChanged;
            _dataService.DataChanged += (s, e) => RefreshProjects();

            Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= MainWindow_Activated;
            await _viewModel.InitializeAsync();

            // HomeViewModelを作成してプロジェクトリストを初期化
            _homeViewModel = new HomeViewModel();
            _homeViewModel.RefreshData();
            ProjectListView.ItemsSource = _homeViewModel.Projects;

            SyncTabs();
            UpdateToggleSwitchVisibility();
        }

        private void RefreshProjects()
        {
            _homeViewModel?.RefreshData();
        }

        private void UpdateToggleSwitchVisibility()
        {
            var isHomeTab = MainTabView.SelectedItem is TabViewItem tabViewItem && 
                           tabViewItem.Tag is TabItem tab && 
                           tab.Type == TabType.Home;

            foreach (var item in ProjectListView.Items)
            {
                var container = ProjectListView.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    var toggle = FindVisualChild<ToggleSwitch>(container);
                    if (toggle != null)
                    {
                        toggle.Visibility = isHomeTab ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        private void OnTabsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SyncTabs();
        }

        private void OnTabAdded(object? sender, TabItem tab)
        {
            var tabViewItem = MainTabView.TabItems.Cast<TabViewItem>()
                .FirstOrDefault(t => t.Tag is TabItem ti && ti.Id == tab.Id);

            if (tabViewItem != null)
            {
                MainTabView.SelectedItem = tabViewItem;
            }
        }

        private void SyncTabs()
        {
            var existingIds = MainTabView.TabItems.Cast<TabViewItem>()
                .Select(t => (t.Tag as TabItem)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToHashSet();

            var modelIds = _tabService.Tabs.Select(t => t.Id).ToHashSet();

            var toRemove = MainTabView.TabItems.Cast<TabViewItem>()
                .Where(t => t.Tag is TabItem ti && !modelIds.Contains(ti.Id))
                .ToList();
            foreach (var item in toRemove)
            {
                MainTabView.TabItems.Remove(item);
            }

            foreach (var tab in _tabService.Tabs)
            {
                if (!existingIds.Contains(tab.Id))
                {
                    var tabViewItem = CreateTabViewItem(tab);
                    MainTabView.TabItems.Add(tabViewItem);
                }
            }

            if (_viewModel.SelectedTab != null)
            {
                var selectedItem = MainTabView.TabItems.Cast<TabViewItem>()
                    .FirstOrDefault(t => t.Tag is TabItem ti && ti.Id == _viewModel.SelectedTab.Id);
                if (selectedItem != null && MainTabView.SelectedItem != selectedItem)
                {
                    MainTabView.SelectedItem = selectedItem;
                }
            }
        }

        private TabViewItem CreateTabViewItem(TabItem tab)
        {
            var tabViewItem = new TabViewItem
            {
                Tag = tab,
                IsClosable = tab.CanClose
            };

            // タブ名のバインディング設定
            var binding = new Microsoft.UI.Xaml.Data.Binding
            {
                Source = tab,
                Path = new PropertyPath("Title"),
                Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay
            };
            tabViewItem.SetBinding(TabViewItem.HeaderProperty, binding);

            // 中央ボタンでタブを閉じる（AddHandlerでhandledTooをtrueに）
            tabViewItem.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((s, e) =>
            {
                var props = e.GetCurrentPoint(tabViewItem).Properties;
                if (props.IsMiddleButtonPressed && tab.CanClose)
                {
                    e.Handled = true;
                    // Dispatcherで遅延実行して確実に処理
                    DispatcherQueue.TryEnqueue(() => _tabService.CloseTab(tab.Id));
                }
            }), true);

            var content = CreateTabContent(tab);

            var container = new Grid { Background = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250)) };
            container.ChildrenTransitions = new TransitionCollection
            {
                new EntranceThemeTransition { FromHorizontalOffset = 30, FromVerticalOffset = 0 }
            };
            container.Children.Add(content);

            tabViewItem.Content = container;
            return tabViewItem;
        }

        private FrameworkElement CreateTabContent(TabItem tab)
        {
            switch (tab.Type)
            {
                case TabType.Home:
                    var homeView = new HomeView();
                    return homeView;

                case TabType.ProjectCreate:
                    var projectCreateView = new ProjectCreateView();
                    projectCreateView.Created += (s, e) => _tabService.CloseTab(tab.Id);
                    return projectCreateView;

                case TabType.ProjectDetail:
                    var projectDetailView = new ProjectDetailView();
                    if (tab.RelatedId.HasValue)
                    {
                        projectDetailView.Load(tab.RelatedId.Value);
                    }
                    return projectDetailView;

                case TabType.TaskCreate:
                    var taskCreateView = new TaskCreateView();
                    taskCreateView.Initialize(tab.Parameter as TaskCreateParameter);
                    taskCreateView.Created += (s, task) =>
                    {
                        _tabService.CloseTab(tab.Id);
                        _tabService.OpenTaskDetailTab(task);
                    };
                    return taskCreateView;

                case TabType.TaskDetail:
                    var taskDetailView = new TaskDetailView();
                    if (tab.RelatedId.HasValue)
                    {
                        taskDetailView.Load(tab.RelatedId.Value);
                    }
                    return taskDetailView;

                default:
                    return new TextBlock { Text = "Unknown tab type" };
            }
        }

        private void TabView_AddTabButtonClick(TabView sender, object args)
        {
        }

        private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (args.Tab.Tag is TabItem tab && tab.CanClose)
            {
                _tabService.CloseTab(tab.Id);
            }
        }

        private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabView.SelectedItem is TabViewItem tabViewItem && tabViewItem.Tag is TabItem tab)
            {
                _viewModel.SelectedTab = tab;
            }
            UpdateToggleSwitchVisibility();
        }

        // プロジェクト関連のイベントハンドラ
        private void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            _tabService.OpenProjectCreateTab();
        }

        private void ProjectListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProjectViewModel vm)
            {
                _tabService.OpenProjectDetailTab(vm.Project);
            }
        }
    }
}
