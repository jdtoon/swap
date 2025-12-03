// Sidebar toggle for mobile
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    sidebar.classList.toggle('open');
}

// Close modal on Escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') {
        const modal = document.querySelector('.modal-overlay');
        if (modal) {
            modal.remove();
        }
    }
});

// Update nav active state on HTMX navigation
document.addEventListener('htmx:pushedIntoHistory', function (e) {
    updateNavActiveState(e.detail.path);
});

// Also handle browser back/forward navigation
window.addEventListener('popstate', function (e) {
    updateNavActiveState(window.location.pathname);
});

function updateNavActiveState(path) {
    const navItems = document.querySelectorAll('.nav-item');
    navItems.forEach(function (item) {
        item.classList.remove('active');
        const href = item.getAttribute('hx-get');
        if (href) {
            // Check if the path matches (handle both exact and prefix matching)
            if (path === href || (href !== '/' && path.startsWith(href))) {
                item.classList.add('active');
            } else if (href === '/' && path === '/') {
                item.classList.add('active');
            }
        }
    });
}

// Close modal on backdrop click (handled inline in modal template)
