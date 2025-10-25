// Function to close the mobile drawer
function closeDrawer() {
    const drawerCheckbox = document.getElementById('main-drawer');
    if (drawerCheckbox) {
        drawerCheckbox.checked = false;
    }
}

// Close drawer when navigation items are clicked (mobile only)
document.addEventListener('click', function(e) {
    if (window.innerWidth < 1024) { // lg breakpoint
        const navContent = document.getElementById('nav-content');
        if (navContent && navContent.contains(e.target) && e.target.closest('a')) {
            closeDrawer();
        }
    }
});

// Close dropdown when items are clicked
document.addEventListener('click', function(e) {
    const dropdownMenuItem = e.target.closest('.dropdown-content a, .dropdown-content button');
    if (dropdownMenuItem) {
        // Find the closest dropdown menu and remove focus from all its focusable elements
        const dropdownMenu = dropdownMenuItem.closest('.dropdown');
        if (dropdownMenu) {
            const focusableElements = dropdownMenu.querySelectorAll('[tabindex="0"]');
            focusableElements.forEach(element => element.blur());
        }
    }
}); 