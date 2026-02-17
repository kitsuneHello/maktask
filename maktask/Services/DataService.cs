using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using maktask.Models;

namespace maktask.Services;

public class DataService
{
    private static DataService? _instance;
    public static DataService Instance => _instance ??= new DataService();

    private readonly string _dataFolder;
    private readonly string _projectsFile;
    private readonly string _tasksFile;
    private readonly string _logsFile;

    private List<Project> _projects = new();
    private List<TaskItem> _tasks = new();
    private List<ProgressLog> _logs = new();

    public event EventHandler? DataChanged;

    private DataService()
    {
        _dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MakTask");
        Directory.CreateDirectory(_dataFolder);
        _projectsFile = Path.Combine(_dataFolder, "projects.json");
        _tasksFile = Path.Combine(_dataFolder, "tasks.json");
        _logsFile = Path.Combine(_dataFolder, "logs.json");
    }

    public async Task LoadDataAsync()
    {
        _projects = await LoadAsync<List<Project>>(_projectsFile) ?? new();
        _tasks = await LoadAsync<List<TaskItem>>(_tasksFile) ?? new();
        _logs = await LoadAsync<List<ProgressLog>>(_logsFile) ?? new();
    }

    private async Task<T?> LoadAsync<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json);
    }

    private async Task SaveAsync<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public IReadOnlyList<Project> GetProjects() => _projects.AsReadOnly();

    public Project? GetProject(Guid id) => _projects.FirstOrDefault(p => p.Id == id);

    public async Task AddProjectAsync(Project project)
    {
        _projects.Add(project);
        await SaveAsync(_projectsFile, _projects);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdateProjectAsync(Project project)
    {
        var index = _projects.FindIndex(p => p.Id == project.Id);
        if (index >= 0)
        {
            _projects[index] = project;
            await SaveAsync(_projectsFile, _projects);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task DeleteProjectAsync(Guid id)
    {
        _projects.RemoveAll(p => p.Id == id);
        _tasks.RemoveAll(t => t.ProjectId == id);
        await SaveAsync(_projectsFile, _projects);
        await SaveAsync(_tasksFile, _tasks);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<TaskItem> GetTasks() => _tasks.AsReadOnly();

    public IEnumerable<TaskItem> GetTasksForProjects(IEnumerable<Guid> projectIds)
    {
        var ids = projectIds.ToHashSet();
        return _tasks.Where(t => ids.Contains(t.ProjectId));
    }

    public IEnumerable<TaskItem> GetTasksForProject(Guid projectId)
    {
        return _tasks.Where(t => t.ProjectId == projectId);
    }

    public TaskItem? GetTask(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

    public async Task AddTaskAsync(TaskItem task)
    {
        _tasks.Add(task);
        await SaveAsync(_tasksFile, _tasks);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        var index = _tasks.FindIndex(t => t.Id == task.Id);
        if (index >= 0)
        {
            _tasks[index] = task;
            await SaveAsync(_tasksFile, _tasks);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        _tasks.RemoveAll(t => t.Id == id);
        _logs.RemoveAll(l => l.TaskId == id);
        await SaveAsync(_tasksFile, _tasks);
        await SaveAsync(_logsFile, _logs);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<ProgressLog> GetLogsForTask(Guid taskId)
    {
        return _logs.Where(l => l.TaskId == taskId).OrderByDescending(l => l.LogDateTime);
    }

    public async Task AddLogAsync(ProgressLog log)
    {
        _logs.Add(log);
        await SaveAsync(_logsFile, _logs);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DeleteLogAsync(Guid id)
    {
        _logs.RemoveAll(l => l.Id == id);
        await SaveAsync(_logsFile, _logs);
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
