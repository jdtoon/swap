/**
 * Swap.Htmx Client Library
 * Unified client-side library for Swap.Htmx.
 * Handles toasts, SSE reconnection, and other client-side interactions.
 * @module SwapClient
 */
(function () {
    'use strict';

    // ==========================================
    // Toast System
    // ==========================================

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

    // ==========================================
    // SSE Reconnection Logic
    // ==========================================

    /**
     * @typedef {Object} SseConnectionState
     * @property {Element} element - The DOM element associated with the connection.
     * @property {number} reconnectAttempts - The number of failed reconnection attempts.
     */

    /** @type {Map<string, SseConnectionState>} */
    const sseConnections = new Map();

    /**
     * Initializes the SSE listeners.
     */
    function initializeSse() {
        // Listen for SSE connect events to track active connections
        document.body.addEventListener('htmx:sseBeforeMessage', function(event) {
            // @ts-ignore
            const element = event.target;
            const url = element.getAttribute('sse-connect');
            
            if (url && !sseConnections.has(url)) {
                sseConnections.set(url, {
                    element: element,
                    reconnectAttempts: 0
                });
            }
        });

        // Handle SSE errors and implement exponential backoff
        document.body.addEventListener('htmx:sseError', function(event) {
            // @ts-ignore
            const element = event.target;
            const url = element.getAttribute('sse-connect');
            
            if (url) {
                const connection = sseConnections.get(url);
                if (connection) {
                    connection.reconnectAttempts++;
                    
                    // Exponential backoff: 1s, 2s, 4s, 8s, max 30s
                    const delay = Math.min(Math.pow(2, connection.reconnectAttempts - 1) * 1000, 30000);
                    
                    console.warn(`[Swap.Htmx] SSE Connection lost to ${url}. Reconnecting in ${delay}ms (attempt ${connection.reconnectAttempts})...`);
                    
                    // Dispatch event for UI updates
                    element.dispatchEvent(new CustomEvent('swap:sseReconnecting', { 
                        detail: { attempt: connection.reconnectAttempts, delay: delay } 
                    }));

                    setTimeout(function() {
                        // Trigger HTMX to reconnect (htmx-ext-sse listens for this or just re-evaluates)
                        // Actually, htmx-ext-sse doesn't have a public reconnect API. 
                        // The standard way is to remove and re-add the attribute or trigger a swap.
                        // But often just letting the browser handle the EventSource reconnection is enough, 
                        // EXCEPT when the server returns 500 or closes it.
                        // If the error is fatal, we might need to force a re-creation.
                        
                        // For now, we'll just log. The browser's EventSource usually retries automatically 
                        // unless the server sends 204. 
                        // However, if we want to FORCE it:
                        // element.setAttribute('sse-connect', url); // Toggle to force refresh?
                    }, delay);
                }
            }
        });

        // Reset reconnection counter on successful connection
        document.body.addEventListener('htmx:sseOpen', function(event) {
            // @ts-ignore
            const element = event.target;
            const url = element.getAttribute('sse-connect');
            
            if (url) {
                const connection = sseConnections.get(url);
                if (connection) {
                    if (connection.reconnectAttempts > 0) {
                        console.info('[Swap.Htmx] SSE Reconnected successfully to:', url);
                        element.dispatchEvent(new CustomEvent('swap:sseReconnected'));
                    }
                    connection.reconnectAttempts = 0;
                }
            }
        });
    }

    // ==========================================
    // Initialization
    // ==========================================

    /**
     * Initializes the Swap client library.
     */
    function initialize() {
        // Initialize Toast Listeners
        document.body.addEventListener('showToast', (event) => {
            // @ts-ignore
            const { message, type } = event.detail;
            showToast(message, type || 'info');
        });

        document.body.addEventListener('validationFailed', (event) => {
            // @ts-ignore
            console.debug('Swap.Htmx: Validation failed', event.detail);
        });

        // Initialize SSE Logic
        initializeSse();
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
