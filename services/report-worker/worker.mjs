// Paste this whole module into Cloudflare Workers. GITHUB_TOKEN is a Secret.
// No player authentication, package dependencies, or client-side credentials.
const REPO = 'Shikieiki049/Frostsworn';
const BRANCH = 'error-reports';
const MAX = 1024 * 1024;
const encoder = new TextEncoder();
const privateKeys = /username|playername|displayname|steamid|accountid|installid|profile|ipaddress|email|authorization|password|token|secret|apikey|chat|hostname|computername/;
let windowStart = 0, requests = 0;

export function sanitize(input) {
  if (!input || input.schema !== 'frostsworn.error-report.v1' || !['diagnostics', 'state_divergence'].includes(input.request)) throw new Error('schema');
  const aliases = new Map();
  const text = value => value.slice(0, 65536)
    .replace(/(?:file:\/\/)?[a-z]:[\\/][^\r\n"'<>]+|(?<![:\w/])\/(?:home|Users|root|tmp|var|mnt)\/[^\r\n"'<>]+|\\\\[^\r\n"'<>]+/gi, '[absolute-path]')
    .replace(/\b(?:github_pat_|gh[pousr]_)[\w]+|Bearer\s+\S+|(?:password|token|secret|api[_-]?key)\s*[=:]\s*\S+/gi, '[credential]')
    .replace(/\b[\w.%+-]+@[\w.-]+\.[a-z]{2,}\b/gi, '[email]')
    .replace(/(?<!\d)\d{17}(?!\d)/g, id => {
      if (!aliases.has(id)) aliases.set(id, `player-${aliases.size + 1}`);
      return aliases.get(id);
    })
    .replace(/\b(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?\b|(?<!\w)(?:[0-9a-f]{0,4}:){2,}[0-9a-f:]{0,39}(?!\w)/gi, '[ip]');
  function walk(value, depth = 0) {
    if (depth > 40) return '[depth-limit]';
    if (typeof value === 'string') return text(value);
    // JSON numbers above Number.MAX_SAFE_INTEGER cannot retain exact peer IDs.
    if (typeof value === 'number' && !Number.isSafeInteger(value) && Number.isInteger(value)) return '[numeric-id]';
    if (Array.isArray(value)) return value.slice(0, 1000).map(v => walk(v, depth + 1));
    if (value && typeof value === 'object') {
      const result = Object.create(null);
      for (const key of Object.keys(value).sort().slice(0, 1000)) {
        if (!privateKeys.test(key.toLowerCase().replace(/[_-]/g, ''))) result[text(key)] = walk(value[key], depth + 1);
      }
      return result;
    }
    return value;
  }
  const allowed = ['schema', 'mod_version', 'timestamp_utc', 'event', 'request', 'payload', 'bundle_files', 'diagnostic_log_tail', 'redaction_note', 'truncated'];
  const filtered = Object.fromEntries(allowed.filter(key => Object.hasOwn(input, key)).map(key => [key, input[key]]));
  const result = walk(filtered);
  if (typeof result.timestamp_utc !== 'string' || !/^\d{4}-\d{2}-\d{2}T/.test(result.timestamp_utc) || !Number.isFinite(Date.parse(result.timestamp_utc))) throw new Error('timestamp');
  const encoded = encoder.encode(JSON.stringify(result));
  if (encoded.length > MAX) throw new Error('size');
  return { result, encoded };
}

function base64(bytes) {
  let binary = '';
  for (let offset = 0; offset < bytes.length; offset += 8192) binary += String.fromCharCode(...bytes.subarray(offset, offset + 8192));
  return btoa(binary);
}

export async function saveReport(report, token, apiFetch = fetch) {
  const { result, encoded } = sanitize(report);
  const digest = Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256', encoded)), x => x.toString(16).padStart(2, '0')).join('');
  const day = new Date(result.timestamp_utc).toISOString().slice(0, 10);
  const path = `error-reports/${day}/${digest}.json`;
  async function api(method, route, body) {
    return apiFetch(`https://api.github.com/repos/${REPO}/${route}`, {
      method, redirect: 'error', signal: AbortSignal.timeout(12000),
      headers: { Authorization: `Bearer ${token}`, 'User-Agent': 'Frostsworn-error-relay', Accept: 'application/vnd.github+json',
        'X-GitHub-Api-Version': '2026-03-10', 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body)
    });
  }
  let ref = await api('GET', `git/ref/heads/${BRANCH}`);
  if (ref.status === 404) {
    const repo = await api('GET', '');
    if (!repo.ok) throw new Error('repository');
    const head = await api('GET', 'git/ref/heads/' + (await repo.json()).default_branch);
    if (!head.ok) throw new Error('head');
    const created = await api('POST', 'git/refs', { ref: `refs/heads/${BRANCH}`, sha: (await head.json()).object.sha });
    if (![201, 422].includes(created.status)) throw new Error('branch');
    ref = await api('GET', `git/ref/heads/${BRANCH}`);
  }
  if (!ref.ok) throw new Error('branch');
  const content = base64(encoded);
  async function existing() {
    const response = await api('GET', `contents/${path}?ref=${BRANCH}`);
    if (response.status === 404) return false;
    if (!response.ok) throw new Error('lookup');
    if ((await response.json()).content.replace(/\s/g, '') !== content) throw new Error('collision');
    return true;
  }
  if (!await existing()) {
    const created = await api('PUT', `contents/${path}`, { message: `Record sanitized Frostsworn diagnostic ${digest.slice(0, 12)}`, branch: BRANCH, content });
    // Concurrent creates can race. Verify the identical existing file, never overwrite.
    if (created.status !== 201 && !([409, 422].includes(created.status) && await existing())) throw new Error('write');
  }
  return `https://github.com/${REPO}/blob/${BRANCH}/${path}`;
}

const reply = (status, body) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' } });

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === 'GET' && url.pathname === '/health') return reply(200, { status: 'running', configured: Boolean(env.GITHUB_TOKEN) });
    if (url.pathname !== '/reports' || request.method !== 'POST') return reply(404, { error: 'Not found' });
    if (!env.GITHUB_TOKEN) return reply(503, { error: 'Service not configured' });
    if (Date.now() - windowStart > 3600000) { windowStart = Date.now(); requests = 0; }
    // Fallback is per isolate, not a global guarantee. Wrangler also supplies
    // a Cloudflare location-based limiter; neither identifies players in reports.
    if (++requests > 60) return reply(429, { error: 'Retry later' });
    if (env.REPORT_RATE_LIMITER && !(await env.REPORT_RATE_LIMITER.limit({ key: '/reports' })).success) return reply(429, { error: 'Retry later' });
    if (!request.headers.get('Content-Type')?.toLowerCase().startsWith('application/json')) return reply(415, { error: 'JSON required' });
    const length = Number(request.headers.get('Content-Length'));
    if (length > MAX) return reply(413, { error: 'Report too large' });
    const reader = request.body?.getReader();
    if (!reader) return reply(400, { error: 'Empty report' });
    const chunks = []; let total = 0;
    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        total += value.length;
        if (total > MAX) { await reader.cancel(); return reply(413, { error: 'Report too large' }); }
        chunks.push(value);
      }
      const bytes = new Uint8Array(total); let offset = 0;
      for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.length; }
      let report;
      try { report = JSON.parse(new TextDecoder().decode(bytes)); sanitize(report); }
      catch { return reply(400, { error: 'Invalid diagnostic report' }); }
      const savedUrl = await saveReport(report, env.GITHUB_TOKEN);
      return reply(201, { saved: true, url: savedUrl });
    } catch { return reply(503, { error: 'Report unavailable; retry later' }); }
  }
};
