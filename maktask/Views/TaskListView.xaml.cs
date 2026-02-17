using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using maktask.Models;
using maktask.ViewModels;

namespace maktask.Views;

public sealed partial class TaskListView : UserControl
{
    private HomeViewModel? _viewModel;
    public ObservableCollection<ProjectTaskGroup> Groups { get; } = new();

    public TaskListView()
    {
        InitializeComponent();
    }

    public void Initialize(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.VisibleTasks.CollectionChanged += (s, e) => RefreshGroups();
        _viewModel.Projects.CollectionChanged += (s, e) => RefreshGroups();
        RefreshGroups();
    }

    private void RefreshGroups()
    {
        if (_viewModel == null) return;

        Groups.Clear();

        var visibleProjects = _viewModel.Projects.Where(p => p.IsVisible).Select(p => p.Project);

        foreach (var project in visibleProjects)
        {
            var tasks = _viewModel.VisibleTasks
                .Where(t => t.ProjectId == project.Id)
                .OrderBy(t => t.StartDateTime)
                .ToList();

            if (tasks.Any())
            {
                var group = new ProjectTaskGroup(project, tasks);
                Groups.Add(group);
            }
        }

        ProjectGroups.ItemsSource = Groups;
    }

    private void TaskBorder_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TaskItem task)
        {
            _viewModel?.OpenTaskDetailCommand.Execute(task);
        }
    }

    private void TaskBorder_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TaskItem task && _viewModel != null)
        {
            e.Handled = true;
            var menu = new MenuFlyout();

            var deleteItem = new MenuFlyoutItem 
            { 
                Text = "削除", 
                Icon = new SymbolIcon(Symbol.Delete),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red)
            };
            deleteItem.Click += async (s, args) => await _viewModel.DeleteTaskCommand.ExecuteAsync(task);
            menu.Items.Add(deleteItem);

            menu.ShowAt(border, e.GetPosition(border));
        }
    }

    private void TaskBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        }
    }

    private void TaskBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (SolidColorBrush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            ProtectedCursor = null;
        }
    }

    private void TaskBorder_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is TaskItem task)
        {
            // 日付テキストを設定
            var grid = border.Child as Grid;
            var dateText = grid?.Children.OfType<TextBlock>().LastOrDefault();
            if (dateText != null)
            {
                if (task.IsAllDay)
                {
                    dateText.Text = $"{task.StartDateTime:M/d} - {task.EndDateTime:M/d}";
                }
                else
                {
                    dateText.Text = $"{task.StartDateTime:M/d H:mm} - {task.EndDateTime:M/d H:mm}";
                }
            }

            // ツールチップを設定
            string tooltipText;
            if (task.IsAllDay)
            {
                tooltipText = $"{task.Name}\n{task.StartDateTime:yyyy/MM/dd} ～ {task.EndDateTime:yyyy/MM/dd}\n({task.DurationDays}日間)";
            }
            else
            {
                tooltipText = $"{task.Name}\n{task.StartDateTime:yyyy/MM/dd HH:mm} ～ {task.EndDateTime:yyyy/MM/dd HH:mm}";
            }
            ToolTipService.SetToolTip(border, tooltipText);
        }
    }
}

public class ProjectTaskGroup
{
    public Project Project { get; }
    public List<TaskItem> Tasks { get; }
    public string TaskCountText => $"({Tasks.Count}件)";

    public ProjectTaskGroup(Project project, List<TaskItem> tasks)
    {
        Project = project;
        Tasks = tasks;
    }
}
