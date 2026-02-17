using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using maktask.Models;
using maktask.ViewModels;
using maktask.Services;

namespace maktask.Views;

public sealed partial class TaskCreateView : UserControl
{
    public TaskCreateViewModel ViewModel { get; } = new TaskCreateViewModel();

    public event EventHandler<TaskItem>? Created;

    public TaskCreateView()
    {
        InitializeComponent();
        ViewModel.Created += OnCreated;
    }

    private void OnCreated(object? sender, TaskItem task)
    {
        Created?.Invoke(this, task);
    }

    public void Initialize(TaskCreateParameter? param)
    {
        ViewModel.Initialize(param);
        RefreshSubtasks();
    }

    private void AddSubtask_Click(object sender, RoutedEventArgs e)
    {
        // フォームを表示し、デフォルト値を設定
        AddSubtaskForm.Visibility = Visibility.Visible;
        NewSubtaskNameBox.Text = "";
        SubtaskIsAllDayToggle.IsOn = true;
        SubtaskIsDeadlineModeToggle.IsOn = false;
        SubtaskStartDatePicker.Date = ViewModel.StartDate;
        SubtaskEndDatePicker.Date = ViewModel.EndDate;

        // 期限モードの表示をリセット
        SubtaskStartDateGrid.Visibility = Visibility.Visible;
        SubtaskEndDateLabel.Text = "終了日";
        SubtaskEndTimeLabel.Text = "終了時刻";
        SubtaskStartTimePanel.Visibility = Visibility.Collapsed;
        SubtaskEndTimePanel.Visibility = Visibility.Collapsed;
    }

    private void CancelAddSubtask_Click(object sender, RoutedEventArgs e)
    {
        AddSubtaskForm.Visibility = Visibility.Collapsed;
    }

    private void SubtaskIsAllDayToggle_Toggled(object sender, RoutedEventArgs e)
    {
       if (SubtaskIsDeadlineModeToggle == null || SubtaskStartTimePanel == null || SubtaskEndTimePanel == null)
           return;

        var visibility = SubtaskIsAllDayToggle.IsOn ? Visibility.Collapsed : Visibility.Visible;
        SubtaskStartTimePanel.Visibility = SubtaskIsDeadlineModeToggle.IsOn ? Visibility.Collapsed : visibility;
        SubtaskEndTimePanel.Visibility = visibility;
    }

    private void SubtaskIsDeadlineModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        var isDeadlineMode = SubtaskIsDeadlineModeToggle.IsOn;

        SubtaskStartDateGrid.Visibility = isDeadlineMode ? Visibility.Collapsed : Visibility.Visible;
        SubtaskEndDateLabel.Text = isDeadlineMode ? "期限日" : "終了日";
        SubtaskEndTimeLabel.Text = isDeadlineMode ? "期限時刻" : "終了時刻";
    }

    private void ConfirmAddSubtask_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewSubtaskNameBox.Text))
        {
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
            var startTime = SubtaskStartTimePicker.Time;
            var endTime = SubtaskEndTimePicker.Time;

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

        ViewModel.Subtasks.Add(new SubtaskViewModel
        {
            Name = subtask.Name,
            StartDate = new DateTimeOffset(subtask.StartDateTime),
            EndDate = new DateTimeOffset(subtask.EndDateTime),
            IsAllDay = subtask.IsAllDay,
            IsDeadlineMode = subtask.IsDeadlineMode
        });

        AddSubtaskForm.Visibility = Visibility.Collapsed;
        RefreshSubtasks();
    }

    private void RefreshSubtasks()
    {
        SubtasksPanel.Children.Clear();

        if (ViewModel.Subtasks.Count == 0)
        {
            SubtasksPanel.Children.Add(new TextBlock 
            { 
                Text = "サブタスクがありません", 
                Foreground = new SolidColorBrush(Colors.Gray),
                FontSize = 13
            });
            return;
        }

        foreach (var subtaskVm in ViewModel.Subtasks)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4), ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = subtaskVm.Name,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameText, 0);
            grid.Children.Add(nameText);

            string dateText;
            if (subtaskVm.IsDeadlineMode)
            {
                dateText = subtaskVm.IsAllDay 
                    ? $"期限: {subtaskVm.EndDate:M/d}" 
                    : $"期限: {subtaskVm.EndDate:M/d H:mm}";
            }
            else
            {
                dateText = subtaskVm.IsAllDay 
                    ? $"{subtaskVm.StartDate:M/d} - {subtaskVm.EndDate:M/d}" 
                    : $"{subtaskVm.StartDate:M/d H:mm} - {subtaskVm.EndDate:M/d H:mm}";
            }

            var dateLabel = new TextBlock
            {
                Text = dateText,
                Foreground = new SolidColorBrush(Colors.Gray),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
            Grid.SetColumn(dateLabel, 1);
            grid.Children.Add(dateLabel);

            var deleteBtn = new Button
            {
                Content = new FontIcon { Glyph = "\uE74D", FontSize = 12 },
                Background = new SolidColorBrush(Colors.Transparent),
                Padding = new Thickness(6),
                BorderThickness = new Thickness(0)
            };
            deleteBtn.Click += (s, e) =>
            {
                ViewModel.Subtasks.Remove(subtaskVm);
                RefreshSubtasks();
            };
            Grid.SetColumn(deleteBtn, 2);
            grid.Children.Add(deleteBtn);

            SubtasksPanel.Children.Add(grid);
        }
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProject == null)
        {
            var dialog = new ContentDialog
            {
                Title = "入力エラー",
                Content = "プロジェクトを選択してください。",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(ViewModel.TaskName))
        {
            var dialog = new ContentDialog
            {
                Title = "入力エラー",
                Content = "タスク名を入力してください。",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        await ViewModel.CreateCommand.ExecuteAsync(null);
    }
}
