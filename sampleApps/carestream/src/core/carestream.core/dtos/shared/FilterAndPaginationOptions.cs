using System;

namespace carestream.core.dtos.shared
{
    public class FilterAndPaginationOptions
    {
        /// <summary>
        /// Generic search term for filtering results.
        /// </summary>
        public string? SearchTerm1 { get; set; } // Generic search term

        /// <summary>
        /// Another generic search term for additional filtering.
        /// </summary>
        public string? SearchTerm2 { get; set; } // Another generic search term

        /// <summary>
        /// Optional start date for filtering results based on a date range.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optional end date for filtering results based on a date range.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// The page number for pagination, starting from 1.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The number of items per page for pagination.
        /// </summary>
        public int PageSize { get; set; } = 25; // Default page size

        /// <summary>
        /// The property to sort by, e.g., "DateDesc", "NameAsc", etc.
        /// </summary>
        public string SortBy { get; set; } = "DefaultSort"; // e.g., "DateDesc"

        /// <summary>
        /// Indicates whether the sorting should be in ascending order.
        /// </summary>
        public bool SortAscending { get; set; } = false;

        /// <summary>
        /// Optional filter for active status (e.g., true for active only, false for inactive only, null for all).
        /// </summary>
        public bool? IsActiveFilter { get; set; } // ADDED: IsActiveFilter
    }
}