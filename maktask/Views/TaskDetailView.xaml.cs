using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using maktask.Models;
using maktask.ViewModels;

namespace maktask.Views;

public sealed partial class TaskDetailView : UserControl
{
    public TaskDetailViewModel ViewModel { get; } = new TaskDetailViewModel();

    public TaskDetailView()
    {
        InitializeComponent();
        ViewModel.Subtasks.CollectionChanged += (s, e) => RefreshSubtasks();
        ViewModel.Logs.CollectionChanged += (s, e) => RefreshLogs();
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.Task) || e.PropertyName == nameof(ViewModel.Project))
            {
                UpdateProjectInfo();
                UpdateDateRange();
            }
        };
    }

    public void Load(Guid taskId)
    {
        ViewModel.Load(taskId);
        UpdateProjectInfo();
        UpdateDateRange();
        RefreshSubtasks();
        RefreshLogs();
        RefreshProgressCalendar();
    }

    private void UpdateProjectInfo()
    {
        if (ViewModel.Project != null)
        {
            ProjectColorEllipse.Fill = new SolidColorBrush(ViewModel.Project.UIColor);
            ProjectNameText.Text = ViewModel.Project.Name;
        }
    }

    private void UpdateDateRange()
    {
        if (ViewModel.Task != null)
        {
            var task = ViewModel.Task;
            string dateText;

            if (task.IsAllDay)
            {
                dateText = $"{task.StartDateTime:yyyy/MM/dd} ～ {task.EndDateTime:yyyy/MM/dd} ({task.DurationDays}日間)";
            }
            else
            {
                dateText = $"{task.StartDateTime:yyyy/MM/dd HH:mm} ～ {task.EndDateTime:yyyy/MM/dd HH:mm}";
            }

            DateRangeText.Text = dateText;
        }
    }

    private void RefreshProgressCalendar()
    {
        ProgressCalendarGrid.Children.Clear();
        ProgressCalendarGrid.ColumnDefinitions.Clear();
        ProgressCalendarGrid.RowDefinitions.Clear();

        if (ViewModel.Task == null) return;

        var startDate = ViewModel.Task.StartDateTime.Date;
        var endDate = ViewModel.Task.EndDateTime.Date;
        var totalDays = (endDate - startDate).Days + 1;

        // 最大14列まで
        var columns = Math.Min(totalDays, 14);
        var rows = (int)Math.Ceiling((double)totalDays / columns);

        for (int c = 0; c < columns; c++)
        {
            ProgressCalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        for (int r = 0; r < rows; r++)
        {
            ProgressCalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var logDates = ViewModel.Logs.Select(l => l.LogDateTime.Date).Distinct().ToHashSet();

        for (int i = 0; i < totalDays; i++)
        {
            var date = startDate.AddDays(i);
            var hasLog = logDates.Contains(date);
            var isToday = date == DateTime.Today;

            var cell = new Border
            {
                Width = 36,
                Height = 36,
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Background = hasLog 
                    ? new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)) // 緑
                    : date < DateTime.Today 
                        ? new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)) // 過去で記録なし
                        : new SolidColorBrush(Color.FromArgb(255, 250, 250, 250)), // 未来
                BorderBrush = isToday 
                    ? new SolidColorBrush(Color.FromArgb(255, 33, 150, 243)) 
                    : new SolidColorBrush(Colors.Transparent),
                BorderThickness = isToday ? new Thickness(2) : new Thickness(0),
                Child = new TextBlock
                {
                    Text = date.Day.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11,
                    Foreground = hasLog 
                        ? new SolidColorBrush(Colors.White) 
                        : new SolidColorBrush(Color.FromArgb(255, 100, 100, 100))
                }
            };

            ToolTipService.SetToolTip(cell, $"{date:yyyy/MM/dd}" + (hasLog ? " (記録あり)" : ""));

            Grid.SetColumn(cell, i % columns);
            Grid.SetRow(cell, i / columns);
            ProgressCalendarGrid.Children.Add(cell);
        }
    }

    private void RefreshSubtasks()
    {
        SubtasksPanel.Children.Clear();

        if (ViewModel.Subtasks.Count == 0)
        {
            SubtasksPanel.Children.Add(new TextBlock { Text = "サブタスクがありません", Foreground = new SolidColorBrush(Colors.Gray) });
            return;
        }

        foreach (var subtaskVm in ViewModel.Subtasks)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4), ColumnSpacing = 12 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var checkBox = new CheckBox { IsChecked = subtaskVm.IsCompleted };
            checkBox.Checked += (s, e) => subtaskVm.IsCompleted = true;
            checkBox.Unchecked += (s, e) => subtaskVm.IsCompleted = false;
            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            var nameText = new TextBlock
            {
                Text = subtaskVm.Subtask.Name,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            var dateText = new TextBlock
            {
                Text = $"{subtaskVm.Subtask.StartDateTime:M/d} - {subtaskVm.Subtask.EndDateTime:M/d}",
                Foreground = new SolidColorBrush(Colors.Gray),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
            Grid.SetColumn(dateText, 2);
            grid.Children.Add(dateText);

            SubtasksPanel.Children.Add(grid);
        }
    }

    private void RefreshLogs()
    {
        LogsPanel.Children.Clear();

        if (ViewModel.Logs.Count == 0)
        {
            LogsPanel.Children.Add(new TextBlock 
            { 
                Text = "進捗記録がありません", 
                Foreground = new SolidColorBrush(Colors.Gray),
                FontSize = 13
            });
            return;
        }

        foreach (var log in ViewModel.Logs.OrderByDescending(l => l.LogDateTime))
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 250, 250, 250)),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(14),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid { RowSpacing = 6 };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            headerPanel.Children.Add(new TextBlock { Text = log.Title, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Colors.Black) });
            headerPanel.Children.Add(new TextBlock { Text = log.LogDateTime.ToString("yyyy/MM/dd HH:mm"), Foreground = new SolidColorBrush(Colors.Gray), FontSize = 12 });
            Grid.SetColumn(headerPanel, 0);
            grid.Children.Add(headerPanel);

            var deleteBtn = new Button 
            { 
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 12 },
                Background = new SolidColorBrush(Colors.Transparent), 
                Padding = new Thickness(6),
                BorderThickness = new Thickness(0)
            };
            deleteBtn.Click += async (s, e) => await ViewModel.DeleteLogCommand.ExecuteAsync(log);
            Grid.SetColumn(deleteBtn, 1);
            grid.Children.Add(deleteBtn);

            if (!string.IsNullOrEmpty(log.Memo))
            {
                var memoText = new TextBlock 
                { 
                    Text = log.Memo, 
                    TextWrapping = TextWrapping.Wrap, 
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)), 
                    FontSize = 13 
                };
                Grid.SetRow(memoText, 1);
                Grid.SetColumnSpan(memoText, 2);
                grid.Children.Add(memoText);
            }

            border.Child = grid;
            LogsPanel.Children.Add(border);
        }
    }

    private void StartEdit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StartEditCommand.Execute(null);
    }

    private async void SaveEdit_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveEditCommand.ExecuteAsync(null);
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelEditCommand.Execute(null);
    }

    private async void AddLog_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.NewLogTitle))
        {
            var dialog = new ContentDialog
            {
                Title = "入力エラー",
                Content = "タイトルを入力してください。",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        await ViewModel.AddLogCommand.ExecuteAsync(null);
        RefreshProgressCalendar();
    }

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "タスクの削除",
            Content = "このタスクを削除してもよろしいですか？",
            PrimaryButtonText = "削除",
            CloseButtonText = "キャンセル",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteTaskCommand.ExecuteAsync(null);
        }
    }
}
