using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using maktask.Models;
using maktask.ViewModels;

namespace maktask.Views;

[ObservableObject]
public sealed partial class GanttChartView : UserControl
{
    private HomeViewModel? _viewModel;

    [ObservableProperty]
    private bool _isMonthScale = false;

    [ObservableProperty]
    private bool _isWeekScale = true;

    [ObservableProperty]
    private bool _isDayScale = false;

    private const double DayWidth = 30;
    private const double RowHeight = 32;

    public GanttChartView()
    {
        InitializeComponent();
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(IsMonthScale) or nameof(IsWeekScale) or nameof(IsDayScale))
            {
                RenderGantt();
            }
        };
    }

    public void Initialize(HomeViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.VisibleTasks.CollectionChanged += (s, e) => RenderGantt();
        RenderGantt();
    }

    partial void OnIsMonthScaleChanged(bool value)
    {
        if (value)
        {
            IsWeekScale = false;
            IsDayScale = false;
        }
    }

    partial void OnIsWeekScaleChanged(bool value)
    {
        if (value)
        {
            IsMonthScale = false;
            IsDayScale = false;
        }
    }

    partial void OnIsDayScaleChanged(bool value)
    {
        if (value)
        {
            IsMonthScale = false;
            IsWeekScale = false;
        }
    }

    private const double HeaderHeight = 30;

    private void RenderGantt()
    {
        if (_viewModel == null) return;

        GanttCanvas.Children.Clear();

        var tasks = _viewModel.VisibleTasks.ToList();
        if (!tasks.Any()) return;

        // 表示範囲を計算
        var earliestTask = tasks.Min(t => t.StartDateTime).Date;
        var latestTask = tasks.Max(t => t.EndDateTime).Date;

        // スケールに応じて開始日を調整
        DateTime minDate;
        DateTime maxDate;

        if (IsMonthScale)
        {
            // 月の最初の日から開始
            minDate = new DateTime(earliestTask.Year, earliestTask.Month, 1);
            maxDate = new DateTime(latestTask.Year, latestTask.Month, 1).AddMonths(2);
        }
        else if (IsWeekScale)
        {
            // 週の開始日に揃える
            var dayOfWeek = (int)earliestTask.DayOfWeek;
            minDate = earliestTask.AddDays(-dayOfWeek);
            maxDate = latestTask.AddDays(14);
        }
        else
        {
            minDate = earliestTask.AddDays(-2);
            maxDate = latestTask.AddDays(5);
        }

        var contentTop = HeaderHeight;

        // 固定幅を設定
        var fixedCellWidth = IsMonthScale ? 150.0 : IsWeekScale ? 80.0 : 30.0;

        // セル情報を収集
        var cellInfos = new List<(DateTime Start, DateTime End, double X)>();
        var tempDate = minDate;
        double xPos = 0;

        while (tempDate <= maxDate)
        {
            DateTime cellEnd;
            if (IsMonthScale)
            {
                cellEnd = new DateTime(tempDate.Year, tempDate.Month, 1).AddMonths(1);
            }
            else if (IsWeekScale)
            {
                cellEnd = tempDate.AddDays(7);
            }
            else
            {
                cellEnd = tempDate.AddDays(1);
            }

            cellInfos.Add((tempDate, cellEnd, xPos));
            xPos += fixedCellWidth;
            tempDate = cellEnd;
        }

        calculatedWidth = xPos;
        GanttCanvas.Width = calculatedWidth;
        GanttCanvas.Height = tasks.Count * RowHeight + HeaderHeight + 10;

        // ヘッダー背景
        var headerBg = new Border
        {
            Width = calculatedWidth,
            Height = HeaderHeight,
            Background = new SolidColorBrush(Color.FromArgb(255, 248, 248, 248))
        };
        Canvas.SetLeft(headerBg, 0);
        Canvas.SetTop(headerBg, 0);
        GanttCanvas.Children.Add(headerBg);

        // ヘッダーセルを描画
        foreach (var cell in cellInfos)
        {
            string label;
            if (IsMonthScale)
            {
                label = cell.Start.ToString("yyyy/M");
            }
            else if (IsWeekScale)
            {
                label = cell.Start.ToString("M/d");
            }
            else
            {
                label = cell.Start.Day.ToString();
            }

            var headerCell = new Border
            {
                Width = fixedCellWidth,
                Height = HeaderHeight,
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 220, 220, 220)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock
                {
                    Text = label,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80))
                }
            };
            Canvas.SetLeft(headerCell, cell.X);
            Canvas.SetTop(headerCell, 0);
            GanttCanvas.Children.Add(headerCell);

            // 縦の点線グリッド
            var verticalLine = new Line
            {
                X1 = cell.X + fixedCellWidth,
                X2 = cell.X + fixedCellWidth,
                Y1 = HeaderHeight,
                Y2 = GanttCanvas.Height,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 230, 230, 230)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };
            GanttCanvas.Children.Add(verticalLine);
        }

        // 横の点線グリッドを描画
        for (int i = 0; i <= tasks.Count; i++)
        {
            var horizontalLine = new Line
            {
                X1 = 0,
                X2 = calculatedWidth,
                Y1 = contentTop + i * RowHeight,
                Y2 = contentTop + i * RowHeight,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235)),
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };
            GanttCanvas.Children.Add(horizontalLine);
        }

        // タスクバーを描画
        for (int i = 0; i < tasks.Count; i++)
        {
            var task = tasks[i];
            var project = _viewModel.GetProjectForTask(task);

            // タスクの開始位置と幅を計算（終日じゃない場合は時刻も考慮）
            double startOffset, endOffset;

            if (task.IsAllDay)
            {
                startOffset = GetPositionForDate(task.StartDateTime.Date, cellInfos, fixedCellWidth);
                endOffset = GetPositionForDate(task.EndDateTime.Date.AddDays(1), cellInfos, fixedCellWidth);
            }
            else
            {
                startOffset = GetPositionForDateTime(task.StartDateTime, cellInfos, fixedCellWidth);
                endOffset = GetPositionForDateTime(task.EndDateTime, cellInfos, fixedCellWidth);
            }

            var barWidth = Math.Max(endOffset - startOffset, 20);

            var taskBar = new Border
            {
                Width = barWidth,
                Height = RowHeight - 10,
                Background = new SolidColorBrush(project?.UIColor ?? Colors.Gray),
                CornerRadius = new CornerRadius(4),
                Child = new TextBlock
                {
                    Text = task.Name,
                    Foreground = new SolidColorBrush(Colors.White),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 8, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = 11
                }
            };

            // ホバー効果
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
            ToolTipService.SetToolTip(taskBar, tooltipText);

            taskBar.Tapped += (s, e) => _viewModel.OpenTaskDetailCommand.Execute(task);
            taskBar.RightTapped += (s, e) => ShowTaskContextMenu(task, taskBar);

            Canvas.SetLeft(taskBar, startOffset);
            Canvas.SetTop(taskBar, contentTop + i * RowHeight + 5);
            GanttCanvas.Children.Add(taskBar);
        }
    }

    private double GetPositionForDateTime(DateTime targetDateTime, List<(DateTime Start, DateTime End, double X)> cellInfos, double cellWidth)
    {
        // 日付部分の位置を取得
        var datePosition = GetPositionForDate(targetDateTime.Date, cellInfos, cellWidth);

        // 時刻による追加オフセットを計算（日スケールの場合のみ意味がある）
        if (!IsMonthScale && !IsWeekScale)
        {
            // 日スケールの場合、時刻に応じてセル内の位置を調整
            var timeRatio = targetDateTime.TimeOfDay.TotalHours / 24.0;
            return datePosition + cellWidth * timeRatio;
        }

        return datePosition;
    }

    private double GetPositionForDate(DateTime targetDate, List<(DateTime Start, DateTime End, double X)> cellInfos, double cellWidth)
    {
        foreach (var cell in cellInfos)
        {
            if (targetDate < cell.Start)
            {
                return cell.X;
            }
            else if (targetDate < cell.End)
            {
                // このセル内にある
                var daysInCell = (cell.End - cell.Start).Days;
                var daysFromStart = (targetDate - cell.Start).Days;
                var ratio = (double)daysFromStart / daysInCell;
                return cell.X + cellWidth * ratio;
            }
        }
        // 最後のセルより後
        return calculatedWidth;
    }

    private double calculatedWidth;

    private void ShowTaskContextMenu(TaskItem task, FrameworkElement target)
    {
        if (_viewModel == null) return;

        var menu = new MenuFlyout();
        var deleteItem = new MenuFlyoutItem { Text = "削除", Icon = new SymbolIcon(Symbol.Delete) };
        deleteItem.Click += async (s, e) => await _viewModel.DeleteTaskCommand.ExecuteAsync(task);
        menu.Items.Add(deleteItem);
        menu.ShowAt(target);
    }
}
