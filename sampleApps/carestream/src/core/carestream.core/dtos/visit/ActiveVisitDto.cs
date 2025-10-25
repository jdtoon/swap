namespace carestream.core.dtos.visit
{
    public class ActiveVisitDto
    {
        public int VisitId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime VisitTimestamp { get; set; }
    }
}
