// SSE Reconnection Logic for TaskFlow
// Handles automatic reconnection when SSE connection drops

(function() {
    'use strict';

    // Track SSE connections
    const sseConnections = new Map();
    
    function updateConnectionStatus(status, color) {
        const statusEl = document.getElementById('sse-status');
        if (statusEl) {
            statusEl.textContent = status;
            statusEl.style.color = color;
        }
    }

    // Listen for SSE connect events
    document.body.addEventListener('htmx:sseBeforeMessage', function(event) {
        const element = event.target;
        const url = element.getAttribute('sse-connect');
        
        if (url && !sseConnections.has(url)) {
            console.log('[TaskFlow SSE] Connected to:', url);
            sseConnections.set(url, {
                element: element,
                reconnectAttempts: 0
            });
        }
    });

    // Handle SSE errors and reconnection
    document.body.addEventListener('htmx:sseError', function(event) {
        const element = event.target;
        const url = element.getAttribute('sse-connect');
        
        updateConnectionStatus('[ERROR] SSE Disconnected', '#ef4444');
        
        if (url) {
            const connection = sseConnections.get(url);
            if (connection) {
                connection.reconnectAttempts++;
                
                // Exponential backoff: 1s, 2s, 4s, 8s, max 30s
                const delay = Math.min(Math.pow(2, connection.reconnectAttempts - 1) * 1000, 30000);
                
                console.log(`[TaskFlow SSE] Connection lost to ${url}. Reconnecting in ${delay}ms (attempt ${connection.reconnectAttempts})...`);
                
                setTimeout(function() {
                    // Trigger HTMX to reconnect
                    htmx.trigger(element, 'sse:reconnect');
                }, delay);
            }
        }
    });

    // Reset reconnection counter on successful connection
    document.body.addEventListener('htmx:sseOpen', function(event) {
        const element = event.target;
        const url = element.getAttribute('sse-connect');
        
        updateConnectionStatus('[CONNECTED] SSE Connected', '#10b981');
        
        if (url) {
            const connection = sseConnections.get(url);
            if (connection && connection.reconnectAttempts > 0) {
                console.log('[TaskFlow SSE] Reconnected successfully to:', url);
                connection.reconnectAttempts = 0;
            }
        }
    });

    // Handle specific SSE events for dashboard updates
    document.body.addEventListener('htmx:sseMessage', function(event) {
        const eventName = event.detail.type;
        
        switch (eventName) {
            case 'heartbeat':
                // Keep connection alive
                break;
                
            case 'stats-update':
                // Dashboard stats changed
                htmx.trigger('#dashboard-stats', 'refresh');
                break;
                
            case 'notification-count':
                // New notification arrived
                const count = parseInt(event.detail.data);
                if (count > 0) {
                    htmx.trigger('#notification-bell', 'refresh');
                }
                break;
                
            case 'task-update':
                // Task changed, refresh Kanban board
                htmx.trigger('.kanban-board', 'refresh');
                break;
                
            default:
                console.log('[TaskFlow SSE] Unknown event:', eventName);
        }
    });

    console.log('[TaskFlow SSE] Reconnection handler initialized');
})();
