# СПО-Вотчер — уведомления из spo.rso23.ru в Telegram

## Что это
Windows-exe, который:
- открывает https://spo.rso23.ru/students/student.html#/ внутри себя (WebView2);
- один раз логинится (сессия сохраняется в `%APPDATA%\spo-watcher\webview`);
- **раз в час** проверяет новые оценки → шлёт в Telegram;
- **каждый день в 19:00** отправляет расписание на завтра одним сообщением.

## Стек (финальный)
**C# WinForms + WebView2 + .NET 6** → **Vercel Serverless** → **Telegram Bot**.

Tauri/Rust **отвергнут**: crates.io на машине недоступен (нулевая скорость загрузки).

## Архитектура
```
┌─────────────────────────────────────┐
│  spo-watcher.exe (C# + WebView2)    │
│  - окно с сайтом                    │
│  - таймер 1 ч: оценки → diff        │
│  - таймер 5 мин: не 19:00 ли?       │
│  - POST на Vercel (HMAC)            │
└───────────────┬─────────────────────┘
                │ HTTPS (обезличено)
                ▼
      ┌──────────────────┐    HTTPS    ┌──────────────┐
      │ Vercel /notify   │ ──────────► │ Telegram Bot │
      └──────────────────┘             └──────────────┘
```

## UI
- Приложение и сайт в тёмной теме. Палитра: фон #181a1b, поверхность #232629, акцент #ff9f43.
- Кастомные кнопки (ModernButton), тёмный лог внизу.
- Сайт перекрашивается через инъекцию `site-dark.css` на каждой навигации.
- Кнопка «Войти» на сайте остаётся оранжевой, как было.

## Компоненты

### app/ — C# приложение
- `Program.cs` — точка входа.
- `MainForm.cs` — окно, кнопки навигации, два таймера, логика diff и отправки.
- `Snapshot.cs` — модель снимка (`grades` + `lessons`).
- `Event.cs` — модель события для сервера.
- `Diff.cs` — сравнение оценок.
- `Sender.cs` — HTTP POST + HMAC-SHA256.
- `Scraper.cs` + `scraper.js` — JS-скрапер (EmbeddedResource, встроен в exe).
- `config.json` — рядом с exe: `{ "server_url": "...", "secret": "..." }`.
- Сборка: `cd app && dotnet build -c Release`.
- Готовый exe: `app/bin/Release/net6.0-windows/win-x64/spo-watcher.exe`.

### server/ — Vercel
- `api/notify.js` — принимает `POST`, проверяет HMAC, шлёт в Telegram.
- Типы событий: `grade`, `lessons`, `startup`, `substitution` (зарезервирован).
- Env: `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID`, `WEBHOOK_SECRET`.

### Telegram
- Бот: `@spo_votcher_bot`.
- Отвечает только владельцу (chat_id жёстко в env Vercel).

## Селекторы парсинга
**Оценки:** `tbody[x-ng-repeat*="rowData in reportRows"]` → `td.big.bold` (название) + `td[x-ng-bind="day.value"]` (оценки). Дни: `th[x-ng-bind*="moment.format"]`.
**Расписание:** `dl[x-ng-repeat*="day in week"]` → `.long-date` (дата), `.lesson` → `.summary big` (предмет), `nobr.teacher[title]`, `nobr.classroom`, `data.time .start/.end`.

## Почему не ловим «замены»
На сайте замены **ничем не отличаются от обычных пар** (подтверждено пользователем). Поэтому вместо diff-по-парам шлём расписание на завтра целиком — надёжнее и полезнее.

## Следующие шаги
1. Вписать URL Vercel и secret в `app/config.json`.
2. Пересобрать exe.
3. Запустить, залогиниться, нажать «Проверить оценки» и «Расписание на завтра».
4. Убедиться, что в Telegram пришло.
