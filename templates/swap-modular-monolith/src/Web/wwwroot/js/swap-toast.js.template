/* Toast event handlers for Swap framework */
(function (global) {
  if (!global.document || !global.document.body) return;

  // Handle success toast events
  global.document.body.addEventListener('ui.toast.success', function (evt) {
    var detail = (evt && evt.detail && (evt.detail.value || evt.detail)) || {};
    var payload = { 
      type: 'success', 
      message: detail.message || detail.text || 'Operation completed', 
      duration: detail.duration || 3000,
      position: detail.position || 'top-right'
    };
    global.document.body.dispatchEvent(new CustomEvent('showToast', { detail: payload }));
  });

  // Handle error toast events
  global.document.body.addEventListener('ui.toast.error', function (evt) {
    var detail = (evt && evt.detail && (evt.detail.value || evt.detail)) || {};
    var payload = { 
      type: 'error', 
      message: detail.message || detail.text || 'An error occurred', 
      duration: detail.duration || 5000,
      position: detail.position || 'top-right'
    };
    global.document.body.dispatchEvent(new CustomEvent('showToast', { detail: payload }));
  });

  // Handle warning toast events
  global.document.body.addEventListener('ui.toast.warning', function (evt) {
    var detail = (evt && evt.detail && (evt.detail.value || evt.detail)) || {};
    var payload = { 
      type: 'warning', 
      message: detail.message || detail.text || 'Warning', 
      duration: detail.duration || 4000,
      position: detail.position || 'top-right'
    };
    global.document.body.dispatchEvent(new CustomEvent('showToast', { detail: payload }));
  });

  // Handle info toast events
  global.document.body.addEventListener('ui.toast.info', function (evt) {
    var detail = (evt && evt.detail && (evt.detail.value || evt.detail)) || {};
    var payload = { 
      type: 'info', 
      message: detail.message || detail.text || 'Information', 
      duration: detail.duration || 3000,
      position: detail.position || 'top-right'
    };
    global.document.body.dispatchEvent(new CustomEvent('showToast', { detail: payload }));
  });
})(window);
