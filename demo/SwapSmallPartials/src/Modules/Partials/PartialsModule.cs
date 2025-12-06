namespace SwapSmallPartials.Modules.Partials;

public static class PartialsModule
{
    public static IServiceCollection AddPartialsModule(this IServiceCollection services)
    {
        services.AddSingleton<PartialsState>();
        return services;
    }
}

/// <summary>
/// Shared state for all the small partials.
/// Each partial reads/writes to this state.
/// </summary>
public class PartialsState
{
    private readonly object _lock = new();
    
    // Counters (10 of them)
    public int Counter1 { get; private set; }
    public int Counter2 { get; private set; }
    public int Counter3 { get; private set; }
    public int Counter4 { get; private set; }
    public int Counter5 { get; private set; }
    public int Counter6 { get; private set; }
    public int Counter7 { get; private set; }
    public int Counter8 { get; private set; }
    public int Counter9 { get; private set; }
    public int Counter10 { get; private set; }
    
    // Status flags (5 of them)
    public bool Status1 { get; private set; }
    public bool Status2 { get; private set; }
    public bool Status3 { get; private set; }
    public bool Status4 { get; private set; }
    public bool Status5 { get; private set; }
    
    // Progress values (5 of them - 0-100)
    public int Progress1 { get; private set; }
    public int Progress2 { get; private set; }
    public int Progress3 { get; private set; }
    public int Progress4 { get; private set; }
    public int Progress5 { get; private set; }
    
    // Computed values (5 of them)
    public int Total => Counter1 + Counter2 + Counter3 + Counter4 + Counter5 + 
                        Counter6 + Counter7 + Counter8 + Counter9 + Counter10;
    public double Average => Total / 10.0;
    public int Max => new[] { Counter1, Counter2, Counter3, Counter4, Counter5, 
                              Counter6, Counter7, Counter8, Counter9, Counter10 }.Max();
    public int Min => new[] { Counter1, Counter2, Counter3, Counter4, Counter5, 
                              Counter6, Counter7, Counter8, Counter9, Counter10 }.Min();
    public int ActiveStatuses => (Status1 ? 1 : 0) + (Status2 ? 1 : 0) + (Status3 ? 1 : 0) + 
                                 (Status4 ? 1 : 0) + (Status5 ? 1 : 0);
    
    public void IncrementCounter(int number)
    {
        lock (_lock)
        {
            switch (number)
            {
                case 1: Counter1++; break;
                case 2: Counter2++; break;
                case 3: Counter3++; break;
                case 4: Counter4++; break;
                case 5: Counter5++; break;
                case 6: Counter6++; break;
                case 7: Counter7++; break;
                case 8: Counter8++; break;
                case 9: Counter9++; break;
                case 10: Counter10++; break;
            }
        }
    }
    
    public void ToggleStatus(int number)
    {
        lock (_lock)
        {
            switch (number)
            {
                case 1: Status1 = !Status1; break;
                case 2: Status2 = !Status2; break;
                case 3: Status3 = !Status3; break;
                case 4: Status4 = !Status4; break;
                case 5: Status5 = !Status5; break;
            }
        }
    }
    
    public void UpdateProgress(int number, int delta)
    {
        lock (_lock)
        {
            switch (number)
            {
                case 1: Progress1 = Math.Clamp(Progress1 + delta, 0, 100); break;
                case 2: Progress2 = Math.Clamp(Progress2 + delta, 0, 100); break;
                case 3: Progress3 = Math.Clamp(Progress3 + delta, 0, 100); break;
                case 4: Progress4 = Math.Clamp(Progress4 + delta, 0, 100); break;
                case 5: Progress5 = Math.Clamp(Progress5 + delta, 0, 100); break;
            }
        }
    }
    
    public int GetCounter(int number) => number switch
    {
        1 => Counter1, 2 => Counter2, 3 => Counter3, 4 => Counter4, 5 => Counter5,
        6 => Counter6, 7 => Counter7, 8 => Counter8, 9 => Counter9, 10 => Counter10,
        _ => 0
    };
    
    public bool GetStatus(int number) => number switch
    {
        1 => Status1, 2 => Status2, 3 => Status3, 4 => Status4, 5 => Status5,
        _ => false
    };
    
    public int GetProgress(int number) => number switch
    {
        1 => Progress1, 2 => Progress2, 3 => Progress3, 4 => Progress4, 5 => Progress5,
        _ => 0
    };
    
    public void Reset()
    {
        lock (_lock)
        {
            Counter1 = Counter2 = Counter3 = Counter4 = Counter5 = 0;
            Counter6 = Counter7 = Counter8 = Counter9 = Counter10 = 0;
            Status1 = Status2 = Status3 = Status4 = Status5 = false;
            Progress1 = Progress2 = Progress3 = Progress4 = Progress5 = 0;
        }
    }
}
