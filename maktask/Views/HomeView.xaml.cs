using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using maktask.Models;
using maktask.ViewModels;
using maktask.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace maktask.Views;

[ObservableObject]
public sealed partial class HomeView : UserControl
{
    public HomeViewModel ViewModel { get; } = new HomeViewModel();

    [ObservableProperty]
    private bool _isCalendarView = true;

    [ObservableProperty]
    private bool _isGanttView = false;

    [ObservableProperty]
    private bool _isListView = false;

    public HomeView()
    {
        InitializeComponent();
        Loaded += HomeView_Loaded;
        ViewModel.VisibleTasks.CollectionChanged += (s, e) => RefreshTodayTasks();
    }

    private void HomeView_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshData();
        CalendarViewControl.Initialize(ViewModel);
        GanttChartViewControl.Initialize(ViewModel);
        TaskListViewControl.Initialize(ViewModel);
        RefreshTodayTasks();
    }

    private void RefreshTodayTasks()
    {
        TodayTasksPanel.Children.Clear();
        TodayDateText.Text = DateTime.Today.ToString("yyyy年M月d日 (ddd)");

        var today = DateTime.Today;
        var todayTasks = ViewModel.VisibleTasks
            .Where(t => today >= t.StartDateTime.Date && today <= t.EndDateTime.Date)
            .ToList();

        if (!todayTasks.Any())
        {
            TodayTasksPanel.Children.Add(new TextBlock
            {
                Text = "今日のタスクはありません",
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                FontSize = 13,
                Margin = new Thickness(4)
            });
            return;
        }

        // プロジェクトごとにグループ化
        var groupedTasks = todayTasks
            .GroupBy(t => t.ProjectId)
            .Select(g => new
            {
                Project = ViewModel.Projects.FirstOrDefault(p => p.Project.Id == g.Key)?.Project,
                Tasks = g.ToList()
            })
            .Where(g => g.Project != null)
            .OrderBy(g => g.Project!.Name);

        foreach (var group in groupedTasks)
        {
            var projectPanel = new StackPanel { Spacing = 6 };

            // プロジェクトヘッダー
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 4) };
            headerPanel.Children.Add(new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(group.Project!.UIColor),
                VerticalAlignment = VerticalAlignment.Center
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = group.Project.Name,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 13,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black)
            });
            projectPanel.Children.Add(headerPanel);

            // タスク一覧
            foreach (var task in group.Tasks)
            {
                var taskBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(255, 248, 248, 248)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 2, 0, 2),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)),
                    BorderThickness = new Thickness(1)
                };

                var taskGrid = new Grid();
                taskGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                taskGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var taskName = new TextBlock
                {
                    Text = task.Name,
                    FontSize = 12,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(taskName, 0);
                taskGrid.Children.Add(taskName);

                // 期間表示（今日が開始日か終了日かを示す）
                string dateInfo = "";
                if (task.StartDateTime.Date == today && task.EndDateTime.Date == today)
                {
                    if (task.IsAllDay)
                    {
                        dateInfo = "今日のみ";
                    }
                    else
                    {
                        dateInfo = $"{task.StartDateTime:H:mm}-{task.EndDateTime:H:mm}";
                    }
                }
                else if (task.StartDateTime.Date == today)
                {
                    if (task.IsAllDay)
                    {
                        dateInfo = "開始";
                    }
                    else
                    {
                        dateInfo = $"{task.StartDateTime:H:mm}～";
                    }
                }
                else if (task.EndDateTime.Date == today)
                {
                    if (task.IsAllDay)
                    {
                        dateInfo = "終了";
                    }
                    else
                    {
                        dateInfo = $"～{task.EndDateTime:H:mm}";
                    }
                }
                else
                {
                    dateInfo = $"{task.DurationDays}日間";
                }

                var dateText = new TextBlock
                {
                    Text = dateInfo,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(dateText, 1);
                taskGrid.Children.Add(dateText);

                taskBorder.Child = taskGrid;

                // ツールチップ（終日じゃない場合は時刻も表示）
                string tooltipText;
                if (task.IsAllDay)
                {
                    tooltipText = $"{task.Name}\n{task.StartDateTime:yyyy/MM/dd} ～ {task.EndDateTime:yyyy/MM/dd}\n({task.DurationDays}日間)";
                }
                else
                {
                    tooltipText = $"{task.Name}\n{task.StartDateTime:yyyy/MM/dd HH:mm} ～ {task.EndDateTime:yyyy/MM/dd HH:mm}";
                }
                ToolTipService.SetToolTip(taskBorder, tooltipText);

                // ホバー効果とクリック処理
                taskBorder.PointerEntered += (s, e) =>
                {
                    taskBorder.Background = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));
                    ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
                };
                taskBorder.PointerExited += (s, e) =>
                {
                    taskBorder.Background = new SolidColorBrush(Color.FromArgb(255, 248, 248, 248));
                    ProtectedCursor = null;
                };
                taskBorder.Tapped += (s, e) =>
                {
                    ViewModel.OpenTaskDetailCommand.Execute(task);
                };

                projectPanel.Children.Add(taskBorder);
            }

            TodayTasksPanel.Children.Add(projectPanel);
        }
    }

    private void CreateProject_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateProjectCommand.Execute(null);
    }

    private void ProjectName_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ProjectViewModel projectVm)
        {
            ViewModel.OpenProjectDetailCommand.Execute(projectVm);
        }
    }

    private void ProjectItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
    }

    private void EditProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is ProjectViewModel projectVm)
        {
            ViewModel.OpenProjectDetailCommand.Execute(projectVm);
        }
    }

    private async void DeleteProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is ProjectViewModel projectVm)
        {
            var dialog = new ContentDialog
            {
                Title = "プロジェクトの削除",
                Content = $"プロジェクト「{projectVm.Project.Name}」を削除しますか？\n関連するタスクも全て削除されます。",
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteProjectCommand.ExecuteAsync(projectVm);
            }
        }
    }

    private void CreateTask_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateTaskCommand.Execute(null);
    }

    private void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.PreviousMonthCommand.Execute(null);
    }

    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.NextMonthCommand.Execute(null);
    }

    private void GoToToday_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GoToTodayCommand.Execute(null);
    }
}
