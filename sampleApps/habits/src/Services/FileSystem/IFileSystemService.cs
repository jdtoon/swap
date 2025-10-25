namespace habits.Services.FileSystem
{
    public interface IFileSystemService
    {
        IEnumerable<string> GetCalendarTypeFiles();
    }
}
