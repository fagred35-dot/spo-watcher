// Маскировка ФИО студента на сайте «Сетевой город» (spo.rso23.ru).
// Инжектится в WebView2 при каждой навигации; MutationObserver ловит
// перерисовки AngularJS (шапка/меню/отчёт появляются после входа).
// Тексты замены — константы ниже.
(function () {
  'use strict';

  var NAME_TEXT  = 'Скрыто';   // вместо «Бабурченков Иван» в заголовке страницы
  var MENU_TEXT  = 'Скрыто';   // вместо «Иван Бабурченков» в меню справа
  var REPORT_TEXT = 'Скрыто';  // подставляется в «Студент: <...> (гр. …)»
  var DEBOUNCE_MS = 60;

  function setText(el, value) {
    if (el && el.textContent !== value) el.textContent = value;
  }

  function applyMask() {
    // 1) Меню пользователя справа: «Иван Бабурченков»
    document.querySelectorAll('nav.main-menu .usermenu .item-title .ng-binding')
      .forEach(function (el) { setText(el, MENU_TEXT); });

    // 2) Заголовок страницы: «Бабурченков Иван»
    document.querySelectorAll('.headline h1.ng-binding, .headline h1')
      .forEach(function (el) { setText(el, NAME_TEXT); });

    // 3) Шапка отчёта: «Студент: Бабурченков Иван (гр. 25-С4)  Дата: 21.09.2026»
    //    Меняем только ФИО; «(гр. …)» и «Дата: …» сохраняем — «Дата» читает scraper.js.
    document.querySelectorAll('.print-content-wrapper .heading h3, h3.ng-binding')
      .forEach(function (el) {
        var t = el.textContent || '';
        var m = t.match(/^([\s\S]*?Студент:\s*)[^(]+?(\s*\(гр\.[\s\S]*)$/);
        if (m) setText(el, m[1] + REPORT_TEXT + m[2]);
      });
  }

  function start() {
    applyMask();

    if (window.__spoNameMask) window.__spoNameMask.disconnect();

    var pending = false;
    var obs = new MutationObserver(function () {
      if (pending) return;
      pending = true;
      setTimeout(function () { pending = false; applyMask(); }, DEBOUNCE_MS);
    });
    obs.observe(document.documentElement, { childList: true, subtree: true, characterData: true });
    window.__spoNameMask = obs;
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start, { once: true });
  } else {
    start();
  }
})();
