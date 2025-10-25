namespace ttw.Dtos.Data
{
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public bool HasMore { get; set; } = false;
        public int TotalRecords { get; set; } = 0;
        public int CurrentPage { get; set; } = 1;
    }
}