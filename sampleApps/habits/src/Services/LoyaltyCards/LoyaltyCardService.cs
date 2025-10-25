using habits.Data;
using habits.Data.Models;
using habits.Dtos;
using Microsoft.EntityFrameworkCore;

namespace habits.Services.LoyaltyCards
{
    public class LoyaltyCardService : ILoyaltyCardService
    {
        private readonly ApplicationDbContext _context;

        public LoyaltyCardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<LoyaltyCardDto> GetCards()
        {
            return _context.LoyaltyCards
                .OrderBy(c => c.Name)
                .Select(c => new LoyaltyCardDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Barcode = c.Barcode,
                    CreatedAt = c.CreatedAt
                })
                .ToList();
        }

        public LoyaltyCardDto GetCard(int id)
        {
            var card = _context.LoyaltyCards
                .Where(c => c.Id == id)
                .Select(c => new LoyaltyCardDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Barcode = c.Barcode,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefault();

            if (card == null)
                throw new InvalidOperationException("Card not found");

            return card;
        }

        public LoyaltyCardDto AddCard(string name, string barcode)
        {
            var card = new LoyaltyCard
            {
                Name = name,
                Barcode = barcode
            };

            _context.LoyaltyCards.Add(card);
            _context.SaveChanges();

            return new LoyaltyCardDto
            {
                Id = card.Id,
                Name = card.Name,
                Barcode = card.Barcode,
                CreatedAt = card.CreatedAt
            };
        }

        public void DeleteCard(int id)
        {
            var card = _context.LoyaltyCards
                .FirstOrDefault(c => c.Id == id);

            if (card != null)
            {
                _context.LoyaltyCards.Remove(card);
                _context.SaveChanges();
            }
        }

        public void UpdateCard(int id, string name)
        {
            var card = _context.LoyaltyCards
                .FirstOrDefault(c => c.Id == id);

            if (card != null)
            {
                card.Name = name;
                _context.SaveChanges();
            }
        }
    }
} 