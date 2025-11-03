/*
 Swap Events client helper (HTMX-native)
 - Parses hx-trigger to discover UI event subscriptions (ui.*)
 - De-duplicates and sends X-Swap-Events in HTMX requests for server-side filtering
 - No custom data-* attributes; HTMX drives refresh via hx-trigger
*/
(function (global) {
  var REGISTRY = new Set();

  function toHeaderValue() {
    if (REGISTRY.size === 0) return '';
    return Array.from(REGISTRY).join(',');
  }

  function extractUiTriggers(triggerValue) {
    // Example: "load, ui.stats.refresh from:body, click .selector"
    if (!triggerValue || typeof triggerValue !== 'string') return [];
    var out = [];
    triggerValue.split(',').forEach(function (part) {
      var s = part.trim();
      if (!s) return;
      // take token before whitespace or before ' from:' segment
      var idx = s.indexOf(' from:');
      if (idx >= 0) s = s.substring(0, idx);
      var first = s.split(/\s+/)[0];
      if (first && first.indexOf('ui.') === 0) out.push(first);
    });
    return out;
  }

  function rescan(root) {
    try {
      var scope = root || (global.document && global.document);
      if (!scope || !scope.querySelectorAll) return;
      var els = scope.querySelectorAll('[hx-trigger]');
      els.forEach(function (el) {
        var triggers = extractUiTriggers(el.getAttribute('hx-trigger'));
        for (var i = 0; i < triggers.length; i++) REGISTRY.add(triggers[i]);
      });
    } catch (_) { /* best-effort */ }
  }

  function ensureHtmxHook() {
    if (!global.htmx || !global.htmx.on) return;
    if (ensureHtmxHook._hooked) return;
    ensureHtmxHook._hooked = true;

    // Add header on all HTMX requests
    global.htmx.on('htmx:configRequest', function (evt) {
      try {
        var headerVal = toHeaderValue();
        if (!headerVal) return;
        evt.detail.headers = evt.detail.headers || {};
        var existing = evt.detail.headers['X-Swap-Events'];
        if (existing) {
          var parts = (existing + ',' + headerVal).split(',').map(function (s) { return s.trim(); }).filter(Boolean);
          var dedup = Array.from(new Set(parts));
          evt.detail.headers['X-Swap-Events'] = dedup.join(',');
        } else {
          evt.detail.headers['X-Swap-Events'] = headerVal;
        }
      } catch (e) {
        if (global && global.console && console.debug) console.debug('[SwapEvents] Failed to set X-Swap-Events header', e);
      }
    });

    // After swaps, new elements may have hx-trigger; rescan
    global.htmx.on('htmx:afterSwap', function () { rescan(global.document); });
  }

  // Init
  if (global.document) {
    if (global.htmx) {
      ensureHtmxHook();
      rescan(global.document);
    } else {
      global.document.addEventListener('DOMContentLoaded', function(){
        ensureHtmxHook();
        rescan(global.document);
      }, { once: true });
    }
  }
})(window);
