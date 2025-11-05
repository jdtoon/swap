public partial class Program
{
    private static readonly object _dbInitLock = new();
    private static bool _dbInitialized;

    public static bool TryInitializeDatabase(IServiceProvider services)
    {
        lock (_dbInitLock)
        {
            if (_dbInitialized) return false;
            _dbInitialized = true;
            return true;
        }
    }
}
