using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace maktask.Models;

public enum TabType
{
    Home,
    ProjectCreate,
    ProjectDetail,
    TaskCreate,
    TaskDetail
}

public class TabItem : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public TabType Type { get; set; }
    public Guid? RelatedId { get; set; }
    public object? Parameter { get; set; }
    public bool CanClose { get; set; } = true;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
