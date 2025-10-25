using Microsoft.AspNetCore.Mvc;
using habits.Services.LoyaltyCards;
using Microsoft.AspNetCore.Authorization;

namespace habits.Controllers
{
    [Authorize]
    public class LoyaltyCardController : Controller
    {
        private readonly ILoyaltyCardService _cardService;

        public LoyaltyCardController(ILoyaltyCardService cardService)
        {
            _cardService = cardService;
        }

        public IActionResult Index()
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();
            return View();
        }

        public IActionResult GetCards()
        {
            var cards = _cardService.GetCards();
            return PartialView("_Cards", cards);
        }

        public IActionResult GetCard(int id)
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");
            var card = _cardService.GetCard(id);
            return PartialView("_CardDisplay", card);
        }

        public IActionResult AddCard()
        {
            return PartialView("_AddCardModal");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCard(string name, string barcode)
        {
            var card = _cardService.AddCard(name, barcode);
            return PartialView("_CardItem", card);
        }

        [HttpDelete]
        public IActionResult DeleteCard(int id)
        {
            _cardService.DeleteCard(id);
            return new EmptyResult();
        }

        [HttpPut]
        public IActionResult UpdateCard(int id, string name)
        {
            _cardService.UpdateCard(id, name);
            var card = _cardService.GetCard(id);
            return PartialView("_CardItem", card);
        }
    }
} 