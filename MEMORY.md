# СПО-Вотчер — уведомления из spo.rso23.ru в Telegram

## Что это
Windows-exe: открывает сайт в WebView2, раз в час проверяет оценки, ежедневно в 19:00 шлёт расписание на завтра в Telegram.

## ФИНАЛЬНЫЙ СТЕК
**C# WinForms + WebView2 + .NET 6** → **Vercel Serverless** → **Telegram Bot**.
Tauri/Rust отвергнут из-за недоступности crates.io.

## Окружение
- .NET SDK 6.0.428 (C:\Program Files\dotnet)
- Node v22.22.2
- WebView2 Runtime 153.0.4234.48 (системный)
- VS 2022
- cargo/rust есть, но НЕ используется

## Структура
```
app/                          C# приложение
  Program.cs                  точка входа
  MainForm.cs                 окно + 2 таймера + diff + отправка
  Snapshot.cs                 модель снимка
  Event.cs                    модель события
  Diff.cs                     сравнение оценок (только grades)
  Sender.cs                   POST + HMAC
  Scraper.cs + scraper.js     JS-скрапер (EmbeddedResource)
  spo-watcher.csproj          net6.0-windows, WinForms, WebView2
  config.json                 server_url + secret (рядом с exe)
  bin/Release/net6.0-windows/win-x64/spo-watcher.exe
server/                       Vercel
  api/notify.js               релей (types: grade, lessons, startup, substitution)
  package.json, vercel.json, README.md
plan.md                       актуальный план
MEMORY.md                     этот файл
*.html                        HTML-снимки сайта (для селекторов)
```

## Сборка и запуск
- Сборка: `cd app && dotnet build -c Release`
- Запуск: `app/bin/Release/net6.0-windows/win-x64/spo-watcher.exe`
- Сессия сайта: `%APPDATA%\spo-watcher\webview`

## Логика
1. Старт → сайт. Логин вручную. Сессия сохраняется.
2. Таймер 1 час → `RunGradesCheckAsync()`: `#/reports/performance`, сбор снимка, diff только по оценкам, новые → POST.
3. Таймер 5 мин → `MaybeSendDailyLessonsAsync()`: если 19:00 и сегодня не шлём → `SendTomorrowLessonsAsync()`: `#/lessons`, при необходимости клик nextWeek, формат расписания на завтра, POST (type: lessons).

## Типы событий (сервер)
- `grade`: subject, date, value = «5»/«4»/«3»
- `lessons`: subject = «Расписание на DD.MM.YYYY», date = ISO, value = многострочный текст
- `startup`: тест
- `substitution`: зарезервирован, не используется

## Env / конфиг
**Vercel**: `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID`, `WEBHOOK_SECRET`
**config.json** (рядом с exe): `server_url`, `secret`

## Ключевые решения
- Замены НЕ парсим отдельно — на сайте они не отличаются. Шлём расписание на завтра.
- HMAC-SHA256 (X-Signature) от raw body.
- `chat_id` жёстко в env Vercel.
- JS-скрапер — отдельным файлом (EmbeddedResource).

## Грабли
- **НЕ пихать JS в C# verbatim-строку** — кавычки рвут строку.
- write_file иногда теряется — проверять stat/read_file.
- `find_tool` НЕ существует.
- `list_dir` вне рабочей папки → EPATHJAIL.
- cmd портит cp866-вывод.
- `start_process` в PowerShell требует `.\` перед exe.
- `edit_many` падал с EARGS — переписывать файл целиком.

## Открытое
- **Ждём URL Vercel** для `config.json`.
- Бот: `@spo_votcher_bot`. Старый токен засвечен → отозван.
- config.json пока пустой.
