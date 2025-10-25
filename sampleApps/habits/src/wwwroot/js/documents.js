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

window.clearModal =
  window.clearModal ||
  function () {
    document.getElementById("add-document-form").reset();
    document.getElementById("add-document-modal").checked = false;
  };

document.addEventListener("htmx:beforeRequest", (event) => {
  if (event.target.id === "prev-page") {
    window.currentPage = Math.max(1, window.currentPage - 1);
  } else if (event.target.id === "next-page") {
    window.currentPage++;
  }
});

document.addEventListener("htmx:afterRequest", (event) => {
  if (event.target.id === "add-document-form") {
    window.clearModal();
    const errorDiv = document.getElementById("error-message");
    const errorText = errorDiv.querySelector(".error-text");

    if (!event.detail.successful) {
      errorText.textContent =
        evt.detail.xhr.responseText || "An error occurred";
      errorDiv.classList.remove("hidden");
      setTimeout(() => {
        errorDiv.classList.add("hidden");
      }, 5000);
    }
  }
});

document.addEventListener("htmx:afterOnLoad", () => {
  window.updateButtonStates();
});

document
  .getElementById("search-input")
  .addEventListener("change", window.resetCurrentPage);

window.updateButtonStates();

window.handleAfterSettle =
  window.handleAfterSettle ||
  function (evt) {
    updateMainNavbarBackgroundColor("main-navbar-placeholder", "#5ee9b5");
  };

// Remove existing listener before adding new one
document.removeEventListener("htmx:afterSettle", window.handleAfterSettle);
// Add the listener
document.addEventListener("htmx:afterSettle", window.handleAfterSettle);
