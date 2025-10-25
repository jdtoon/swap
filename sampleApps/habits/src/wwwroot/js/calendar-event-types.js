window.updateEventType =
  window.updateEventType ||
  function (id, element) {
    const nameInput =
      element.parentElement.parentElement.querySelector('input[type="text"]');
    const colorInput = document.getElementById(`color-${id}`);
    const iconSelect = document.getElementById(`icon-${id}`);

    const vals = {
      id: id,
      name: nameInput.value,
      color: colorInput.value,
      iconPath: iconSelect.value,
    };

    nameInput.setAttribute("hx-vals", JSON.stringify(vals));
    nameInput.dispatchEvent(new Event("change"));
  };

window.handleAfterSettle =
  window.handleAfterSettle ||
  function (evt) {
    updateMainNavbarBackgroundColor("main-navbar-placeholder", "#5ee9b5");
  };

// Remove existing listener before adding new one
document.removeEventListener("htmx:afterSettle", window.handleAfterSettle);
// Add the listener
document.addEventListener("htmx:afterSettle", window.handleAfterSettle);
