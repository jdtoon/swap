namespace habits.Dtos
{
    public class LoyaltyCardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
} 