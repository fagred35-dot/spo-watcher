# spo-watcher-server

Релей: принимает обезличенные события от приложения СПО-Вотчер и отправляет сообщения в Telegram.

## Эндпоинты

- `GET /` или `GET /health` — health-check. Возвращает `{ok, service, time, configured}`.
- `POST /api/notify` — приём события. Тело:
  ```json
  { "type": "grade" | "lessons" | "startup", "subject": "...", "date": "...", "value": "..." }
  ```
  Заголовок `X-Signature` — HMAC-SHA256 от raw body с `WEBHOOK_SECRET` (если секрет задан).

## Env-переменные

| Имя | Обязательна | Описание |
|---|---|---|
| `TELEGRAM_BOT_TOKEN` | да | Токен бота от @BotFather |
| `TELEGRAM_CHAT_ID` | да | ID чата владельца (жёстко, из запроса не берётся) |
| `WEBHOOK_SECRET` | нет | HMAC-секрет. Если пусто — подпись не проверяется |
| `PORT` | нет | Render задаёт сам |

## Деплой на Render

1. Залей папку `server/` в GitHub (репозиторий приватный).
2. На render.com: **New → Web Service → Connect GitHub repo**.
3. Настройки:
   - **Root Directory**: `server` (если репо с корнем проекта) или пусто (если репо = папка server)
   - **Environment**: `Node`
   - **Build Command**: `npm install` (или оставить пустым — нет зависимостей)
   - **Start Command**: `npm start`
   - **Instance Type**: Free
4. **Environment → Add Environment Variable** — три переменные из таблицы.
5. **Create Web Service**.

## Про Free Plan

Render Free засыпает через 15 минут без трафика. Первый запрос после сна ждёт 30–60 с.
Решение — пинговать `/health` раз в 10 минут через [cron-job.org](https://cron-job.org) (бесплатно).

## Проверка

```bash
curl https://<app>.onrender.com/health
# → {"ok":true,"service":"spo-watcher-server","configured":true}
```
