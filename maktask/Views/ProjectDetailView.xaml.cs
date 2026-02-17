using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using maktask.Models;
using maktask.ViewModels;

namespace maktask.Views;

[ObservableObject]
public sealed partial class ProjectDetailView : UserControl
{
    public ProjectDetailViewModel ViewModel { get; } = new ProjectDetailViewModel();

    public bool HasNoTasks => ViewModel.Tasks.Count == 0;

    public ProjectDetailView()
    {
        InitializeComponent();
        ViewModel.Tasks.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasNoTasks));
    }

    public void Load(Guid projectId)
    {
        ViewModel.Load(projectId);
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

    private void ColorBorder_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is string color)
        {
            ViewModel.ThemeColor = color;
        }
    }

    private void ColorBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 0.7;
            border.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
            border.BorderThickness = new Thickness(3);
            ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Hand);
        }
    }

    private void ColorBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Opacity = 1.0;
            border.BorderBrush = null;
            border.BorderThickness = new Thickness(0);
            ProtectedCursor = null;
        }
    }

    private void CreateTask_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CreateTaskCommand.Execute(null);
    }

    private void TaskItem_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TaskItem task)
        {
            if (e.GetCurrentPoint(fe).Properties.IsLeftButtonPressed)
            {
                ViewModel.OpenTaskDetailCommand.Execute(task);
            }
        }
    }

    private void TaskItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
    }

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.DataContext is TaskItem task)
        {
            var dialog = new ContentDialog
            {
                Title = "タスクの削除",
                Content = $"タスク「{task.Name}」を削除しますか？",
                PrimaryButtonText = "削除",
                CloseButtonText = "キャンセル",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteTaskCommand.ExecuteAsync(task);
            }
        }
    }
}
