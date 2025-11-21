using SwapExpenses.Models;

namespace SwapExpenses.Services;

public class ExpenseService
{
    private readonly List<Expense> _expenses = new();
    private int _nextId = 1;

    public ExpenseService()
    {
        // Seed some data
        Add(new Expense { Description = "Coffee", Amount = 4.50m, Category = "Food" });
        Add(new Expense { Description = "Taxi", Amount = 25.00m, Category = "Transport" });
        Add(new Expense { Description = "Lunch", Amount = 12.00m, Category = "Food" });
    }

    public List<Expense> GetAll() => _expenses.OrderByDescending(e => e.Date).ToList();

    public decimal GetTotal() => _expenses.Sum(e => e.Amount);

    public void Add(Expense expense)
    {
        expense.Id = _nextId++;
        expense.Date = DateTime.Now;
        _expenses.Add(expense);
    }

    public void Delete(int id)
    {
        var expense = _expenses.FirstOrDefault(e => e.Id == id);
        if (expense != null)
        {
            _expenses.Remove(expense);
        }
    }
}
