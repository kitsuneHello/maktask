using System;
using System.Collections.ObjectModel;
using System.Linq;
using maktask.Models;

namespace maktask.Services;

public class TabService
{
    private static TabService? _instance;
    public static TabService Instance => _instance ??= new TabService();

    public ObservableCollection<TabItem> Tabs { get; } = new();
    public event EventHandler<TabItem>? TabAdded;
    public event EventHandler<Guid>? TabCloseRequested;

    private TabService()
    {
    }

    public void Initialize()
    {
        if (!Tabs.Any(t => t.Type == TabType.Home))
        {
            var homeTab = new TabItem
            {
                Title = "ホーム",
                Type = TabType.Home,
                CanClose = false
            };
            Tabs.Add(homeTab);
        }
    }

    public TabItem OpenTab(TabType type, string title, Guid? relatedId = null, object? parameter = null)
    {
        var existing = Tabs.FirstOrDefault(t => t.Type == type && t.RelatedId == relatedId);
        if (existing != null)
        {
            TabAdded?.Invoke(this, existing);
            return existing;
        }

        var tab = new TabItem
        {
            Title = title,
            Type = type,
            RelatedId = relatedId,
            Parameter = parameter
        };
        Tabs.Add(tab);
        TabAdded?.Invoke(this, tab);
        return tab;
    }

    public void CloseTab(Guid tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab != null && tab.CanClose)
        {
            Tabs.Remove(tab);
            TabCloseRequested?.Invoke(this, tabId);
        }
    }

    public void OpenProjectCreateTab()
    {
        OpenTab(TabType.ProjectCreate, "プロジェクト作成");
    }

    public void OpenProjectDetailTab(Project project)
    {
        OpenTab(TabType.ProjectDetail, project.Name, project.Id);
    }

    public void OpenTaskCreateTab(Guid? projectId = null, DateTime? initialDate = null)
    {
        var param = new TaskCreateParameter
        {
            ProjectId = projectId,
            InitialDate = initialDate ?? DateTime.Today
        };
        var existingCreate = Tabs.FirstOrDefault(t => t.Type == TabType.TaskCreate && t.RelatedId == null);
        if (existingCreate != null)
        {
            existingCreate.Parameter = param;
            TabAdded?.Invoke(this, existingCreate);
        }
        else
        {
            OpenTab(TabType.TaskCreate, "タスク作成", null, param);
        }
    }

    public void OpenTaskDetailTab(TaskItem task)
    {
        OpenTab(TabType.TaskDetail, task.Name, task.Id);
    }
}

public class TaskCreateParameter
{
    public Guid? ProjectId { get; set; }
    public DateTime InitialDate { get; set; } = DateTime.Today;
}
