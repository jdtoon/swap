window.handleAfterSettleHome =
    window.handleAfterSettleHome ||
    function (evt) {
        // Call the color update after HTMX has settled
        updateMainNavbarBackgroundColor("main-navbar-placeholder", "#4a5662");
        // ... any other initialization code ...
    };

// Remove existing listener before adding new one
document.removeEventListener("htmx:afterSettle", window.handleAfterSettleHome);
// Add the listener
document.addEventListener("htmx:afterSettle", window.handleAfterSettleHome);