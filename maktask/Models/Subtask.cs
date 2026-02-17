using System;

namespace maktask.Models;

public class Subtask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; } = DateTime.Today;
    public DateTime EndDateTime { get; set; } = DateTime.Today;
    public bool IsCompleted { get; set; } = false;
    public bool IsAllDay { get; set; } = true;
    public bool IsDeadlineMode { get; set; } = false;
}
