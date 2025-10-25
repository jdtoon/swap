using System;
using System.ComponentModel.DataAnnotations;

namespace carestream.core.dtos.medication
{
    /// <summary>
    /// DTO for displaying detailed medication stock information in the inventory view.
    /// </summary>
    public class MedicationStockDetailDto
    {
        public int MedicationId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty; // From app.medications

        [StringLength(100)]
        public string? Strength { get; set; } // From app.medications

        [StringLength(100)]
        public string? Form { get; set; } // From app.medications

        [StringLength(100)]
        public string? Category { get; set; } // From app.medications

        public int QuantityOnHand { get; set; } // From app.medication_stock
        public int MinimumStockLevel { get; set; } // From app.medication_stock

        /// <summary>
        /// Indicates the stock status (e.g., "In Stock", "Low Stock", "Out of Stock").
        /// This is derived logic, not a direct DB column.
        /// </summary>
        public string StockStatus { get; set; } = string.Empty; // Calculated property

        public DateTimeOffset LastUpdatedAt { get; set; } // From app.medication_stock
    }
}