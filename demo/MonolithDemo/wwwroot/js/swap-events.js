/*
 Swap Events client helper
 - Tracks active client-side event subscriptions
 - Adds X-Swap-Events header to HTMX requests via htmx:configRequest
 Usage:
   <script src="/js/swap-events.js"></script>
   <script>
     SwapEvents.activate('ui.refreshList');
     // later: SwapEvents.deactivate('ui.refreshList');
   </script>
*/
(function (global) {
  var REGISTRY = new Set();

  function toHeaderValue() {
    if (REGISTRY.size === 0) return '';
    return Array.from(REGISTRY).join(',');
  }

  function ensureHtmxHook() {
    if (!global.htmx || !global.htmx.on) return;
    // Avoid double-hooking
    if (ensureHtmxHook._hooked) return;
    ensureHtmxHook._hooked = true;

    global.htmx.on('htmx:configRequest', function (evt) {
      try {
        var headerVal = toHeaderValue();
        if (headerVal) {
          evt.detail.headers = evt.detail.headers || {};
          // Do not overwrite if user explicitly set it; merge unique values instead
          var existing = evt.detail.headers['X-Swap-Events'];
          if (existing) {
            var parts = (existing + ',' + headerVal)
              .split(',')
              .map(function (s) { return s.trim(); })
              .filter(Boolean);
            var dedup = Array.from(new Set(parts));
            evt.detail.headers['X-Swap-Events'] = dedup.join(',');
          } else {
            evt.detail.headers['X-Swap-Events'] = headerVal;
          }
        }
      } catch (e) {
        // best-effort, never throw
        if (global && global.console && console.debug) {
          console.debug('[SwapEvents] Failed to set X-Swap-Events header', e);
        }
      }
    });
  }

  function activate(name) {
    if (!name || typeof name !== 'string') return;
    REGISTRY.add(name);
  }

  function deactivate(name) {
    if (!name || typeof name !== 'string') return;
    REGISTRY.delete(name);
  }

  function list() { return Array.from(REGISTRY); }
  function clear() { REGISTRY.clear(); }
  function setAll(arr) {
    REGISTRY.clear();
    if (!Array.isArray(arr)) return;
    for (var i = 0; i < arr.length; i++) {
      var s = arr[i];
      if (typeof s === 'string' && s.trim()) REGISTRY.add(s.trim());
    }
  }

  var api = {
    activate: activate,
    deactivate: deactivate,
    set: setAll,
    clear: clear,
    list: list
  };

  global.SwapEvents = global.SwapEvents || api;

  // Hook now if htmx is already loaded, else hook when it becomes available
  if (global.htmx) {
    ensureHtmxHook();
  } else if (global.document) {
    // Try again on DOM ready in case htmx loads late
    global.document.addEventListener('DOMContentLoaded', ensureHtmxHook, { once: true });
  }
})(window);
