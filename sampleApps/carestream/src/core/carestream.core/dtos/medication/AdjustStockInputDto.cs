namespace carestream.core.dtos.medication
{
    /// <summary>
    /// DTO for capturing input to adjust medication stock levels.
    /// </summary>
    public class AdjustStockInputDto
    {
        /// <summary>
        /// Gets or sets the ID of the medication whose stock is to be adjusted.
        /// </summary>
        public int MedicationId { get; set; }

        /// <summary>
        /// Gets or sets the name of the medication (for display purposes in the form).
        /// </summary>
        public string MedicationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current quantity of the medication (for display purposes in the form).
        /// </summary>
        public int CurrentQuantity { get; set; }

        /// <summary>
        /// Gets or sets the quantity by which to adjust the stock.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the adjustment is an increment (true) or a decrement (false).
        /// </summary>
        public bool IsIncrement { get; set; }
    }
}