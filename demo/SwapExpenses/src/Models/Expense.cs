using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SwapExpenses.Models;

public class Expense
{
    [ValidateNever]
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = "General";
    [ValidateNever]
    public DateTime Date { get; set; } = DateTime.Now;
}
