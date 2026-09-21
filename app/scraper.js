// Скрапер: собирает снимок текущей страницы сайта.
// Выполняется в WebView2 через ExecuteScriptAsync, возвращает JSON-строку.
(function () {
  'use strict';

  function txt(el) {
    if (!el) return '';
    return (el.textContent || '').replace(/\s+/g, ' ').trim();
  }

  function monthToNum(name) {
    var map = { 'январ':1,'феврал':2,'март':3,'апрел':4,'ма':5,'июн':6,'июл':7,'август':8,'сентябр':9,'октябр':10,'ноябр':11,'декабр':12 };
    var n = (name || '').toLowerCase();
    for (var k in map) if (n.indexOf(k) === 0) return map[k];
    return 0;
  }

  function scrapeGrades() {
    var rows = document.querySelectorAll('tbody[x-ng-repeat*="rowData in reportRows"]');
    if (!rows.length) return null;
    var table = rows[0].closest('table');
    if (!table) return null;
    var dayThs = table.querySelectorAll('th[x-ng-bind*="moment.format"]');
    var monthTh = table.querySelector('th[x-ng-bind*="monthWithDays.month.name"]');
    var monthName = txt(monthTh);
    var h3 = document.querySelector('.print-content-wrapper .heading h3');
    var year = '';
    if (h3) {
      var m = (h3.textContent || '').match(/Дата:\s*\d{2}\.\d{2}\.(\d{4})/);
      if (m) year = m[1];
    }
    var monthNum = monthToNum(monthName);
    var dayNums = [];
    dayThs.forEach(function(th){ dayNums.push(txt(th)); });
    var out = [];
    rows.forEach(function(tbody){
      var nameTd = tbody.querySelector('td.big.bold');
      var subject = txt(nameTd);
      if (!subject) return;
      var dayTds = tbody.querySelectorAll('td[x-ng-bind="day.value"]');
      dayTds.forEach(function(td, i){
        var value = txt(td);
        if (!value) return;
        var dayNum = dayNums[i] || String(i+1);
        var dd = String(dayNum).padStart(2,'0');
        var mm = monthNum ? String(monthNum).padStart(2,'0') : '00';
        var yy = year || '0000';
        out.push({ subject: subject, date: dd + '.' + mm + '.' + yy, value: value });
      });
    });
    return out;
  }

  function parseRuDate(s) {
    var months = { 'янв':1,'фев':2,'мар':3,'апр':4,'мая':5,'май':5,'июн':6,'июл':7,'авг':8,'сен':9,'окт':10,'ноя':11,'дек':12 };
    var m = (s || '').match(/(\d{1,2})\s+([а-яё]+)\.?\s+(\d{4})/i);
    if (!m) return '';
    var day = String(m[1]).padStart(2,'0');
    var monName = m[2].toLowerCase().slice(0,3);
    var mon = months[monName] || 0;
    var year = m[3];
    if (!mon) return '';
    return year + '-' + String(mon).padStart(2,'0') + '-' + day;
  }

  function scrapeLessons() {
    var days = document.querySelectorAll('dl[x-ng-repeat*="day in week"]');
    if (!days.length) return null;
    var out = [];
    days.forEach(function(dl){
      var dateStr = txt(dl.querySelector('.long-date'));
      var isoDate = parseRuDate(dateStr);
      var lessons = dl.querySelectorAll('.lesson');
      lessons.forEach(function(l){
        var summary = l.querySelector('.summary');
        if (!summary) return;
        var subject = txt(summary.querySelector('big'));
        var teacherEl = l.querySelector('nobr.teacher');
        var teacher = teacherEl ? (teacherEl.getAttribute('title') || txt(teacherEl)) : '';
        var room = txt(l.querySelector('nobr.classroom'));
        var start = txt(l.querySelector('data.time .start'));
        var end = txt(l.querySelector('data.time .end'));
        if (!subject) return;
        out.push({ date: isoDate, time_start: start, time_end: end, subject: subject, teacher: teacher, room: room });
      });
    });
    return out;
  }

  function buildSnapshot() {
    var snap = { grades: {}, lessons: {} };
    var g = scrapeGrades();
    if (g) g.forEach(function(item){ snap.grades[item.subject + '|' + item.date] = item.value; });
    var l = scrapeLessons();
    if (l) l.forEach(function(item){
      snap.lessons[item.date + '|' + item.time_start] = {
        subject: item.subject, teacher: item.teacher, room: item.room, time_end: item.time_end
      };
    });
    // WebView2 сам сериализует результат в JSON. Возвращаем объект, не строку.
    return snap;
  }

  return buildSnapshot();
})();
