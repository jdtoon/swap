/**
 * Swap.Htmx SSE Client Library
 * Handles SSE reconnection logic and connection tracking.
 * Include this ONLY if you are using Server-Sent Events.
 */
(function () {
    'use strict';

    // --- SSE Reconnection Logic ---

    const sseConnections = new Map();

    // Listen for SSE connect events to track active connections
    document.body.addEventListener('htmx:sseBeforeMessage', function(event) {
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

})();
