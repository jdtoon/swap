/**
 * Swap.Htmx Client Library
 * Handles toasts, events, and other client-side interactions.
 * @module Swap
 */
(function () {
    'use strict';

    // --- Toast System ---

    /**
     * Creates the toast container if it doesn't exist.
     * @returns {HTMLElement} The toast container element.
     */
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

    /**
     * Creates and displays a toast notification.
     * @param {string} message - The message to display.
     * @param {'info'|'success'|'warning'|'error'} [level='info'] - The severity level of the toast.
     */
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
        const closeBtn = toast.querySelector('.toast-close');
        if (closeBtn) {
            closeBtn.addEventListener('click', () => {
                clearTimeout(autoDismissTimer);
                dismissToast(toast);
            });
        }
    }

    /**
     * Dismisses a toast with an animation.
     * @param {HTMLElement} toast - The toast element to dismiss.
     */
    function dismissToast(toast) {
        toast.classList.remove('show');
        toast.classList.add('hide');
        setTimeout(() => toast.remove(), 300);
    }

    /**
     * Escapes HTML characters to prevent XSS.
     * @param {string} text - The text to escape.
     * @returns {string} The escaped HTML string.
     */
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Initializes the Swap client library.
     */
    function initialize() {
        // Listen for showToast events from server (via HX-Trigger header)
        document.body.addEventListener('showToast', (event) => {
            // @ts-ignore
            const { message, type } = event.detail;
            showToast(message, type || 'info');
        });

        // Listen for validationFailed events
        document.body.addEventListener('validationFailed', (event) => {
            // This event is triggered when SwapValidationErrors is used.
            // The form is usually re-rendered with errors, but you can use this 
            // to trigger additional UI effects (e.g., shake animation, scroll to error).
            // @ts-ignore
            console.debug('Swap.Htmx: Validation failed', event.detail);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Global API
    // @ts-ignore
    window.Swap = {
        showToast: showToast
    };

})();
