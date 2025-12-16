namespace SwapDebtors.Models;

public class Debt
{
    public int Id { get; set; }
    public int DebtorId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsPaid { get; set; }

    // Navigation property
    public Debtor Debtor { get; set; } = null!;
}
