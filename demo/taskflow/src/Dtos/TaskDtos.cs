namespace TaskFlow.Dtos;

public class TaskStatsDto
{
    public int TotalTasks { get; set; }
    public int TodoCount { get; set; }
    public int InProgressCount { get; set; }
    public int DoneCount { get; set; }
    public int HighPriorityCount { get; set; }
    public int OverdueCount { get; set; }
}

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class UpdateTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; }
    public int Priority { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class TaskFilterDto
{
    public string? Search { get; set; }
    public int? Status { get; set; }
    public int? Priority { get; set; }
    public string? AssignedTo { get; set; }
}
