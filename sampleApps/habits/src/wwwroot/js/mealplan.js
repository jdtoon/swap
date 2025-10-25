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