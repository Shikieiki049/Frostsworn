import assert from 'node:assert/strict';
import { test } from 'node:test';
import worker, { sanitize, saveReport } from './worker.mjs';
const sample = () => ({ schema: 'frostsworn.error-report.v1', request: 'diagnostics', mod_version: '0.8.30', timestamp_utc: '2026-10-10T12:00:00Z', payload: { message: 'ERROR: hp 48' } });
const json = (body, status = 200) => new Response(JSON.stringify(body), { status });

test('virtual game paths survive repeated scrubbing while filesystem paths stay private', () => {
  const report = sample();
  report.payload.paths = ['res://Frostsworn/art075/energy.png', 'user://logs/godot.log',
    'C:\\Users\\PrivateName\\save.json', 's:/private/save.json', 'file:///C:/private/save.json',
    '/home/private/save.json', '\\\\server\\private\\save.json'];
  const once = sanitize(report).result;
  assert.deepEqual(once.payload.paths.slice(0, 2), report.payload.paths.slice(0, 2));
  assert.deepEqual(once.payload.paths.slice(2), Array(5).fill('[absolute-path]'));
  assert.deepEqual(sanitize(once).result.payload.paths, once.payload.paths);
});

test('public boundary strips identities and arbitrary destination', () => {
  const report = sample();
  report.path = 'Source/Entry.cs'; report.repo = 'attacker/repo'; report.bundle = 'RAW_SAVE';
  report.payload.username = 'PrivateName'; report.payload.password = 'private-password';
  report.payload.message = 'ERROR: 76561198000000001 76561198000000002 a@example.com 192.168.1.2 ::1 ghp_SECRET C:\\Users\\PrivateName\\save.json';
  const encoded = new TextDecoder().decode(sanitize(report).encoded);
  for (const forbidden of ['76561198', 'example.com', '192.168', '::1', 'ghp_SECRET', 'PrivateName', 'private-password', 'Source/Entry.cs', 'attacker', 'RAW_SAVE']) assert(!encoded.includes(forbidden), forbidden);
  assert(encoded.includes('player-1') && encoded.includes('player-2'));
  assert.throws(() => sanitize({ ...sample(), request: 'run_history' }));
  assert.throws(() => sanitize({ ...sample(), timestamp_utc: '../../escape' }));
});

test('fixed repository and branch, create only, exact repeat deduplication', async () => {
  const files = new Map(); let writes = 0;
  const github = async (url, options) => {
    assert(url.startsWith('https://api.github.com/repos/Shikieiki049/Frostsworn/'));
    assert(options.headers.Authorization === 'Bearer test-secret');
    assert.equal(options.redirect, 'manual'); // Cloudflare supports manual/follow; never forward credentials.
    const path = new URL(url).pathname;
    if (path.endsWith('git/ref/heads/error-reports')) return json({});
    if (options.method === 'GET') return files.has(path) ? json({ content: files.get(path) }) : json({}, 404);
    assert(options.method === 'PUT'); assert(path.includes('/contents/error-reports/2026-10-10/'));
    const body = JSON.parse(options.body); assert.equal(body.branch, 'error-reports'); assert(!Object.hasOwn(body, 'sha'));
    writes++; files.set(path, body.content); return json({}, 201);
  };
  const first = await saveReport(sample(), 'test-secret', github);
  assert.equal(await saveReport(sample(), 'test-secret', github), first);
  assert.equal(writes, 1);
});

test('branch creation and concurrent create race verify existing bytes', async () => {
  let ready = false, content, post = 0;
  const github = async (url, options) => {
    if (url.endsWith('git/ref/heads/error-reports')) return json({}, ready ? 200 : 404);
    if (url.endsWith('/Frostsworn')) return json({ default_branch: 'main' });
    if (url.endsWith('git/ref/heads/main')) return json({ object: { sha: 'existing-head' } });
    if (options.method === 'POST') { const body = JSON.parse(options.body); assert.deepEqual(body, { ref: 'refs/heads/error-reports', sha: 'existing-head' }); post++; ready = true; return json({}, 201); }
    if (options.method === 'GET') return content ? json({ content }) : json({}, 404);
    content = JSON.parse(options.body).content; return json({}, 409);
  };
  assert((await saveReport(sample(), 'test', github)).includes('/blob/error-reports/error-reports/'));
  assert.equal(post, 1);
});

test('GitHub rejection never becomes a successful receipt', async () => {
  await assert.rejects(saveReport(sample(), 'test', async () => json({}, 403)));
  await assert.rejects(saveReport(sample(), 'test', async () => new Response(null, { status: 302, headers: { Location: 'https://example.com' } })));
});

test('HTTP validation, missing secret, rate limiting, health', async () => {
  const req = (path, body = {}, headers = { 'Content-Type': 'application/json' }) => new Request('https://example.workers.dev' + path, { method: 'POST', headers, body: JSON.stringify(body) });
  let result = await worker.fetch(new Request('https://example.workers.dev/health'), {});
  assert.deepEqual(await result.json(), { status: 'running', configured: false });
  assert.equal((await worker.fetch(req('/reports', sample()), {})).status, 503);
  assert.equal((await worker.fetch(req('/wrong'), { GITHUB_TOKEN: 'test' })).status, 404);
  assert.equal((await worker.fetch(req('/reports'), { GITHUB_TOKEN: 'test' })).status, 400);
  assert.equal((await worker.fetch(req('/reports', {}, { 'Content-Type': 'application/json', 'Content-Length': '1048577' }), { GITHUB_TOKEN: 'test' })).status, 413);
  assert.equal((await worker.fetch(req('/reports'), { GITHUB_TOKEN: 'test', REPORT_RATE_LIMITER: { limit: async () => ({ success: false }) } })).status, 429);
});
