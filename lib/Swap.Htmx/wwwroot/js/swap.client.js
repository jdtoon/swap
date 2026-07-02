/**
 * Swap.Htmx Client Library
 * Unified client-side library for Swap.Htmx.
 * Handles toasts, SSE reconnection, validation, and other client-side interactions.
 * @module SwapClient
 */
(function () {
    'use strict';

    // ==========================================
    // Validation Error Handling
    // ==========================================

    /**
     * Handles the validationFailed event by updating swap-validation elements.
     * @param {Object} errors - Dictionary of field names to error messages.
     */
    function handleValidationErrors(errors) {
        // Clear all existing validation messages first
        document.querySelectorAll('[data-swap-validation]').forEach(function(el) {
            el.textContent = '';
            el.classList.remove('field-validation-error');
            el.removeAttribute('aria-invalid');
            
            // Also clear aria-invalid from associated input if present
            var fieldName = el.getAttribute('data-swap-validation');
            var input = document.querySelector('[name="' + fieldName + '"]');
            if (input) {
                input.removeAttribute('aria-invalid');
                input.removeAttribute('aria-describedby');
            }
        });

        // Set new error messages
        if (errors && typeof errors === 'object') {
            Object.keys(errors).forEach(function(fieldName) {
                var messages = errors[fieldName];
                var message = Array.isArray(messages) ? messages[0] : messages;
                
                // Find the validation element for this field
                var el = document.querySelector('[data-swap-validation="' + fieldName + '"]');
                if (el) {
                    el.textContent = message;
                    el.classList.add('field-validation-error');
                    el.setAttribute('aria-invalid', 'true');
                    
                    // Also set aria-invalid on the associated input for screen readers
                    var input = document.querySelector('[name="' + fieldName + '"]');
                    if (input) {
                        input.setAttribute('aria-invalid', 'true');
                        input.setAttribute('aria-describedby', el.id);
                    }
                }
            });
        }
    }

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
    // OOB Guards (data-swap-seq + data-swap-hash)
    // ==========================================

    /** @type {Map<string, number>} Highest applied sequence per target id. */
    const swapSeq = new Map();

    /**
     * Guards out-of-band swaps: drops stale/duplicate updates via the monotonic data-swap-seq stamp,
     * and skips unchanged content via the data-swap-hash fingerprint (matching the target's current
     * hash). Fragments carrying neither attribute are unaffected.
     */
    function skipSwap(event) {
        // htmx honors detail.shouldSwap to cancel an OOB swap; preventDefault is a belt-and-braces fallback.
        // @ts-ignore
        if (event.detail) {
            event.detail.shouldSwap = false;
        }
        // @ts-ignore
        if (typeof event.preventDefault === 'function') {
            event.preventDefault();
        }
    }

    function initializeOobGuards() {
        document.body.addEventListener('htmx:oobBeforeSwap', function(event) {
            // @ts-ignore
            var detail = event.detail || {};

            // htmx passes the EXISTING element as detail.target and the incoming content as
            // detail.fragment (a DocumentFragment). Read the target's id, then find the matching incoming
            // element inside the fragment — the fragment itself has no getAttribute.
            var target = detail.target;
            if (!target || typeof target.getAttribute !== 'function' || !target.id) {
                return;
            }
            var id = target.id;

            var incoming = null;
            if (detail.fragment && typeof detail.fragment.querySelector === 'function') {
                incoming = detail.fragment.querySelector('[id="' + id.replace(/"/g, '\\"') + '"]');
            }
            if (!incoming && detail.newNode && typeof detail.newNode.getAttribute === 'function') {
                incoming = detail.newNode; // fallback for htmx detail shapes that pass the element directly
            }
            if (!incoming) {
                return;
            }

            // Note: htmx fires htmx:oobBeforeSwap only when the target EXISTS (missing targets fire
            // htmx:oobErrorNoTarget and are skipped natively), so AlsoUpdateIfExists needs no guard
            // here — the data-swap-if-exists marker is consumed by the dev tools to suppress the
            // otherwise-expected "no target" warning.

            // 1. Out-of-order guard (data-swap-seq): drop stale or duplicate updates.
            var seqStr = incoming.getAttribute('data-swap-seq');
            if (seqStr !== null) {
                var seq = parseInt(seqStr, 10);
                if (!isNaN(seq)) {
                    var prev = swapSeq.get(id);
                    if (prev !== undefined && seq <= prev) {
                        skipSwap(event);
                        return;
                    }
                    swapSeq.set(id, seq);
                }
            }

            // 2. Unchanged-content guard (data-swap-hash): skip when the incoming content fingerprint
            // matches what's already on the target element (avoids needless re-render + lost focus/scroll).
            var hashStr = incoming.getAttribute('data-swap-hash');
            if (hashStr !== null && target.getAttribute('data-swap-hash') === hashStr) {
                skipSwap(event);
                return;
            }
        });
    }

    // ==========================================
    // Optimistic UI rollback (data-swap-optimistic)
    // ==========================================

    /** @type {WeakMap<Element, {target: Element, html: string}>} Pre-request snapshots for rollback. */
    const optimisticSnapshots = new WeakMap();

    /**
     * Safe optimistic UI: when a request originates from an element carrying
     * data-swap-optimistic="<css-selector>", snapshot that target's inner HTML before the request and
     * add a 'swap-pending' class. On success htmx swaps in the authoritative content; on any failure
     * (non-2xx, network error, or timeout) the snapshot is restored — so a rejected request never
     * leaves an optimistic change stuck on screen. An empty attribute value falls back to the request's
     * hx-target (or the element itself).
     */
    function initializeOptimistic() {
        document.body.addEventListener('htmx:beforeRequest', function(event) {
            // @ts-ignore
            var detail = event.detail || {};
            var elt = detail.elt;
            if (!elt || typeof elt.getAttribute !== 'function') {
                return;
            }

            var sel = elt.getAttribute('data-swap-optimistic');
            if (sel === null) {
                return;
            }

            var target = sel ? document.querySelector(sel) : (detail.target || elt);
            if (!target) {
                return;
            }

            optimisticSnapshots.set(elt, { target: target, html: target.innerHTML });
            if (elt.classList) {
                elt.classList.add('swap-pending');
            }
        });

        document.body.addEventListener('htmx:afterRequest', function(event) {
            // @ts-ignore
            var detail = event.detail || {};
            var elt = detail.elt;
            if (!elt) {
                return;
            }

            var snap = optimisticSnapshots.get(elt);
            if (snap) {
                // Roll back on anything that isn't an explicit success. htmx sets detail.successful only
                // on a completed HTTP response (2xx true / non-2xx false); network error, timeout, and
                // abort fire afterRequest with successful === undefined — so fail closed with !== true to
                // restore the target's own pre-request server-rendered HTML (trusted snapshot).
                if (detail.successful !== true) {
                    snap.target.innerHTML = snap.html;
                    // Re-activate htmx behaviors on the restored nodes.
                    // @ts-ignore
                    if (window.htmx && typeof window.htmx.process === 'function') {
                        window.htmx.process(snap.target);
                    }
                }
                optimisticSnapshots.delete(elt);
            }

            if (elt.classList) {
                elt.classList.remove('swap-pending');
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
        document.body.addEventListener('showToast', function(event) {
            // @ts-ignore
            var detail = event.detail || {};
            showToast(detail.message, detail.type || 'info');
        });

        // Initialize Validation Error Listeners
        document.body.addEventListener('validationFailed', function(event) {
            // @ts-ignore
            var errors = event.detail;
            console.debug('[Swap.Htmx] Validation failed:', errors);
            handleValidationErrors(errors);
        });

        // Initialize SSE Logic
        initializeSse();

        // Initialize OOB version guards (data-swap-seq)
        initializeOobGuards();

        // Initialize optimistic UI rollback (data-swap-optimistic)
        initializeOptimistic();

        console.debug('[Swap.Htmx] Client library initialized');
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Global API
    // @ts-ignore
    window.Swap = {
        showToast: showToast,
        handleValidationErrors: handleValidationErrors
    };

})();
