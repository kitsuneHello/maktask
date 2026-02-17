using System;

namespace maktask.Models;

public class ProgressLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime LogDateTime { get; set; } = DateTime.Now;
    public string Memo { get; set; } = string.Empty;
}
