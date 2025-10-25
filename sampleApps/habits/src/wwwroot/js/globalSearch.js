// Initialize the references getter
window.getSearchElements =
    window.getSearchElements ||
    function () {
        window.searchResults = document.getElementById("search-results");
        window.searchInput = document.querySelector(
            "#search-bar-placeholder input"
        );
    };

window.handleSearchClickOutside =
    window.handleSearchClickOutside ||
    function (e) {
        // Refresh references before using them
        window.getSearchElements();
        if (window.searchResults &&
            e.target) {
            if (
                !window.searchResults.contains(e.target) &&
                e.target !== window.searchInput
            ) {
                window.searchResults.classList.add("hidden");
            }
        }
    };

window.handleSearchResults =
    window.handleSearchResults ||
    function (e) {
        // Refresh references before using them
        window.getSearchElements();
        if (window.searchResults) {
            if (e.target.id === "search-results" && e.target.children.length > 0) {
                window.searchResults.classList.remove("hidden");
            }
        }
    };

window.handleSearchFocus =
    window.handleSearchFocus ||
    function () {
        // Refresh references before using them
        window.getSearchElements();
        if (window.searchResults) {
            if (window.searchResults.children.length > 0) {
                window.searchResults.classList.remove("hidden");
            }
        }
    };

// Initial setup
window.getSearchElements();

// Remove existing listeners
document.removeEventListener("click", window.handleSearchClickOutside);
document.removeEventListener("htmx:afterSwap", window.handleSearchResults);

// Add the listeners
document.addEventListener("click", window.handleSearchClickOutside);
document.addEventListener("htmx:afterSwap", window.handleSearchResults);

// Add focus listener if input exists
if (window.searchInput) {
    window.searchInput.removeEventListener("focus", window.handleSearchFocus);
    window.searchInput.addEventListener("focus", window.handleSearchFocus);
}