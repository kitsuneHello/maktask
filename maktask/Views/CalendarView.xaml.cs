using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using maktask.Models;
using maktask.ViewModels;
using maktask.Services;

namespace maktask.Views;

public sealed partial class CalendarView : UserControl
{
    private HomeViewModel? _viewModel;
    private const int MaxTasksPerDay = 3;

    public CalendarView()
    {
        InitializeComponent();
    }

    public void Initialize(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(HomeViewModel.CurrentMonth))
            {
                RenderCalendar();
            }
        };
        _viewModel.VisibleTasks.CollectionChanged += (s, e) => RenderCalendar();
        RenderCalendar();
    }

    private void RenderCalendar()
    {
        if (_viewModel == null) return;

        DaysGrid.Children.Clear();
        DaysGrid.RowDefinitions.Clear();

        var currentMonth = _viewModel.CurrentMonth;
        var firstDay = new DateTime(currentMonth.Year, currentMonth.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var startDay = firstDay.AddDays(-(int)firstDay.DayOfWeek);
        var endDay = lastDay.AddDays(6 - (int)lastDay.DayOfWeek);

        var totalWeeks = (int)Math.Ceiling((endDay - startDay).TotalDays / 7.0);

        for (int i = 0; i < totalWeeks; i++)
        {
            DaysGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (int i = 0; i < 7; i++)
        {
            if (DaysGrid.ColumnDefinitions.Count <= i)
            {
                DaysGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
        }

        var tasks = _viewModel.VisibleTasks.OrderByDescending(t => t.DurationDays).ThenBy(t => t.StartDateTime).ToList();
        var taskPositions = new Dictionary<Guid, int>();
        var dayTaskSlots = new Dictionary<DateTime, List<int>>();

        foreach (var task in tasks)
        {
            var taskStart = task.StartDateTime.Date < startDay ? startDay : task.StartDateTime.Date;
            var taskEnd = task.EndDateTime.Date > endDay ? endDay : task.EndDateTime.Date;

            int slot = 0;
            for (var day = taskStart; day <= taskEnd; day = day.AddDays(1))
            {
                if (!dayTaskSlots.ContainsKey(day))
                    dayTaskSlots[day] = new List<int>();

                while (dayTaskSlots[day].Contains(slot))
                    slot++;
            }

            taskPositions[task.Id] = slot;

            for (var day = taskStart; day <= taskEnd; day = day.AddDays(1))
            {
                if (!dayTaskSlots.ContainsKey(day))
                    dayTaskSlots[day] = new List<int>();
                dayTaskSlots[day].Add(slot);
            }
        }

        var currentDate = startDay;
        for (int week = 0; week < totalWeeks; week++)
        {
            for (int dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++)
            {
                var date = currentDate;
                var isCurrentMonth = date.Month == currentMonth.Month;

                var dayCell = new Grid
                {
                    BorderBrush = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Opacity = isCurrentMonth ? 1.0 : 0.4,
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Colors.Transparent) // 全体で右クリック可能に
                };

                // ホバー効果を追加
                dayCell.PointerEntered += (s, e) => 
                {
                    if (s is Grid g) g.Background = new SolidColorBrush(Color.FromArgb(20, 0, 0, 0));
                };
                dayCell.PointerExited += (s, e) => 
                {
                    if (s is Grid g) g.Background = new SolidColorBrush(Colors.Transparent);
                };

                dayCell.RightTapped += (s, e) => 
                {
                    e.Handled = true;
                    ShowDayContextMenu(date, s as FrameworkElement);
                };

                var dayStack = new StackPanel();

                // 日付表示部分（高さを固定）
                var dateHeader = new Grid
                {
                    Height = 24,
                    Margin = new Thickness(4, 2, 4, 0),
                    IsHitTestVisible = false
                };

                if (date == DateTime.Today)
                {
                    var todayBorder = new Border
                    {
                        Background = new SolidColorBrush(Colors.DodgerBlue),
                        CornerRadius = new CornerRadius(10),
                        Width = 20,
                        Height = 20,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = date.Day.ToString(),
                            Foreground = new SolidColorBrush(Colors.White),
                            FontSize = 12,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                        }
                    };
                    dateHeader.Children.Add(todayBorder);
                }
                else
                {
                    var dayNumber = new TextBlock
                    {
                        Text = date.Day.ToString(),
                        FontSize = 13,
                        FontWeight = Microsoft.UI.Text.FontWeights.Normal,
                        Foreground = dayOfWeek == 0 ? new SolidColorBrush(Color.FromArgb(255, 220, 80, 80)) :
                                    dayOfWeek == 6 ? new SolidColorBrush(Color.FromArgb(255, 80, 120, 200)) :
                                    new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(2, 0, 0, 0)
                    };
                    dateHeader.Children.Add(dayNumber);
                }

                dayStack.Children.Add(dateHeader);

                var dayTasks = tasks.Where(t => date >= t.StartDateTime.Date && date <= t.EndDateTime.Date)
                                   .OrderBy(t => taskPositions.GetValueOrDefault(t.Id, 0))
                                   .ToList();

                // スロット位置に基づいてタスクを配置（空きスロットにはプレースホルダーを入れる）
                int currentSlot = 0;
                int slotsUsed = 0;      // 使用したスロット数（プレースホルダー含む）
                int tasksDisplayed = 0; // 実際に表示したタスク数

                foreach (var task in dayTasks)
                {
                    var taskSlot = taskPositions.GetValueOrDefault(task.Id, 0);

                    // 空きスロットにプレースホルダーを追加
                    while (currentSlot < taskSlot && slotsUsed < MaxTasksPerDay)
                    {
                        var placeholder = new Border
                        {
                            Height = 20,
                            Margin = new Thickness(4, 1, 4, 1),
                            Background = new SolidColorBrush(Colors.Transparent)
                        };
                        dayStack.Children.Add(placeholder);
                        currentSlot++;
                        slotsUsed++;
                    }

                    if (slotsUsed >= MaxTasksPerDay)
                    {
                        // 残りのタスク数を表示
                        var remainingTasks = dayTasks.Count - tasksDisplayed;
                        if (remainingTasks > 0)
                        {
                            var moreText = new TextBlock
                            {
                                Text = $"... 他{remainingTasks}件",
                                FontSize = 10,
                                Foreground = new SolidColorBrush(Color.FromArgb(255, 120, 120, 120)),
                                Margin = new Thickness(6, 1, 2, 1)
                            };
                            dayStack.Children.Add(moreText);
                        }
                        break;
                    }

                    var project = _viewModel.GetProjectForTask(task);
                    var isStart = date == task.StartDateTime.Date;
                    var isEnd = date == task.EndDateTime.Date;
                    var taskColor = project?.UIColor ?? Colors.Gray;

                    var taskBar = new Border
                    {
                        Background = new SolidColorBrush(taskColor),
                        CornerRadius = new CornerRadius(
                            isStart ? 3 : 0, isEnd ? 3 : 0,
                            isEnd ? 3 : 0, isStart ? 3 : 0),
                        Margin = new Thickness(isStart ? 4 : -1, 1, isEnd ? 4 : -1, 1),
                        Padding = new Thickness(isStart ? 6 : 2, 2, isEnd ? 6 : 2, 2),
                        Height = 20
                    };

                    // ホバー効果を追加
                    taskBar.PointerEntered += (s, e) =>
                    {
                        if (s is Border b) b.Opacity = 0.8;
                        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
                    };
                    taskBar.PointerExited += (s, e) =>
                    {
                        if (s is Border b) b.Opacity = 1.0;
                        ProtectedCursor = null;
                    };

                    // ツールチップを追加（終日じゃない場合は時刻も表示）
                    string tooltipText;
                    if (task.IsAllDay)
                    {
                        tooltipText = $"{task.Name}\n{task.StartDateTime:yyyy/MM/dd} ～ {task.EndDateTime:yyyy/MM/dd}\n({task.DurationDays}日間)";
                    }
                    else
                    {
                        tooltipText = $"{task.Name}\n{task.StartDateTime:yyyy/MM/dd HH:mm} ～ {task.EndDateTime:yyyy/MM/dd HH:mm}";
                    }
                    ToolTipService.SetToolTip(taskBar, tooltipText);

                    if (isStart)
                    {
                        taskBar.Child = new TextBlock
                        {
                            Text = task.Name,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Colors.White),
                            TextTrimming = TextTrimming.CharacterEllipsis
                        };
                    }

                    taskBar.Tapped += (s, e) =>
                    {
                        e.Handled = true;
                        _viewModel.OpenTaskDetailCommand.Execute(task);
                    };

                    dayStack.Children.Add(taskBar);
                    currentSlot = taskSlot + 1;
                    slotsUsed++;
                    tasksDisplayed++;
                }

                dayCell.Children.Add(dayStack);
                Grid.SetRow(dayCell, week);
                Grid.SetColumn(dayCell, dayOfWeek);
                DaysGrid.Children.Add(dayCell);

                currentDate = currentDate.AddDays(1);
            }
        }
    }

    private void ShowDayContextMenu(DateTime date, FrameworkElement? target)
    {
        if (_viewModel == null || target == null) return;

        var dayTasks = _viewModel.VisibleTasks
            .Where(t => date >= t.StartDateTime.Date && date <= t.EndDateTime.Date)
            .ToList();

        var menu = new MenuFlyout();

        if (dayTasks.Any())
        {
            // プロジェクトごとにグループ化
            var groupedTasks = dayTasks
                .GroupBy(t => t.ProjectId)
                .Select(g => new
                {
                    Project = _viewModel.Projects.FirstOrDefault(p => p.Project.Id == g.Key)?.Project,
                    Tasks = g.ToList()
                })
                .Where(g => g.Project != null)
                .OrderBy(g => g.Project!.Name);

            foreach (var group in groupedTasks)
            {
                var projectItem = new MenuFlyoutSubItem 
                { 
                    Text = $"{group.Project!.Name} ({group.Tasks.Count})",
                    Icon = new FontIcon 
                    { 
                        Glyph = "\uE8D7",
                        Foreground = new SolidColorBrush(group.Project.UIColor)
                    }
                };

                foreach (var task in group.Tasks.OrderBy(t => t.StartDateTime))
                {
                    var taskItem = new MenuFlyoutItem { Text = task.Name };
                    taskItem.Click += (s, e) => _viewModel.OpenTaskDetailCommand.Execute(task);
                    projectItem.Items.Add(taskItem);
                }

                menu.Items.Add(projectItem);
            }

            menu.Items.Add(new MenuFlyoutSeparator());
        }

        var createItem = new MenuFlyoutItem 
        { 
            Text = "この日にタスクを作成",
            Icon = new SymbolIcon(Symbol.Add)
        };
        createItem.Click += (s, e) => _viewModel.CreateTaskForDateCommand.Execute(date);
        menu.Items.Add(createItem);

        menu.ShowAt(target);
    }
}
