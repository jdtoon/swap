/**
 * Swap.Htmx Toast System
 * Listens for swap:toast events and displays notifications
 */
(function () {
    'use strict';

    // Create toast container if it doesn't exist
    function ensureToastContainer() {
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.setAttribute('aria-live', 'polite');
            container.setAttribute('aria-atomic', 'true');
            document.body.appendChild(container);
        }
        return container;
    }

    // Create and display a toast
    function showToast(message, level = 'info') {
        const container = ensureToastContainer();

        const toast = document.createElement('div');
        toast.className = `toast toast-${level}`;
        toast.setAttribute('role', 'status');
        toast.innerHTML = `
            <div class="toast-content">
                <span class="toast-message">${escapeHtml(message)}</span>
                <button class="toast-close" aria-label="Close">&times;</button>
            </div>
        `;

        // Add to container
        container.appendChild(toast);

        // Animate in
        setTimeout(() => toast.classList.add('show'), 10);

        // Auto-dismiss after 5 seconds
        const autoDismissTimer = setTimeout(() => dismissToast(toast), 5000);

        // Manual dismiss
        toast.querySelector('.toast-close').addEventListener('click', () => {
            clearTimeout(autoDismissTimer);
            dismissToast(toast);
        });
    }

    // Dismiss a toast with animation
    function dismissToast(toast) {
        toast.classList.remove('show');
        toast.classList.add('hide');
        setTimeout(() => toast.remove(), 300);
    }

    // Escape HTML to prevent XSS
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Listen for swap:toast events from server
    document.addEventListener('swap:toast', (event) => {
        const { message, level } = event.detail;
        showToast(message, level || 'info');
    });

    // Global function for manual toast triggering (optional)
    window.swapToast = showToast;
})();
