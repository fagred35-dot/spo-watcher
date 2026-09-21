// Автозаполнение формы входа.
// Значения приходят из C# через window.__spoCreds (объект { login, password }).
// Возвращает строку-статус.
(function () {
  try {
    var loginEl = document.getElementById('login');
    var passEl = document.getElementById('password');
    var btn = document.getElementById('loginButton');
    var form = document.querySelector('form[name="formAuth"]');

    if (!loginEl && !passEl) return 'no_form';
    if (!loginEl || !passEl) return 'no_elements';

    var creds = window.__spoCreds;
    if (!creds || !creds.login || !creds.password) return 'no_creds';

    var hasAngular = !!(window.angular && window.angular.element);

    // Установка значения с прогоном digest AngularJS.
    function setVal(el, v) {
      el.focus();
      el.value = v;
      el.dispatchEvent(new Event('input', { bubbles: true }));
      el.dispatchEvent(new Event('change', { bubbles: true }));
      if (hasAngular) {
        try { window.angular.element(el).triggerHandler('input'); } catch (e) {}
        try { window.angular.element(el).triggerHandler('change'); } catch (e) {}
      }
    }

    setVal(loginEl, creds.login);
    setVal(passEl, creds.password);

    try { delete window.__spoCreds; } catch (e) {}

    // Даём Angular время прогнать digest, потом запускаем отправку.
    setTimeout(function () {
      // Прогоняем digest принудительно.
      if (hasAngular && form) {
        try {
          var scope = window.angular.element(form).scope();
          if (scope && !scope.$root.$$phase) scope.$apply();
        } catch (e) { console.warn('[spo-watcher] $apply:', e); }
      }

      // Способ 1: прямой вызов authenticate через scope (AngularJS-специфично).
      if (hasAngular && form) {
        try {
          var sc = window.angular.element(form).scope();
          if (sc && typeof sc.authenticate === 'function') {
            sc.$apply(function () { sc.authenticate(sc.formAuth); });
            console.log('[spo-watcher] отправил через scope.authenticate');
            return;
          }
        } catch (e) { console.warn('[spo-watcher] scope.authenticate:', e); }
      }

      // Способ 2: submit формы.
      if (form) {
        try {
          if (typeof form.requestSubmit === 'function') form.requestSubmit();
          else form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
          console.log('[spo-watcher] отправил через form.submit');
          return;
        } catch (e) { console.warn('[spo-watcher] submit:', e); }
      }

      // Способ 3: клик по кнопке.
      if (btn) {
        try { btn.click(); console.log('[spo-watcher] отправил через btn.click'); return; }
        catch (e) { console.warn('[spo-watcher] click:', e); }
      }

      // Способ 4: Enter в поле пароля.
      try {
        passEl.focus();
        ['keydown', 'keypress', 'keyup'].forEach(function (t) {
          passEl.dispatchEvent(new KeyboardEvent(t, { bubbles: true, key: 'Enter', keyCode: 13, which: 13 }));
        });
        console.log('[spo-watcher] отправил через Enter');
      } catch (e) { console.warn('[spo-watcher] enter:', e); }
    }, 500);

    return 'ok';
  } catch (e) {
    return 'error: ' + (e && e.message ? e.message : String(e));
  }
})();
