/**
 * Swap.Htmx Client Library
 * Handles toasts, events, and other client-side interactions.
 */
(function () {
    'use strict';

    // --- Toast System ---

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

    function initialize() {
        // Listen for showToast events from server (via HX-Trigger header)
        document.body.addEventListener('showToast', (event) => {
            const { message, type } = event.detail;
            showToast(message, type || 'info');
        });

        // Listen for validationFailed events
        document.body.addEventListener('validationFailed', (event) => {
            // This event is triggered when SwapValidationErrors is used.
            // The form is usually re-rendered with errors, but you can use this 
            // to trigger additional UI effects (e.g., shake animation, scroll to error).
            console.debug('Swap.Htmx: Validation failed', event.detail);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Global API
    window.Swap = {
        showToast: showToast
    };

})();
