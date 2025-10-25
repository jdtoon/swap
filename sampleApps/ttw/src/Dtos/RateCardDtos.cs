using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Serialization;
using ttw.Data.Models;
using ttw.Enums;

namespace ttw.Dtos
{
    public class RateCardEditModel
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = "";
        public List<RateCardViewModel> Model { get; set; } = new List<RateCardViewModel>();

        [JsonIgnore]
        public Currency Currency { get; set; } = null!;

        [JsonIgnore]
        public bool IncludeSupplier { get; set; } = false;
    }

    [Serializable]
    public class RateCardViewModel
    {
        public int selectedHotel { get; set; } = 1;
        public int? selectedSupplier { get; set; } = 0;
        public int numberOfRows { get; set; } = 1;
        public int numberOfRooms { get; set; } = 1;
        public List<int> selectedRooms { get; set; } = new List<int>();
        public List<RateCardRowViewModel> rows { get; set; } = new List<RateCardRowViewModel>();
        public string mealNotes { get; set; } = "";
        public string additionalNotes { get; set; } = "";
        public bool mealPerRow { get; set; } = false;
        public int placementOrder { get; set; } = 0;

        [JsonIgnore]
        public SelectList Hotels { get; set; } = null!;

        [JsonIgnore]
        public SelectList Suppliers { get; set; } = null!;

        [JsonIgnore]
        public string SelectedHotelName { get; set; } = "";

        [JsonIgnore]
        public string SelectedSupplierName { get; set; } = "";

        [JsonIgnore]
        public List<string> SelectedRoomNames { get; set; } = new List<string>();

        [JsonIgnore]
        public City City { get; set; } = null!;
    }

    [Serializable]
    public class RateCardRowViewModel
    {
        public string from { get; set; } = "";
        public string to { get; set; } = "";
        public RowRateCard selection { get; set; }
        public List<RateCardRatesViewModel> rates { get; set; } = new List<RateCardRatesViewModel>();
        public string mealNote { get; set; } = "";
    }

    [Serializable]
    public class RateCardRatesViewModel
    {
        public int rate1 { get; set; } = 0;
        public int rate2 { get; set; } = 0;
    }

    public class RowRateSelectViewModel
    {
        public int Row { get; set; }
        public RowRateCard Selection { get; set; }
    }

    public class RateCardPartialViewModel
    {
        public int NoOfDates { get; set; } = 1;
        public int NoOfRoomTypes { get; set; } = 1;
        public List<RowRateSelectViewModel> RowRates { get; set; } = new List<RowRateSelectViewModel>();
        public int CardId { get; set; } = 0;
        public bool MealPerRow { get; set; } = false;
    }

    public class RateCardTableViewModel
    {
        public string Actions { get; set; } = null!;

        public int Id { get; set; }

        public string Name { get; set; } = null!;
    }

    public class RateCardViewTableViewModel
    {
        public List<RateCardTableViewModel> RateCards { get; set; } = null!;

        public int filteredResultsCount { get; set; }

        public int totalResultsCount { get; set; }
    }

    public class RateCardDisplayTableViewModel
    {
        public RateCardViewTableViewModel ViewTable { get; set; } = null!;

        public List<RateCardTableViewModel> RateCards { get; set; } = null!;
    }

    public class SelectViewModel<T>
    {
        public required T Id { get; set; }
        public string Name { get; set; } = null!;
    }
}