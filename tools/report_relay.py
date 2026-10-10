"""Frostsworn diagnostic relay. Run behind an HTTPS reverse proxy.

Only the server receives a repository-scoped GitHub token. No third-party packages.
"""
import base64
import datetime as dt
import hashlib
import json
import os
import re
import threading
import time
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

REPO = 'Shikieiki049/Frostsworn'
BRANCH = 'error-reports'
SCHEMA = 'frostsworn.error-report.v1'
LIMIT = 1024 * 1024
KEYS = ('username', 'playername', 'displayname', 'steamid', 'accountid',
        'installid', 'profile', 'ipaddress', 'email', 'authorization',
        'password', 'token', 'secret', 'apikey', 'chat', 'hostname', 'computername')
SECRET = re.compile(r'\b(?:github_pat_|gh[pousr]_)[\w]+|Bearer\s+\S+|(?:password|token|secret|api[_-]?key)\s*[=:]\s*\S+', re.I)
PATH = re.compile(r'(?:file://)?[a-z]:[\\/][^\r\n\"\'<>]+|(?<![:\w/])/(?:home|Users|root|tmp|var|mnt)/[^\r\n\"\'<>]+|\\\\[^\r\n\"\'<>]+', re.I)
EMAIL = re.compile(r'\b[\w.%+-]+@[\w.-]+\.[a-z]{2,}\b', re.I)
IP = re.compile(r'\b(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?\b|(?<!\w)(?:[0-9a-f]{0,4}:){2,}[0-9a-f:]{0,39}(?!\w)', re.I)
IDS = re.compile(r'(?<!\d)\d{17}(?!\d)')


