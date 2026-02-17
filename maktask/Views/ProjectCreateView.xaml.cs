using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using maktask.ViewModels;
using maktask.Services;

namespace maktask.Views;

public sealed partial class ProjectCreateView : UserControl
{
    public ProjectCreateViewModel ViewModel { get; } = new ProjectCreateViewModel();

    public event EventHandler? Created;

    public ProjectCreateView()
    {
        InitializeComponent();
        ViewModel.Created += OnCreated;
    }

    private void OnCreated(object? sender, EventArgs e)
    {
        Created?.Invoke(this, EventArgs.Empty);
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

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.ProjectName))
        {
            var dialog = new ContentDialog
            {
                Title = "入力エラー",
                Content = "プロジェクト名を入力してください。",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        await ViewModel.CreateCommand.ExecuteAsync(null);
    }
}
