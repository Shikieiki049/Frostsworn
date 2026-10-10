import base64
import json
import threading
import unittest
import urllib.error
import urllib.request
from http.server import ThreadingHTTPServer
from report_relay import GitHubStore, RelayHandler, sanitize, LIMIT


def report():
    return {'schema': 'frostsworn.error-report.v1', 'request': 'diagnostics',
            'timestamp_utc': '2026-10-10T12:00:00Z', 'mod_version': '0.8.29',
            'payload': {'message': 'ERROR: peer 76561198000000001, hp 48',
                        'username': 'PrivateUser', 'token': 'private-secret'}}


class RelayTests(unittest.TestCase):
    def test_virtual_paths_and_repeated_scrubbing(self):
        raw = report()
        raw['payload']['paths'] = ['res://Frostsworn/art075/energy.png', 'user://logs/godot.log',
                                  r'C:\Users\PrivateName\save.json', 's:/private/save.json',
                                  'file:///C:/private/save.json', '/home/private/save.json',
                                  r'\\server\private\save.json']
        once = json.loads(sanitize(raw))
        self.assertEqual(once['payload']['paths'][:2], raw['payload']['paths'][:2])
        self.assertEqual(once['payload']['paths'][2:], ['[absolute-path]'] * 5)
        self.assertEqual(json.loads(sanitize(once))['payload']['paths'], once['payload']['paths'])

    def test_public_boundary_and_path(self):
        raw = report()
        raw.update(repo='attacker/other', path='Source/Entry.cs', bundle='RAW_SAVE')
        raw['payload']['message'] += ' a@example.com 192.168.1.2 ::1 ghp_SECRET C:\\Users\\PrivateUser\\save.json'
        encoded = sanitize(raw)
        for forbidden in (b'PrivateUser', b'private-secret', b'76561198', b'example.com', b'192.168', b'::1', b'ghp_SECRET', b'attacker', b'RAW_SAVE'):
            self.assertNotIn(forbidden, encoded)
        calls = []
        files = {}

        def github(method, path, data=None):
            calls.append((method, path, data))
            if path == 'git/ref/heads/error-reports':
                return 200, {}
            if method == 'GET':
                key = path.split('?')[0]
                return (200, {'content': files[key]}) if key in files else (404, {})
            self.assertEqual(method, 'PUT')
            self.assertTrue(path.startswith('contents/error-reports/2026-10-10/'))
            self.assertEqual(data['branch'], 'error-reports')
            self.assertNotIn('sha', data)  # create only; never overwrite existing content
            files[path] = data['content']
            return 201, {}

        store = GitHubStore('test-server-only-token', github)
        url = store.save(encoded)
        self.assertEqual(store.save(encoded), url)
        self.assertEqual(sum(c[0] == 'PUT' for c in calls), 1)
        self.assertTrue(url.startswith('https://github.com/Shikieiki049/Frostsworn/blob/error-reports/error-reports/'))

    def test_network_failure_is_not_success(self):
        store = GitHubStore('test', lambda method, path, data=None: (403, {}))
        with self.assertRaises(RuntimeError):
            store.save(sanitize(report()))

    def test_new_branch_uses_default_head(self):
        calls = []
        ready = False

        def github(method, path, data=None):
            nonlocal ready
            calls.append((method, path, data))
            if path == 'git/ref/heads/error-reports':
                return (200 if ready else 404), {}
            if path == '':
                return 200, {'default_branch': 'main'}
            if path == 'git/ref/heads/main':
                return 200, {'object': {'sha': 'existing-head'}}
            if path == 'git/refs':
                self.assertEqual(data, {'ref': 'refs/heads/error-reports', 'sha': 'existing-head'})
                ready = True
                return 201, {}
            return (404, {}) if method == 'GET' else (201, {})

        GitHubStore('test', github).save(sanitize(report()))
        self.assertEqual(sum(c[0] == 'POST' for c in calls), 1)

    def test_http_contract(self):
        received = []

        class Store:
            def save(self, data):
                received.append(data)
                return 'https://github.com/test/report'

        class Handler(RelayHandler):
            store = Store()

        server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        address = 'http://127.0.0.1:' + str(server.server_port)
        try:
            req = urllib.request.Request(address + '/reports', json.dumps(report()).encode(), headers={'Content-Type': 'application/json'})
            with urllib.request.urlopen(req) as response:
                self.assertEqual(response.status, 201)
                self.assertTrue(json.load(response)['saved'])
            self.assertEqual(len(received), 1)
            self.assertNotIn(b'PrivateUser', received[0])
            for path, body, status in [('/wrong', b'{}', 404), ('/reports', b'{}', 400), ('/reports', b'x' * (LIMIT + 1), 413)]:
                req = urllib.request.Request(address + path, body, headers={'Content-Type': 'application/json'})
                with self.assertRaises(urllib.error.HTTPError) as error:
                    urllib.request.urlopen(req)
                self.assertEqual(error.exception.code, status)
            self.assertEqual(len(received), 1)
        finally:
            server.shutdown()
            server.server_close()
            thread.join()


if __name__ == '__main__':
    unittest.main()
