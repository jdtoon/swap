using carestream.core.dtos.shared;

namespace carestream.core.dtos.medication
{
    /// <summary>
    /// Represents a view model for displaying a paginated list of medication stock details.
    /// </summary>
    public class MedicationInventoryViewModel
    {
        /// <summary>
        /// Gets or sets the list of detailed medication stock items.
        /// </summary>
        public IEnumerable<MedicationStockDetailDto> InventoryItems { get; set; } = new List<MedicationStockDetailDto>();

        /// <summary>
        /// Gets or sets the pagination details for the inventory list.
        /// </summary>
        public PaginationDto Pagination { get; set; } = new PaginationDto();

        /// <summary>
        /// Gets or sets the filtering and pagination options used to generate this view model.
        /// </summary>
        public FilterAndPaginationOptions Filters { get; set; } = new FilterAndPaginationOptions();

        /// <summary>
        /// Gets or sets the total count of medications currently below their minimum stock level.
        /// </summary>
        public int LowStockItemsCount { get; set; }
    }
}