window.handleAfterSettle =
  window.handleAfterSettle ||
  function (evt) {
    // Call the color update after HTMX has settled
    updateMainNavbarBackgroundColor("main-navbar-placeholder", "#5ee9b5");
    // ... any other initialization code ...
  };

// Remove existing listener before adding new one
document.removeEventListener("htmx:afterSettle", window.handleAfterSettle);
// Add the listener
document.addEventListener("htmx:afterSettle", window.handleAfterSettle);

window.showAddEventModal =
  window.showAddEventModal ||
  function (date) {
    document.getElementById("startDate").value = date;
    document.getElementById("add-event-modal").checked = true;
  };

window.closeEditModal =
  window.closeEditModal ||
  function () {
    const modal = document.getElementById("edit-event-modal");
    if (modal) {
      modal.close(); // Close the modal
      modal.remove(); // Remove the modal from the DOM
    }
  };

window.toggleEditTimeInputs =
  window.toggleEditTimeInputs ||
  function (isFullDay) {
    const timeInputs = document.getElementById("edit-time-inputs");
    timeInputs.style.display = isFullDay ? "none" : "block";

    if (isFullDay) {
      document.querySelector('input[name="StartTime"]').value = "";
      document.querySelector('input[name="EndTime"]').value = "";
    }
  };

window.toggleTimeInputs =
  window.toggleTimeInputs ||
  function (isFullDay) {
    const timeInputs = document.getElementById("timeInputs");
    timeInputs.style.display = isFullDay ? "none" : "block";

    // Clear time inputs when full day is selected
    if (isFullDay) {
      document.querySelector('input[name="StartTime"]').value = "";
      document.querySelector('input[name="EndTime"]').value = "";
    }
  };

window.closeModal =
  window.closeModal ||
  function () {
    toggleTimeInputs(false);
    document.getElementById(`add-event-modal`).checked = false;
  };
