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
  MainForm.cs          окно, кнопки, 2 таймера, diff, отправка, автологин, CSS-инъекция
  SettingsForm.cs      диалог ввода логина/пароля (тёмная тема)
  Credentials.cs       DPAPI-хранилище логина/пароля
  Theme.cs             палитра тёмной темы приложения
  ModernButton.cs      кастомная кнопка (скругление, hover, accent)
  SiteTheme.cs         обёртка site-dark.css (EmbeddedResource)
  Snapshot.cs, Lesson  модель снимка
  Event.cs             модель события
  Diff.cs              сравнение оценок
  Sender.cs            POST + HMAC
  Scraper.cs           обёртка scraper.js (EmbeddedResource)
  AutoLogin.cs         обёртка autologin.js (EmbeddedResource)
  scraper.js           JS-скрапер страниц
  autologin.js         JS-автозаполнение формы входа
  site-dark.css        тёмная тема сайта (инжектится на каждой навигации)
  spo-watcher.csproj
  config.json          server_url + secret (в .gitignore)
server/
  index.js             Express-подобный http-сервер (Render)
  package.json
  README.md
```

## UI/тема
- Приложение: тёмная тема (Theme.cs, палитра #181a1b / #232629 / #ff9f43 оранжевый акцент).
- Кнопки: ModernButton со скруглением и hover, Accent=true для главных действий.
- Сайт внутри WebView2: `site-dark.css` инжектится на каждом `NavigationCompleted` через `<style id='spo-dark-theme'>`.
- Кнопка «Войти» на сайте НЕ трогается (пользователь попросил) — только остальное перекрашено.

## Логика приложения
1. При старте открывается сайт, идёт попытка автологина (`NavigationCompleted`). На том же хуке инжектится CSS.
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

## UI v0.3.0 (сделано 2026-09-22)
- **Theme.cs**: расширенная палитра (Bg/Surface/Surface2/Surface3, BorderHeavy, AccentHover/AccentPress/AccentSubtle, состояния Ok/Warn/Err с Subtle-вариантами, цвета лога LogInfo/LogSuccess/LogWarning/LogError/LogDebug/LogTimestamp).
- **ModernButton.cs**: варианты стилей Primary/Accent/Secondary/Ghost/Danger, тени для акцентных, совместимость `Accent` property.
- **ModernTextBox.cs**: кастомный TextBox со скруглением, подсветкой фокуса (акцентная рамка), hover-эффектом. Используется в SettingsForm.
- **SettingsForm.cs**: переработан — ModernTextBox, стилизованный чекбокс, информационная панель с рамкой, Ghost/Accent/Secondary кнопки.
- **MainForm.cs**: toolbar 64px с нижней границей; лог RichTextBox с цветными уровнями (DBG/✓/!/✕), бейдж ошибок/предупреждений в шапке лога; статус-бар с круглым индикатором (glow-эффект); тёмное контекстное меню трея (DarkMenuRenderer + DarkColorTable).
- **site-dark.css v0.3.0**: тёмная тема карточек среднего балла (.average .inner/.cell), кнопки сайта (кроме #loginButton), модальные окна, алерты, выделение текста, transition-анимации.
- Логику (RunGradesCheckAsync, SendAllGradesAsync, SendTomorrowLessonsAsync, OnNavigationCompleted, InitAsync, NavigateTo) НЕ трогали.
- Версия 0.3.0.

## ГРАБЛИ: параллельная сессия
- 2026-09-21 в этой папке параллельно работала ДРУГАЯ сессия: перезаписывала `app/MainForm.cs` своей версией (другие поля: `_logTogglePanel`, `_notifyIcon`, `_statusLabel`, заголовок v0.1.0), оставила мусорный файл `$null` (обломок PowerShell с TASKKILL). Проверять `stat` перед правкой: mtime/размер могут не совпасть с только что записанным.

## UI v0.3.1 (сделано 2026-09-22)
- **ГЛАВНАЯ ГРАБЛЯ**: `csproj` имел `<Version>0.2.0</Version>`, а код MainForm был уже v0.3.0 — exe на скрине показывал v0.2.0, а правки UI «не применялись». MainForm берёт версию из AssemblyVersion. Поднял до 0.3.1.
- **Ghost-кнопки выглядели светлыми плашками**: в `ModernButton.OnPaint` был `g.Clear(Parent?.BackColor ?? Theme.Bg)`, а у FlowLayoutPanel `BackColor = Color.Transparent` (ARGB 0x00FFFFFF) → Graphics.Clear заливает белым. Фикс: если `parentBg.A == 0`, брать `Theme.Surface`.
- Текст ghost-кнопок был `Theme.TextDim` (бледный) → сделал `Theme.Text`, hover → Accent.
- **Дашборд `.average`**: усилены селекторы `div.average > div.cell > div.inner` + `background-color/background-image: none`, сам `.average` — прозрачный. Карточки стали тёмными.
- **Активный пункт меню сайта** (синяя плитка «Занятия»): AngularJS вешает `current`/`active` — добавлены правила `nav.main-menu .item.current/.active/.selected` в site-dark.css.
- Сборка/запуск: `cd app && dotnet build -c Release > build.log 2>&1`, затем `start_process .\bin\Release\net6.0-windows\win-x64\spo-watcher.exe` shell cmd.
- `dotnet build | findstr` НЕ работает (cp866-вывод) — писать в build.log и читать `read_lines { tail, encoding: "auto" }`.
- `screenshot { window: "..." }` иногда ловит чужое окно или не успевает — брать по process-имени `"spo-watcher"`, waitMs 2000–4000.

## UI v0.3.2 — чистка (2026-09-22)
- Удалён мёртвый код: `ModernButton.Accent` (legacy-свойство, нигде не использовалось), `Theme.AccentSubtle/OkSubtle/Warn/WarnSubtle/ErrSubtle/InfoSubtle`.
- Кнопка «Главная» получила `Margin = new Padding(0, 0, 4, 0)` — была единственной без правого отступа, прилипала к соседям.
- **Корень проекта очищен**: удалены HTML-дампы сайта (средний бал.html, страница входа.html, текущая успеваемость.html, уроки.html), 12 png-скриншотов, test_notify.js, устаревшие plan.md (описывал Vercel — неактуально) и UI_TASK.md (задача выполнена). Всё в .trash.
- .gitignore: `ui_*.png` → `ui*.png` (ловит и ui-preview.png).
- **В репо НЕ должно быть скриншотов и HTML-дампов** — они содержат ФИО/оценки. В корне оставлен один `ui_v031_final3.png` для справки (не в git? проверить).
- `plan.md` удалён — актуальное состояние только в MEMORY.md.

## Маскировка ФИО (v0.3.2, 2026-09-22)
- Пользователь просил, чтобы на сайте-журнале (внутри WebView2) его имя не светилось.
- Файлы: `app/name-mask.js` (EmbeddedResource) + `app/NameMask.cs` (обёртка, как SiteTheme/Scraper).
- Инжектится в `OnNavigationCompleted` сразу после CSS, ДО автологина.
- Маскирует 3 места: `nav.main-menu .usermenu .item-title .ng-binding` (меню справа), `.headline h1` (заголовок), `.print-content-wrapper .heading h3` (шапка отчёта — только ФИО, «(гр. …)» и «Дата: …» сохраняются, их читает scraper.js).
- Константы в начале name-mask.js: NAME_TEXT / MENU_TEXT / REPORT_TEXT (сейчас все «Скрыто»).
- MutationObserver (debounce 60 мс) ловит перерисовки Angular после входа.
- **ВАЖНО**: `taskkill /F /IM spo-watcher.exe` ПЕРЕД `dotnet build`, иначе ошибка MSB3021 (exe занят). Иногда taskkill + сразу build не успевает — добавить `ping -n 2 127.0.0.1 >nul` между ними.
- `screenshot { window: ... }` часто ловит чужое окно (Chrome/Discord/Волна): надёжнее `window: "СПО-Вотчер v0.3.1"` с точным заголовком или снимать весь экран (`{ path }` без window) и смотреть глазами.

## Открытое / возможные улучшения
- ~~Сайт на дашборде не полностью темизирован: белые карточки~~ — сделано в v0.3.1.
- Тулбар: пустой промежуток по центру между навигацией и «Настройки» — можно заполнить статусом/инфо.
- Статус-бар: «последняя: —» пустая до первой проверки — ок.
- `$null` не удаляется через delete (ENOENT на rename с именем `$null`) — оставлен, добавлен в .gitignore.
- Render Free засыпает через 15 минут. Пинговать `/health` через cron-job.org раз в 10 минут.
- Возможный перенос exe на Render — НЕВОЗМОЖЕН: Render это Linux, exe это Windows. Можно разделить логику: сервер + веб-интерфейс, но тогда теряется WebView2 и автологин.
