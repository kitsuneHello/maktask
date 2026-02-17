using System;
using System.Collections.Generic;

namespace maktask.Models;

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; } = DateTime.Today;
    public DateTime EndDateTime { get; set; } = DateTime.Today;
    public DateTime? Deadline { get; set; }
    public bool IsAllDay { get; set; } = true;
    public bool IsDeadlineMode { get; set; } = false;
    public string Memo { get; set; } = string.Empty;
    public List<Subtask> Subtasks { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int DurationDays => (EndDateTime.Date - StartDateTime.Date).Days + 1;
}
