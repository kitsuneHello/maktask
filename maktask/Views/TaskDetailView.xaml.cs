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
        var logDates = ViewModel.Logs.Select(l => l.LogDateTime.Date).Distinct().ToHashSet();

        // 月ごとにグループ化
        var months = new List<(int Year, int Month)>();
        var current = new DateTime(startDate.Year, startDate.Month, 1);
        var lastMonth = new DateTime(endDate.Year, endDate.Month, 1);

        while (current <= lastMonth)
        {
            months.Add((current.Year, current.Month));
            current = current.AddMonths(1);
        }

        // 7列（日〜土）固定
        for (int c = 0; c < 7; c++)
        {
            ProgressCalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        int rowIndex = 0;

        foreach (var (year, month) in months)
        {
            var firstDayOfMonth = new DateTime(year, month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // タスク期間内の日付のみ表示
            var displayStart = firstDayOfMonth < startDate ? startDate : firstDayOfMonth;
            var displayEnd = lastDayOfMonth > endDate ? endDate : lastDayOfMonth;

            // 月ヘッダー行
            ProgressCalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var monthHeader = new TextBlock
            {
                Text = $"{year}年{month}月",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)),
                Margin = new Thickness(0, rowIndex > 0 ? 12 : 0, 0, 4)
            };
            Grid.SetRow(monthHeader, rowIndex);
            Grid.SetColumnSpan(monthHeader, 7);
            ProgressCalendarGrid.Children.Add(monthHeader);
            rowIndex++;

            // 曜日ヘッダー行
            ProgressCalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var dayNames = new[] { "日", "月", "火", "水", "木", "金", "土" };
            for (int d = 0; d < 7; d++)
            {
                var dayHeader = new TextBlock
                {
                    Text = dayNames[d],
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = d == 0 ? new SolidColorBrush(Color.FromArgb(255, 220, 80, 80)) :
                                d == 6 ? new SolidColorBrush(Color.FromArgb(255, 80, 120, 200)) :
                                new SolidColorBrush(Color.FromArgb(255, 120, 120, 120))
                };
                Grid.SetRow(dayHeader, rowIndex);
                Grid.SetColumn(dayHeader, d);
                ProgressCalendarGrid.Children.Add(dayHeader);
            }
            rowIndex++;

            // カレンダー開始位置（月初の曜日）
            var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var totalCells = firstDayOfWeek + daysInMonth;
            var weeksNeeded = (int)Math.Ceiling(totalCells / 7.0);

            for (int w = 0; w < weeksNeeded; w++)
            {
                ProgressCalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                for (int d = 0; d < 7; d++)
                {
                    var cellIndex = w * 7 + d;
                    var dayNum = cellIndex - firstDayOfWeek + 1;

                    if (dayNum < 1 || dayNum > daysInMonth)
                    {
                        // 空セル
                        continue;
                    }

                    var date = new DateTime(year, month, dayNum);
                    var isInRange = date >= startDate && date <= endDate;
                    var hasLog = logDates.Contains(date);
                    var isToday = date == DateTime.Today;

                    var cell = new Border
                    {
                        Width = 28,
                        Height = 28,
                        Margin = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Opacity = isInRange ? 1.0 : 0.3,
                        Background = !isInRange
                            ? new SolidColorBrush(Color.FromArgb(255, 245, 245, 245))
                            : hasLog 
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
                            Text = dayNum.ToString(),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 10,
                            Foreground = hasLog && isInRange
                                ? new SolidColorBrush(Colors.White) 
                                : d == 0 ? new SolidColorBrush(Color.FromArgb(255, 220, 80, 80)) :
                                  d == 6 ? new SolidColorBrush(Color.FromArgb(255, 80, 120, 200)) :
                                  new SolidColorBrush(Color.FromArgb(255, 80, 80, 80))
                        }
                    };

                    if (isInRange)
                    {
                        ToolTipService.SetToolTip(cell, $"{date:yyyy/MM/dd}" + (hasLog ? " (記録あり)" : ""));
                    }

                    Grid.SetRow(cell, rowIndex);
                    Grid.SetColumn(cell, d);
                    ProgressCalendarGrid.Children.Add(cell);
                }
                rowIndex++;
            }
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
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4), ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var checkBox = new CheckBox { IsChecked = subtaskVm.IsCompleted };
            checkBox.Checked += (s, e) => subtaskVm.IsCompleted = true;
            checkBox.Unchecked += (s, e) => subtaskVm.IsCompleted = false;
            Grid.SetColumn(checkBox, 0);
            grid.Children.Add(checkBox);

            var nameText = new TextBlock
            {
                Text = subtaskVm.Subtask.Name,
                VerticalAlignment = VerticalAlignment.Center,
                TextDecorations = subtaskVm.IsCompleted ? Windows.UI.Text.TextDecorations.Strikethrough : Windows.UI.Text.TextDecorations.None
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

            var deleteBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 12 },
                Background = new SolidColorBrush(Colors.Transparent),
                Padding = new Thickness(6),
                BorderThickness = new Thickness(0)
            };
            var subtask = subtaskVm.Subtask;
            deleteBtn.Click += async (s, e) =>
            {
                await ViewModel.DeleteSubtaskCommand.ExecuteAsync(subtask);
                RefreshSubtasks();
            };
            Grid.SetColumn(deleteBtn, 3);
            grid.Children.Add(deleteBtn);

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

    private void AddSubtask_Click(object sender, RoutedEventArgs e)
    {
        // フォームを表示し、デフォルト値を設定
        AddSubtaskForm.Visibility = Visibility.Visible;
        NewSubtaskNameBox.Text = "";
        SubtaskIsAllDayToggle.IsOn = true;
        SubtaskIsDeadlineModeToggle.IsOn = false;

        if (ViewModel.Task != null)
        {
            SubtaskStartDatePicker.Date = new DateTimeOffset(ViewModel.Task.StartDateTime.Date);
            SubtaskEndDatePicker.Date = new DateTimeOffset(ViewModel.Task.EndDateTime.Date);
        }
        else
        {
            SubtaskStartDatePicker.Date = DateTimeOffset.Now;
            SubtaskEndDatePicker.Date = DateTimeOffset.Now;
        }
    }

    private void CancelAddSubtask_Click(object sender, RoutedEventArgs e)
    {
        AddSubtaskForm.Visibility = Visibility.Collapsed;
    }

    private void SubtaskIsAllDayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        // 終日トグルの状態に応じて時刻選択の表示/非表示を切り替え
        var startTimePanel = FindName("SubtaskStartTimePanel") as StackPanel;
        var endTimePanel = FindName("SubtaskEndTimePanel") as StackPanel;

        var visibility = SubtaskIsAllDayToggle.IsOn ? Visibility.Collapsed : Visibility.Visible;

        if (startTimePanel != null) startTimePanel.Visibility = visibility;
        if (endTimePanel != null) endTimePanel.Visibility = visibility;
    }

    private void SubtaskIsDeadlineModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        // 期限モードの状態に応じて開始日を非表示にし、ラベルを変更
        var startDateGrid = FindName("SubtaskStartDateGrid") as Grid;
        var endDateLabel = FindName("SubtaskEndDateLabel") as TextBlock;
        var endTimeLabel = FindName("SubtaskEndTimeLabel") as TextBlock;

        var isDeadlineMode = SubtaskIsDeadlineModeToggle.IsOn;

        if (startDateGrid != null)
        {
            startDateGrid.Visibility = isDeadlineMode ? Visibility.Collapsed : Visibility.Visible;
        }

        if (endDateLabel != null)
        {
            endDateLabel.Text = isDeadlineMode ? "期限日" : "終了日";
        }

        if (endTimeLabel != null)
        {
            endTimeLabel.Text = isDeadlineMode ? "期限時刻" : "終了時刻";
        }
    }

    private async void ConfirmAddSubtask_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewSubtaskNameBox.Text))
        {
            var dialog = new ContentDialog
            {
                Title = "入力エラー",
                Content = "サブタスク名を入力してください。",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        DateTime startDateTime, endDateTime;

        var startDate = SubtaskStartDatePicker.Date?.DateTime.Date ?? DateTime.Today;
        var endDate = SubtaskEndDatePicker.Date?.DateTime.Date ?? DateTime.Today;

        if (SubtaskIsAllDayToggle.IsOn)
        {
            startDateTime = startDate;
            endDateTime = endDate;
        }
        else
        {
            var startTimePicker = FindName("SubtaskStartTimePicker") as TimePicker;
            var endTimePicker = FindName("SubtaskEndTimePicker") as TimePicker;

            var startTime = startTimePicker?.Time ?? new TimeSpan(9, 0, 0);
            var endTime = endTimePicker?.Time ?? new TimeSpan(17, 0, 0);

            startDateTime = startDate.Add(startTime);
            endDateTime = endDate.Add(endTime);
        }

        var subtask = new Subtask
        {
            Name = NewSubtaskNameBox.Text.Trim(),
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            IsAllDay = SubtaskIsAllDayToggle.IsOn,
            IsDeadlineMode = SubtaskIsDeadlineModeToggle.IsOn
        };

        await ViewModel.AddSubtaskCommand.ExecuteAsync(subtask);
        AddSubtaskForm.Visibility = Visibility.Collapsed;
        RefreshSubtasks();
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
