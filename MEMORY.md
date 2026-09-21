# СПО-Вотчер — уведомления из spo.rso23.ru в Telegram

## Что это
Windows-exe: открывает сайт в WebView2, автоматически входит, раз в час проверяет новые оценки, ежедневно в 19:00 шлёт расписание на завтра, по кнопке — все оценки или расписание сейчас.

## СТЕК
**C# WinForms + WebView2 + .NET 6** → **Render** (Node.js) → **Telegram Bot**.
Tauri/Rust отвергнут (crates.io недоступен). Vercel заменён на Render (там был server_not_configured, потом переехали).

## URL и репо
- **Сервер**: https://spo-watcher.onrender.com
- **GitHub**: https://github.com/fagred35-dot/spo-watcher (публичный)
- **Бот**: @spo_votcher_bot

## Окружение
- .NET SDK 6.0.428
- Node v22.22.2
- WebView2 Runtime 153.0.4234.48
- gh CLI авторизован как fagred35-dot

## Структура
```
app/
  Program.cs           точка входа
  MainForm.cs          окно, кнопки, 2 таймера, diff, отправка, автологин
  SettingsForm.cs      диалог ввода логина/пароля
  Credentials.cs       DPAPI-хранилище логина/пароля
  Snapshot.cs, Lesson  модель снимка
  Event.cs             модель события
  Diff.cs              сравнение оценок
  Sender.cs            POST + HMAC
  Scraper.cs           обёртка scraper.js (EmbeddedResource)
  AutoLogin.cs         обёртка autologin.js (EmbeddedResource)
  scraper.js           JS-скрапер страниц
  autologin.js         JS-автозаполнение формы входа
  spo-watcher.csproj
  config.json          server_url + secret (в .gitignore)
server/
  index.js             Express-подобный http-сервер (Render)
  package.json
  README.md
```

## Логика приложения
1. При старте открывается сайт, идёт попытка автологина (`NavigationCompleted`).
2. Автологин: `%APPDATA%\spo-watcher\credentials.dat` (DPAPI) → 4 способа отправки формы.
3. Таймер 1 час → `RunGradesCheckAsync()` → diff → новые оценки.
4. Таймер 5 мин → `MaybeSendDailyLessonsAsync()` → в 19:00 шлёт расписание на завтра (type: lessons).
5. Кнопка «Все оценки сейчас» → `SendAllGradesAsync()` → шлёт ВСЕ оценки (type: grades_all).
6. Кнопка «Расписание на завтра» → `SendTomorrowLessonsAsync()`.

## Типы событий (сервер)
- `grade` — новая оценка (subject, date, value)
- `grades_all` — все оценки сразу (subject = «Все оценки на DD.MM.YYYY»)
- `lessons` — расписание (subject = «Расписание на DD.MM.YYYY», value = HTML-текст)
- `startup` — тест связи
- `substitution` — зарезервирован

Формат Telegram: HTML (parse_mode: 'HTML'), эмодзи в превью.

## Селекторы
**Форма входа:** `#login`, `#password`, `#loginButton`, `form[name="formAuth"]`. AngularJS, отправка через `x-ng-submit="authenticate(formAuth)"`.
**Оценки:** `tbody[x-ng-repeat*="rowData in reportRows"]`, `td.big.bold`, `td[x-ng-bind="day.value"]`, `th[x-ng-bind*="moment.format"]`.
**Расписание:** `dl[x-ng-repeat*="day in week"]`, `.long-date`, `.lesson`, `.summary big`, `nobr.teacher[title]`, `nobr.classroom`, `data.time .start/.end`.

## Env Vercel/Render
- `TELEGRAM_BOT_TOKEN`
- `TELEGRAM_CHAT_ID`
- `WEBHOOK_SECRET` = `4f148a9871fd794fb6c3e00e8000e9055aad8f037a49c31b0b490afdb2642a70`

## config.json (рядом с exe)
```json
{
  "server_url": "https://spo-watcher.onrender.com/api/notify",
  "secret": "4f148a9871fd794fb6c3e00e8000e9055aad8f037a49c31b0b490afdb2642a70"
}
```

## Грабли
- **НЕ пихать JS в C# verbatim-строку** — кавычки рвут строку. Только EmbeddedResource.
- **WebView2 сам сериализует результат ExecuteScriptAsync в JSON** — в JS возвращать объект, не JSON.stringify.
- **AngularJS-форма входа** требует прогона digest и/или прямого вызова `scope.authenticate()`. btn.click() недостаточно.
- **`var enc = System.Net.WebUtility.HtmlEncode;`** — не работает, две перегрузки. Локальная функция.
- exe может остаться в памяти после kill — искать `tasklist /FI "IMAGENAME eq spo-watcher.exe"` и `taskkill /F /PID`.
- `write_file` иногда теряется — проверять `stat`/`read_file`.
- `find_tool` НЕ существует.
- `list_dir` вне рабочей папки → EPATHJAIL.
- cmd портит cp866-вывод.
- `start_process` в PowerShell требует `.\` перед exe.

## Безопасность
- Логин/пароль от сайта — DPAPI в `%APPDATA%\spo-watcher\credentials.dat`. Только этот Windows-пользователь, только эта машина.
- HMAC-SHA256 (X-Signature) от raw body.
- `chat_id` жёстко в env сервера.
- Токен бота не в репо, не в exe.

## Открытое / возможные улучшения
- UI exe простоват (панель кнопок + лог). Можно улучшить: статус-бар, иконки, сворачивание в трей.
- Render Free засыпает через 15 минут. Пинговать `/health` через cron-job.org раз в 10 минут.
- Возможный перенос exe на Render — НЕВОЗМОЖЕН: Render это Linux, exe это Windows. Можно разделить логику: сервер + веб-интерфейс, но тогда теряется WebView2 и автологин.
