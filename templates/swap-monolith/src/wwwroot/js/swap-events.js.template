/* HTMX-native subscription scanner for ui.* events */
(function (global) {
  var REGISTRY = new Set();
  function toHeaderValue() { return REGISTRY.size ? Array.from(REGISTRY).join(',') : ''; }
  function extractUiTriggers(s) {
    if (!s || typeof s !== 'string') return [];
    var out = [];
    s.split(',').forEach(function (p) {
      var t = p.trim(); if (!t) return;
      var idx = t.indexOf(' from:'); if (idx >= 0) t = t.substring(0, idx);
      var first = t.split(/\s+/)[0]; if (first && first.indexOf('ui.') === 0) out.push(first);
    }); return out;
  }
  function rescan(root) {
    try { var scope = root || global.document; if (!scope) return;
      scope.querySelectorAll('[hx-trigger]').forEach(function (el) {
        extractUiTriggers(el.getAttribute('hx-trigger')).forEach(function (e) { REGISTRY.add(e); });
      });
    } catch (_) {}
  }
  function ensure() {
    if (!global.htmx || !global.htmx.on || ensure._h) return; ensure._h = true;
    global.htmx.on('htmx:configRequest', function (evt) {
      var hv = toHeaderValue(); if (!hv) return;
      evt.detail.headers = evt.detail.headers || {};
      var ex = evt.detail.headers['X-Swap-Events'];
      if (ex) {
        var set = new Set((ex + ',' + hv).split(',').map(function (x){return x.trim();}).filter(Boolean));
        evt.detail.headers['X-Swap-Events'] = Array.from(set).join(',');
      } else { evt.detail.headers['X-Swap-Events'] = hv; }
    });
    global.htmx.on('htmx:afterSwap', function () { rescan(global.document); });
  }
  if (global.document) {
    if (global.htmx) { ensure(); rescan(global.document); }
    else { global.document.addEventListener('DOMContentLoaded', function(){ ensure(); rescan(global.document); }, { once: true }); }
  }
  // Forward common UI events to showToast helper for visual feedback
  global.document && global.document.body && global.document.body.addEventListener('ui.toast.success', function (evt) {
    var detail = (evt && evt.detail && (evt.detail.value || evt.detail)) || {};
    var payload = { type: 'success', message: detail.message || detail.text || 'Operation completed', position: detail.position };
    global.document.body.dispatchEvent(new CustomEvent('showToast', { detail: payload }));
  });
})(window);
