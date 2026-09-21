// СПО-Вотчер — сервер-релей для Render.
// Принимает POST /api/notify, проверяет HMAC, шлёт в Telegram.
// GET / — health-check (для пинга, чтобы Render не засыпал).

const http = require('http');
const crypto = require('crypto');

const PORT = process.env.PORT || 3000;
const ALLOWED_TYPES = new Set(['grade', 'substitution', 'startup', 'lessons']);
const MAX_LEN = 300;
const MAX_LEN_VALUE = 4000;
const MAX_BODY = 64 * 1024; // 64 KB

function checkSignature(rawBody, header, secret) {
  if (!secret) return true;
  if (!header) return false;
  const expected = crypto.createHmac('sha256', secret).update(rawBody).digest('hex');
  const a = Buffer.from(expected, 'utf8');
  const b = Buffer.from(String(header), 'utf8');
  if (a.length !== b.length) return false;
  return crypto.timingSafeEqual(a, b);
}

function cleanStr(v, maxLen) {
  if (typeof v !== 'string') return null;
  const s = v.trim();
  if (!s || s.length > maxLen) return null;
  return s;
}

function formatMessage(ev) {
  if (ev.type === 'grade') return `\uD83D\uDCDD Оценка: ${ev.value}\n${ev.subject}\n${ev.date}`;
  if (ev.type === 'substitution') return `\uD83D\uDD01 Замена:\n${ev.subject}\n${ev.date}\n${ev.value}`;
  if (ev.type === 'startup') return `\u2705 Мониторинг запущен.\n${ev.date}`;
  if (ev.type === 'lessons') return `\uD83D\uDCC5 ${ev.subject}\n\n${ev.value}`;
  return null;
}

function sendJson(res, code, obj) {
  const body = JSON.stringify(obj);
  res.writeHead(code, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': Buffer.byteLength(body)
  });
  res.end(body);
}

async function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on('data', (c) => {
      size += c.length;
      if (size > MAX_BODY) { reject(new Error('body_too_large')); req.destroy(); return; }
      chunks.push(c);
    });
    req.on('end', () => resolve(Buffer.concat(chunks).toString('utf8')));
    req.on('error', reject);
  });
}

async function handleNotify(req, res) {
  let raw;
  try {
    raw = await readBody(req);
  } catch (e) {
    return sendJson(res, 400, { ok: false, error: 'bad_body' });
  }

  const sig = req.headers['x-signature'];
  if (!checkSignature(raw, sig, process.env.WEBHOOK_SECRET)) {
    return sendJson(res, 401, { ok: false, error: 'bad_signature' });
  }

  let ev;
  try { ev = JSON.parse(raw); } catch { return sendJson(res, 400, { ok: false, error: 'invalid_json' }); }
  if (!ev || typeof ev !== 'object') return sendJson(res, 400, { ok: false, error: 'invalid_event' });
  if (!ALLOWED_TYPES.has(ev.type)) return sendJson(res, 400, { ok: false, error: 'invalid_type' });

  const subject = cleanStr(ev.subject, MAX_LEN) || '';
  const date    = cleanStr(ev.date, MAX_LEN) || '';
  const value   = cleanStr(ev.value, MAX_LEN_VALUE) || '';

  if (ev.type !== 'startup') {
    if (!subject) return sendJson(res, 400, { ok: false, error: 'invalid_subject' });
    if (!date)    return sendJson(res, 400, { ok: false, error: 'invalid_date' });
    if (!value)   return sendJson(res, 400, { ok: false, error: 'invalid_value' });
  }

  const text = formatMessage({ type: ev.type, subject, date, value });
  if (!text) return sendJson(res, 400, { ok: false, error: 'cannot_format' });

  const token = process.env.TELEGRAM_BOT_TOKEN;
  const chatId = process.env.TELEGRAM_CHAT_ID;
  if (!token || !chatId) return sendJson(res, 500, { ok: false, error: 'server_not_configured' });

  const tgUrl = `https://api.telegram.org/bot${token}/sendMessage`;
  try {
    const r = await fetch(tgUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ chat_id: chatId, text, disable_web_page_preview: true })
    });
    const data = await r.json().catch(() => ({}));
    if (!r.ok || !data.ok) {
      console.error('telegram error:', data.description || r.status);
      return sendJson(res, 502, { ok: false, error: 'telegram_error: ' + (data.description || r.status) });
    }
  } catch (e) {
    return sendJson(res, 502, { ok: false, error: 'telegram_unreachable: ' + (e && e.message) });
  }

  return sendJson(res, 200, { ok: true });
}

const server = http.createServer((req, res) => {
  const url = new URL(req.url, 'http://localhost');
  const path = url.pathname;

  // Health-check для пинга и для Render.
  if (req.method === 'GET' && (path === '/' || path === '/health')) {
    return sendJson(res, 200, {
      ok: true,
      service: 'spo-watcher-server',
      time: new Date().toISOString(),
      configured: Boolean(process.env.TELEGRAM_BOT_TOKEN && process.env.TELEGRAM_CHAT_ID)
    });
  }

  if (req.method === 'POST' && path === '/api/notify') {
    return handleNotify(req, res);
  }

  return sendJson(res, 404, { ok: false, error: 'not_found' });
});

server.listen(PORT, () => {
  console.log(`spo-watcher-server listening on :${PORT}`);
  console.log('configured:', Boolean(process.env.TELEGRAM_BOT_TOKEN && process.env.TELEGRAM_CHAT_ID));
  console.log('hmac:', Boolean(process.env.WEBHOOK_SECRET));
});
