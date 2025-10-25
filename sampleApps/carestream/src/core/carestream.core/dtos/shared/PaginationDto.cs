namespace carestream.core.dtos.shared
{
    /// <summary>
    /// Data Transfer Object for carrying pagination metadata.
    /// </summary>
    public class PaginationDto
    {
        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        public int CurrentPage { get; set; } = 1;

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 25; // Default page size

        /// <summary>
        /// Gets or sets the total number of items available across all pages.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the base URL for HX-GET requests for pagination links.
        /// This should be dynamically set by the controller.
        /// </summary>
        public string HxGetUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTML ID of the target element for HX-GET pagination swaps.
        /// </summary>
        public string HxTarget { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HX-SWAP strategy for pagination links (e.g., "innerHTML", "outerHTML").
        /// </summary>
        public string HxSwap { get; set; } = "innerHTML";

        /// <summary>
        /// Check if there is a page before the current page
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Check if there is a page after the current page
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}