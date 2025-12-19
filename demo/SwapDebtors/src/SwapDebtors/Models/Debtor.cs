namespace SwapDebtors.Models;

public class Debtor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public ICollection<Debt> Debts { get; set; } = [];
    
    // Computed properties
    public decimal TotalOwed => Debts.Where(d => !d.IsPaid).Sum(d => d.Amount);
    public int ActiveDebtsCount => Debts.Count(d => !d.IsPaid);
}
