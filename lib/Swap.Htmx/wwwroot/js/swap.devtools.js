/**
 * Swap.Htmx DevTools - Development Mode Diagnostics
 * 
 * This script provides enhanced debugging capabilities for Swap.Htmx applications:
 * - Event timeline logging
 * - State inspection
 * - OOB swap tracking
 * - Visual event flow
 * 
 * Include this script AFTER swap.client.js in development environments.
 * 
 * @module SwapDevTools
 */
(function () {
    'use strict';

    // ==========================================
    // Configuration
    // ==========================================

    const config = {
        logEvents: true,
        logOobSwaps: true,
        logStateChanges: true,
        showTimeline: false,
        enablePanel: false,
        maxTimelineEntries: 100
    };

    // ==========================================
    // Event Timeline
    // ==========================================

    /** @type {Array<{time: Date, type: string, name: string, detail: any}>} */
    const timeline = [];

    /**
     * Adds an entry to the event timeline.
     * @param {string} type - Event type (trigger, swap, oob, state)
     * @param {string} name - Event or element name
     * @param {any} detail - Additional details
     */
    function addTimelineEntry(type, name, detail) {
        const entry = {
            time: new Date(),
            type: type,
            name: name,
            detail: detail
        };

        timeline.push(entry);

        // Keep timeline bounded
        if (timeline.length > config.maxTimelineEntries) {
            timeline.shift();
        }

        // Update panel if visible
        if (config.enablePanel) {
            updateTimelinePanel();
        }
    }

    // ==========================================
    // Console Logging
    // ==========================================

    const styles = {
        event: 'color: #4CAF50; font-weight: bold',
        trigger: 'color: #2196F3; font-weight: bold',
        oob: 'color: #FF9800; font-weight: bold',
        state: 'color: #9C27B0; font-weight: bold',
        error: 'color: #f44336; font-weight: bold',
        warning: 'color: #FF9800',
        info: 'color: #2196F3',
        dim: 'color: #999'
    };

    /**
     * Logs an event with styling.
     * @param {string} style - Style key
     * @param {string} prefix - Log prefix
     * @param {string} message - Log message
     * @param {any} [data] - Optional data to log
     */
    function log(style, prefix, message, data) {
        const timestamp = new Date().toISOString().split('T')[1].slice(0, -1);
        
        if (data !== undefined) {
            console.log(`%c[${timestamp}] %c${prefix}%c ${message}`, styles.dim, styles[style], 'color: inherit', data);
        } else {
            console.log(`%c[${timestamp}] %c${prefix}%c ${message}`, styles.dim, styles[style], 'color: inherit');
        }
    }

    // ==========================================
    // HTMX Event Listeners
    // ==========================================

    /**
     * Initializes all HTMX event listeners for debugging.
     */
    function initializeEventListeners() {
        // Track all HX-Trigger events
        document.body.addEventListener('htmx:trigger', function(event) {
            if (!config.logEvents) return;
            
            const triggerHeader = event.detail?.xhr?.getResponseHeader('HX-Trigger');
            if (triggerHeader) {
                try {
                    const triggers = JSON.parse(triggerHeader);
                    Object.entries(triggers).forEach(([eventName, payload]) => {
                        log('trigger', '→ TRIGGER', eventName, payload);
                        addTimelineEntry('trigger', eventName, payload);
                    });
                } catch (e) {
                    // Single event without JSON
                    log('trigger', '→ TRIGGER', triggerHeader);
                    addTimelineEntry('trigger', triggerHeader, null);
                }
            }
        });

        // Track custom events (from HX-Trigger)
        const knownEvents = new Set();
        
        // Listen for any custom event on body
        const originalDispatchEvent = EventTarget.prototype.dispatchEvent;
        EventTarget.prototype.dispatchEvent = function(event) {
            // Only intercept CustomEvents on body that look like Swap events
            if (this === document.body && 
                event instanceof CustomEvent && 
                event.type.includes('.')) {
                
                if (!knownEvents.has(event.type)) {
                    knownEvents.add(event.type);
                }
                
                if (config.logEvents) {
                    log('event', '⚡ EVENT', event.type, event.detail);
                    addTimelineEntry('event', event.type, event.detail);
                }
            }
            return originalDispatchEvent.call(this, event);
        };

        // Track OOB swaps
        document.body.addEventListener('htmx:oobBeforeSwap', function(event) {
            if (!config.logOobSwaps) return;
            
            const fragment = event.detail?.fragment;
            if (fragment) {
                const oobElements = fragment.querySelectorAll('[hx-swap-oob], [data-hx-swap-oob]');
                oobElements.forEach(el => {
                    const targetId = el.id;
                    const swapMode = el.getAttribute('hx-swap-oob') || el.getAttribute('data-hx-swap-oob');
                    log('oob', '↔ OOB', `#${targetId} (${swapMode})`, el.outerHTML.slice(0, 100) + '...');
                    addTimelineEntry('oob', targetId, { swapMode, preview: el.outerHTML.slice(0, 200) });
                });
            }
        });

        // Track OOB errors (target not found)
        document.body.addEventListener('htmx:oobErrorNoTarget', function(event) {
            const content = event.detail?.content;
            // AlsoUpdateIfExists marks a swap as intentionally conditional (data-swap-if-exists); a
            // missing target is expected there, so log it as a skip and don't raise the warning.
            const intentional = content && typeof content.getAttribute === 'function' &&
                content.getAttribute('data-swap-if-exists') !== null;
            if (intentional) {
                log('oob', '⤳ OOB skipped', 'Conditional target absent (data-swap-if-exists)', content);
                return;
            }

            log('error', '❌ OOB ERROR', 'Target not found', content);
            addTimelineEntry('oob-error', 'missing-target', content);

            console.warn('[Swap.Htmx] OOB swap failed: Target element not found in DOM. ' +
                'Check that the element ID matches and the element exists on this page.');
        });

        // Track regular swaps
        document.body.addEventListener('htmx:beforeSwap', function(event) {
            if (!config.logOobSwaps) return;
            
            const target = event.detail?.target;
            if (target) {
                const targetId = target.id || target.tagName;
                log('oob', '↓ SWAP', `#${targetId}`);
            }
        });

        // Track afterSwap for timing
        document.body.addEventListener('htmx:afterSwap', function(event) {
            if (!config.logOobSwaps) return;
            
            const target = event.detail?.target;
            if (target) {
                const targetId = target.id || target.tagName;
                log('info', '✓ SWAPPED', `#${targetId}`);
                addTimelineEntry('swap', targetId, { success: true });
            }
        });

        // Track requests
        document.body.addEventListener('htmx:beforeRequest', function(event) {
            const elt = event.detail?.elt;
            const path = event.detail?.path;
            
            log('info', '→ REQUEST', path, { 
                method: event.detail?.verb,
                trigger: elt?.id || elt?.tagName 
            });
        });

        document.body.addEventListener('htmx:afterRequest', function(event) {
            const path = event.detail?.pathInfo?.requestPath;
            const status = event.detail?.xhr?.status;
            
            if (status >= 400) {
                log('error', '← RESPONSE', `${path} (${status})`);
            } else {
                log('info', '← RESPONSE', `${path} (${status})`);
            }
        });

        console.log('%c[Swap.Htmx DevTools] %cInitialized - Event logging enabled', 
            'color: #4CAF50; font-weight: bold', 'color: inherit');
    }

    // ==========================================
    // State Inspector
    // ==========================================

    /**
     * Dumps all state containers to console.
     * @returns {Object} Current state object
     */
    function dumpState() {
        const stateContainers = document.querySelectorAll('[id$="-state"]');
        const state = {};

        stateContainers.forEach(container => {
            const containerId = container.id || 'unnamed';
            state[containerId] = {};

            container.querySelectorAll('input[type="hidden"]').forEach(input => {
                state[containerId][input.name || input.id] = input.value;
            });
        });

        console.group('%c[Swap.Htmx] Current State', styles.state);
        Object.entries(state).forEach(([containerId, fields]) => {
            console.log(`%c${containerId}:`, 'font-weight: bold', fields);
        });
        console.groupEnd();

        return state;
    }

    /**
     * Watches for state changes in a container.
     * @param {string} containerId - Container element ID
     */
    function watchState(containerId) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn(`[Swap.Htmx] State container #${containerId} not found`);
            return;
        }

        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes' && mutation.attributeName === 'value') {
                    const input = mutation.target;
                    log('state', '📝 STATE', `${input.name || input.id} = "${input.value}"`);
                    addTimelineEntry('state', input.name || input.id, input.value);
                }
            });
        });

        container.querySelectorAll('input[type="hidden"]').forEach(input => {
            observer.observe(input, { attributes: true, attributeFilter: ['value'] });
            
            // Also intercept programmatic value changes
            const descriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');
            Object.defineProperty(input, 'value', {
                get: function() {
                    return descriptor.get.call(this);
                },
                set: function(newValue) {
                    const oldValue = descriptor.get.call(this);
                    descriptor.set.call(this, newValue);
                    if (oldValue !== newValue && config.logStateChanges) {
                        log('state', '📝 STATE', `${this.name || this.id}: "${oldValue}" → "${newValue}"`);
                        addTimelineEntry('state', this.name || this.id, { old: oldValue, new: newValue });
                    }
                }
            });
        });

        console.log(`[Swap.Htmx] Watching state container #${containerId}`);
    }

    // ==========================================
    // DevTools Panel
    // ==========================================

    let panelElement = null;

    /**
     * Creates the DevTools panel overlay.
     */
    function createPanel() {
        if (panelElement) return;

        panelElement = document.createElement('div');
        panelElement.id = 'swap-devtools-panel';
        panelElement.innerHTML = `
            <style>
                #swap-devtools-panel {
                    position: fixed;
                    bottom: 0;
                    right: 0;
                    width: 400px;
                    max-height: 300px;
                    background: #1e1e1e;
                    color: #d4d4d4;
                    font-family: 'Consolas', 'Monaco', monospace;
                    font-size: 12px;
                    border-top-left-radius: 8px;
                    box-shadow: -2px -2px 10px rgba(0,0,0,0.3);
                    z-index: 99999;
                    overflow: hidden;
                    display: flex;
                    flex-direction: column;
                }
                #swap-devtools-panel .header {
                    background: #333;
                    padding: 8px 12px;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    cursor: move;
                }
                #swap-devtools-panel .header h3 {
                    margin: 0;
                    font-size: 12px;
                    color: #4CAF50;
                }
                #swap-devtools-panel .tabs {
                    display: flex;
                    background: #2d2d2d;
                }
                #swap-devtools-panel .tab {
                    padding: 6px 12px;
                    cursor: pointer;
                    border: none;
                    background: transparent;
                    color: #888;
                    font-size: 11px;
                }
                #swap-devtools-panel .tab.active {
                    color: #fff;
                    background: #1e1e1e;
                }
                #swap-devtools-panel .content {
                    flex: 1;
                    overflow-y: auto;
                    padding: 8px;
                }
                #swap-devtools-panel .timeline-entry {
                    padding: 4px 8px;
                    border-left: 3px solid #444;
                    margin-bottom: 4px;
                    font-size: 11px;
                }
                #swap-devtools-panel .timeline-entry.trigger { border-color: #2196F3; }
                #swap-devtools-panel .timeline-entry.event { border-color: #4CAF50; }
                #swap-devtools-panel .timeline-entry.oob { border-color: #FF9800; }
                #swap-devtools-panel .timeline-entry.state { border-color: #9C27B0; }
                #swap-devtools-panel .timeline-entry.error { border-color: #f44336; background: #3d1a1a; }
                #swap-devtools-panel .time { color: #666; margin-right: 8px; }
                #swap-devtools-panel .close-btn {
                    background: none;
                    border: none;
                    color: #888;
                    cursor: pointer;
                    font-size: 16px;
                }
                #swap-devtools-panel .close-btn:hover { color: #fff; }
            </style>
            <div class="header">
                <h3>🔧 Swap DevTools</h3>
                <button class="close-btn" onclick="Swap.DevTools.hidePanel()">×</button>
            </div>
            <div class="tabs">
                <button class="tab active" data-tab="timeline">Timeline</button>
                <button class="tab" data-tab="state">State</button>
                <button class="tab" data-tab="events">Events</button>
            </div>
            <div class="content" id="swap-devtools-content">
                <div id="swap-devtools-timeline"></div>
            </div>
        `;

        document.body.appendChild(panelElement);

        // Tab switching
        panelElement.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                panelElement.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                // Store active tab so it persists
                panelElement.dataset.activeTab = tab.dataset.tab;
                updatePanelContent(tab.dataset.tab);
            });
        });

        // Set up auto-refresh interval for live updates
        setInterval(() => {
            if (config.enablePanel && panelElement && panelElement.style.display !== 'none') {
                const activeTab = panelElement.dataset.activeTab || 'timeline';
                updatePanelContent(activeTab);
            }
        }, 500);

        updateTimelinePanel();
    }

    /**
     * Updates the panel content based on active tab.
     * @param {string} tab - Tab name
     */
    function updatePanelContent(tab) {
        const content = document.getElementById('swap-devtools-content');
        if (!content) return;

        switch (tab) {
            case 'timeline':
                // Ensure the timeline container exists before updating
                if (!document.getElementById('swap-devtools-timeline')) {
                    content.innerHTML = '<div id="swap-devtools-timeline"></div>';
                }
                updateTimelinePanel();
                break;
            case 'state':
                const state = {};
                
                // Find all potential state containers
                const stateContainers = document.querySelectorAll(
                    '[id$="-state"], [id$="-container"], #state-container, form'
                );
                
                stateContainers.forEach(container => {
                    const containerId = container.id || container.getAttribute('name') || 'unnamed-' + Math.random().toString(36).substr(2, 4);
                    const inputs = container.querySelectorAll('input[type="hidden"], input[name], select[name]');
                    
                    if (inputs.length > 0) {
                        state[containerId] = {};
                        inputs.forEach(input => {
                            const name = input.name || input.id;
                            if (name && !name.startsWith('__')) {
                                state[containerId][name] = input.value;
                            }
                        });
                        // Only show if there's actual state
                        if (Object.keys(state[containerId]).length === 0) {
                            delete state[containerId];
                        }
                    }
                });
                
                if (Object.keys(state).length === 0) {
                    content.innerHTML = '<div style="padding:8px;color:#888;">No state containers found. Look for elements with id ending in "-state" (e.g., product-filter-state).</div>';
                } else {
                    content.innerHTML = `<pre style="margin:0;white-space:pre-wrap;font-size:11px;">${JSON.stringify(state, null, 2)}</pre>`;
                }
                break;
            case 'events':
                const uniqueEvents = [...new Set(timeline.filter(e => e.type === 'event' || e.type === 'trigger').map(e => e.name))];
                if (uniqueEvents.length === 0) {
                    content.innerHTML = '<div style="padding:8px;color:#888;">No events captured yet. Interact with the page to see triggered events.</div>';
                } else {
                    content.innerHTML = uniqueEvents.map(e => `<div style="padding:4px 8px;border-bottom:1px solid #333;font-size:11px;">${e}</div>`).join('');
                }
                break;
        }
    }

    /**
     * Updates the timeline panel with recent entries.
     */
    function updateTimelinePanel() {
        const timelineEl = document.getElementById('swap-devtools-timeline');
        if (!timelineEl) return;

        const recentEntries = timeline.slice(-20).reverse();
        timelineEl.innerHTML = recentEntries.map(entry => {
            const time = entry.time.toISOString().split('T')[1].slice(0, 12);
            return `
                <div class="timeline-entry ${entry.type}">
                    <span class="time">${time}</span>
                    <strong>${entry.name}</strong>
                    ${entry.detail ? `<div style="color:#888;font-size:10px;margin-top:2px;">${JSON.stringify(entry.detail).slice(0, 80)}</div>` : ''}
                </div>
            `;
        }).join('');
    }

    /**
     * Shows the DevTools panel.
     */
    function showPanel() {
        config.enablePanel = true;
        if (!panelElement) {
            createPanel();
        }
        panelElement.style.display = 'flex';
    }

    /**
     * Hides the DevTools panel.
     */
    function hidePanel() {
        config.enablePanel = false;
        if (panelElement) {
            panelElement.style.display = 'none';
        }
    }

    // ==========================================
    // Public API
    // ==========================================

    /**
     * Gets the event timeline.
     * @returns {Array} Timeline entries
     */
    function getTimeline() {
        return [...timeline];
    }

    /**
     * Clears the event timeline.
     */
    function clearTimeline() {
        timeline.length = 0;
        console.log('[Swap.Htmx] Timeline cleared');
        if (config.enablePanel) {
            updateTimelinePanel();
        }
    }

    /**
     * Configures DevTools options.
     * @param {Partial<typeof config>} options - Configuration options
     */
    function configure(options) {
        Object.assign(config, options);
        console.log('[Swap.Htmx DevTools] Configuration updated:', config);
    }

    // ==========================================
    // Initialization
    // ==========================================

    function initialize() {
        initializeEventListeners();

        // Auto-watch any state containers
        document.querySelectorAll('[id$="-state"]').forEach(container => {
            if (container.id) {
                watchState(container.id);
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Extend global Swap object
    window.Swap = window.Swap || {};
    window.Swap.DevTools = {
        configure: configure,
        dumpState: dumpState,
        watchState: watchState,
        getTimeline: getTimeline,
        clearTimeline: clearTimeline,
        showPanel: showPanel,
        hidePanel: hidePanel,
        timeline: timeline
    };

    // Log available commands
    console.log('%c[Swap.Htmx DevTools] %cAvailable commands:', 
        'color: #4CAF50; font-weight: bold', 'color: inherit');
    console.log('  Swap.DevTools.dumpState()     - Show current state');
    console.log('  Swap.DevTools.showPanel()     - Show DevTools panel');
    console.log('  Swap.DevTools.getTimeline()   - Get event timeline');
    console.log('  Swap.DevTools.clearTimeline() - Clear timeline');
    console.log('  Swap.DevTools.watchState(id)  - Watch a state container');

})();
