// In your page scripts
window.currentPage = window.currentPage || 1;

window.updateButtonStates =
  window.updateButtonStates ||
  function () {
    const prevButton = document.getElementById("prev-page");
    const nextButton = document.getElementById("next-page");

    if (prevButton) {
      prevButton.disabled = window.currentPage === 1;
    }

    if (nextButton) {
      const isLastPage = document.getElementById("table_end") !== null;
      nextButton.disabled = isLastPage;
    }
  };

window.resetCurrentPage =
  window.resetCurrentPage ||
  function () {
    window.currentPage = 1;
  };

window.handleBeforeRequest =
  window.handleBeforeRequest ||
  function (event) {
    if (event.target.id === "prev-page") {
      window.currentPage = Math.max(1, window.currentPage - 1);
    } else if (event.target.id === "next-page") {
      window.currentPage++;
    }
  };

window.handleAfterLoad =
  window.handleAfterLoad ||
  function () {
    window.updateButtonStates();
  };

window.handleAfterSettle =
  window.handleAfterSettle ||
  function (evt) {
    updateMainNavbarBackgroundColor("main-navbar-placeholder", "#5ee9b5");
  };

// Remove existing listeners before adding new ones
document.removeEventListener("htmx:beforeRequest", window.handleBeforeRequest);
document.removeEventListener("htmx:afterOnLoad", window.handleAfterLoad);
document.removeEventListener("htmx:afterSettle", window.handleAfterSettle);

// Add the listeners
document.addEventListener("htmx:beforeRequest", window.handleBeforeRequest);
document.addEventListener("htmx:afterOnLoad", window.handleAfterLoad);
document.addEventListener("htmx:afterSettle", window.handleAfterSettle);

// Handle search and filter inputs
window.searchInput = document.getElementById("search-input");
if (window.searchInput) {
  window.searchInput.removeEventListener("change", window.resetCurrentPage);
  window.searchInput.addEventListener("change", window.resetCurrentPage);
}

window.statusFilter = document.getElementById("status-filter");
if (window.statusFilter) {
  window.statusFilter.removeEventListener("change", window.resetCurrentPage);
  window.statusFilter.addEventListener("change", window.resetCurrentPage);
}

window.updateButtonStates();
