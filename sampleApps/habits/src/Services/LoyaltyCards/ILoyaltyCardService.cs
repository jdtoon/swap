using habits.Dtos;

namespace habits.Services.LoyaltyCards
{
    public interface ILoyaltyCardService
    {
        List<LoyaltyCardDto> GetCards();
        LoyaltyCardDto GetCard(int id);
        LoyaltyCardDto AddCard(string name, string barcode);
        void DeleteCard(int id);
        void UpdateCard(int id, string name);
    }
} 