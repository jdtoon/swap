using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KanbanApp.Data;

public class Board
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int Position { get; set; }
    
    public bool IsArchived { get; set; }
    
    // Navigation
    public ICollection<KanbanList> Lists { get; set; } = new List<KanbanList>();
}

public class KanbanList
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public int Position { get; set; }
    
    public bool IsArchived { get; set; }
    
    // Foreign Key
    public int BoardId { get; set; }
    
    // For form binding (like TTW's MapCityId pattern)
    [NotMapped]
    public int MapBoardId { get; set; }
    
    // Navigation
    public Board Board { get; set; } = null!;
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}

public class Card
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int Position { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? DueDate { get; set; }
    
    public CardPriority Priority { get; set; } = CardPriority.Medium;
    
    // Foreign Key
    public int ListId { get; set; }
    
    // For form binding (TTW pattern)
    [NotMapped]
    public int MapListId { get; set; }
    
    // Navigation
    public KanbanList List { get; set; } = null!;
}

public enum CardPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}