def sanitize(report):
    """Apply the same constraints again at the public boundary."""
    aliases = {}

    def alias(match):
        raw = match.group(0)
        return aliases.setdefault(raw, f'player-{len(aliases) + 1}')

    def text(value):
        value = PATH.sub('[absolute-path]', value[:65536])
        value = SECRET.sub('[credential]', value)
        value = EMAIL.sub('[email]', value)
        return IP.sub('[ip]', IDS.sub(alias, value))

    def walk(value, depth=0):
        if depth > 40:
            return '[depth-limit]'
        if isinstance(value, dict):
            return {text(k): walk(v, depth + 1) for k, v in list(value.items())[:1000]
                    if not any(s in k.lower().replace('_', '').replace('-', '') for s in KEYS)}
        if isinstance(value, list):
            return [walk(v, depth + 1) for v in value[:1000]]
        if isinstance(value, str):
            return text(value)
        if isinstance(value, int) and len(str(value)) == 17:
            return IDS.sub(alias, str(value))
        return value

    if not isinstance(report, dict) or report.get('schema') != SCHEMA:
        raise ValueError('Unsupported report schema')
    if report.get('request') not in ('diagnostics', 'state_divergence'):
        raise ValueError('Unsupported diagnostic request')
    # No client-supplied repository, branch, filename or raw ZIP is honored.
    allowed = ('schema', 'mod_version', 'timestamp_utc', 'event', 'request',
               'payload', 'bundle_files', 'diagnostic_log_tail', 'redaction_note', 'truncated')
    result = walk({k: report[k] for k in allowed if k in report})
    encoded = json.dumps(result, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()
    if len(encoded) > LIMIT:
        raise ValueError('Report too large')
    return encoded


class GitHubStore:
    def __init__(self, token, request=None):
        self.token = token
        self.request = request or self._request
        self.lock = threading.Lock()
        self.branch_ready = False

    def _request(self, method, path, data=None):
        req = urllib.request.Request('https://api.github.com/repos/' + REPO + '/' + path,
            data=None if data is None else json.dumps(data).encode(), method=method,
            headers={'Authorization': 'Bearer ' + self.token,
                     'User-Agent': 'Frostsworn-error-relay',
                     'Accept': 'application/vnd.github+json',
                     'X-GitHub-Api-Version': '2026-03-10', 'Content-Type': 'application/json'})
        try:
            with urllib.request.urlopen(req, timeout=15) as response:
                return response.status, json.load(response)
        except urllib.error.HTTPError as error:
            # Do not echo GitHub response bodies or credentials to clients/logs.
            return error.code, {}

    def _branch(self):
        if self.branch_ready:
            return
        status, _ = self.request('GET', 'git/ref/heads/' + BRANCH)
        if status == 404:
            status, repo = self.request('GET', '')
            if status != 200:
                raise RuntimeError('Repository unavailable')
            status, ref = self.request('GET', 'git/ref/heads/' + repo['default_branch'])
            if status != 200:
                raise RuntimeError('Default branch unavailable')
            status, _ = self.request('POST', 'git/refs', {'ref': 'refs/heads/' + BRANCH, 'sha': ref['object']['sha']})
            if status not in (201, 422):
                raise RuntimeError('Report branch unavailable')
            status, _ = self.request('GET', 'git/ref/heads/' + BRANCH)
        if status != 200:
            raise RuntimeError('Report branch unavailable')
        self.branch_ready = True

    def save(self, encoded):
        digest = hashlib.sha256(encoded).hexdigest()
        report = json.loads(encoded)
        # Stable across midnight/retries; invalid timestamps never influence paths.
        try:
            day = dt.datetime.fromisoformat(report['timestamp_utc'].replace('Z', '+00:00')).astimezone(dt.timezone.utc).date().isoformat()
        except (KeyError, ValueError, TypeError, AttributeError):
            raise ValueError('Invalid report timestamp')
        path = 'error-reports/' + day + '/' + digest + '.json'
        with self.lock:
            self._branch()
            status, current = self.request('GET', 'contents/' + path + '?ref=' + BRANCH)
            if status == 200:
                if base64.b64decode(current.get('content', '')) != encoded:
                    raise RuntimeError('Report collision')
            elif status == 404:
                status, _ = self.request('PUT', 'contents/' + path, {
                    'message': 'Record sanitized Frostsworn diagnostic ' + digest[:12],
                    'branch': BRANCH, 'content': base64.b64encode(encoded).decode()})
                if status != 201:
                    raise RuntimeError('GitHub write failed')
            else:
                raise RuntimeError('GitHub lookup failed')
        return 'https://github.com/' + REPO + '/blob/' + BRANCH + '/' + path


class RelayHandler(BaseHTTPRequestHandler):
    store = None
    budget_lock = threading.Lock()
    budget_start = time.monotonic()
    budget_used = 0

    def log_message(self, *args):
        pass  # No player IP, payload, path, or token in server access logs.

    def reply(self, status, value):
        encoded = json.dumps(value).encode()
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(encoded)))
        self.send_header('Cache-Control', 'no-store')
        self.end_headers()
        self.wfile.write(encoded)

    def do_GET(self):
        self.reply(200 if self.path == '/health' else 404, {'status': 'relay running'})

    def do_POST(self):
        if self.path != '/reports':
            return self.reply(404, {'error': 'Not found'})
        with RelayHandler.budget_lock:
            if time.monotonic() - RelayHandler.budget_start >= 3600:
                RelayHandler.budget_start = time.monotonic()
                RelayHandler.budget_used = 0
            if RelayHandler.budget_used >= 60:
                return self.reply(429, {'error': 'Rate limit; retry later'})
            RelayHandler.budget_used += 1
        try:
            if self.headers.get('Transfer-Encoding'):
                return self.reply(400, {'error': 'Content-Length required'})
            length = int(self.headers.get('Content-Length', '0'))
            if length < 1 or length > LIMIT:
                return self.reply(413, {'error': 'Invalid report size'})
            if self.headers.get_content_type() != 'application/json':
                return self.reply(415, {'error': 'JSON required'})
            self.connection.settimeout(10)
            body = self.rfile.read(length)
            if len(body) != length:
                raise ValueError('Incomplete request')
            encoded = sanitize(json.loads(body))
            url = self.store.save(encoded)
            self.reply(201, {'saved': True, 'url': url})
        except (ValueError, RecursionError):
            self.reply(400, {'error': 'Invalid diagnostic report'})
        except Exception:
            self.reply(503, {'error': 'Report unavailable; retry later'})


if __name__ == '__main__':
    token = os.environ.get('FROST_REPORT_GITHUB_TOKEN', '')
    if not token:
        raise SystemExit('Set FROST_REPORT_GITHUB_TOKEN on the server only.')
    RelayHandler.store = GitHubStore(token)
    ThreadingHTTPServer(('127.0.0.1', int(os.environ.get('FROST_REPORT_PORT', '8780'))), RelayHandler).serve_forever()
