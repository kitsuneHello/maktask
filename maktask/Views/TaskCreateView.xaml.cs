using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    }

    private void AddSubtask_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AddSubtaskCommand.Execute(null);
    }

    private void RemoveSubtask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SubtaskViewModel subtask)
        {
            ViewModel.RemoveSubtaskCommand.Execute(subtask);
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
