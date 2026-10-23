import io
import json
import os
import sys
import tempfile
import threading
import unittest
from contextlib import redirect_stdout, redirect_stderr
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / 'scripts'))
from operations.events import parse_event
from operations.collector import OperationalCollector
from operations.reader import read_events
from operations.streams import forward_output


def alert(service='ordering', event_id=7100):
    return json.dumps({
        'Timestamp': '2026-10-23T14:00:00.000Z',
        'EventId': event_id, 'LogLevel': 'Information' if event_id == 7102 else 'Warning',
        'Category': 'SurubDirect.' + service.title() + '.Api.Workers.MessagingMonitorWorker',
        'Message': 'sensitive message', 'Exception': 'sensitive exception', 'Scopes': ['secret'],
        'State': {'AlertCode': service + ('.broker-unavailable' if service == 'ordering' else '.outbox-delayed'),
                  'AlertState': {7100: 'Raised', 7101: 'Reminder', 7102: 'Recovered'}[event_id],
                  'Message': 'private', 'Password': 'secret', 'Payload': 'private body'},
    })


class OperationalLogsTests(unittest.TestCase):
    def test_allowlist_preserves_transitions_and_excludes_sensitive_fields(self):
        for event_id, name in [(7100, 'Raised'), (7101, 'Reminder'), (7102, 'Recovered')]:
            event = parse_event('ordering', alert(event_id=event_id))
            self.assertEqual(name, event['alertState'])
            self.assertEqual('2026-10-23T14:00:00+00:00', event['timestamp'])
            self.assertEqual('ordering', event['service'])
            text = json.dumps(event)
            for secret in ['sensitive', 'secret', 'private', 'Password', 'Scopes']:
                self.assertNotIn(secret, text)

    def test_wrong_source_category_event_level_and_transition_are_ignored(self):
        self.assertIsNone(parse_event('catalog', alert()))
        self.assertIsNone(parse_event('inventory', alert()))
        for key, value in [('Category', 'Untrusted'), ('EventId', 1), ('EventId', True),
                           ('LogLevel', 'Information')]:
            event = json.loads(alert())
            event[key] = value
            self.assertIsNone(parse_event('ordering', json.dumps(event)))
        for key, value in [('AlertState', 'Recovered'), ('AlertCode', 'ordering.secret'), ('AlertCode', {})]:
            event = json.loads(alert())
            event['State'][key] = value
            self.assertIsNone(parse_event('ordering', json.dumps(event)))

    def test_invalid_and_oversized_lines_do_not_break_collection(self):
        for value in ['not-json', '[]', 'null', '{}', '{"State":[]}', 'x' * 65537]:
            self.assertIsNone(parse_event('ordering', value))

    def test_retention_events_have_typed_bounded_counts(self):
        for service, event_id, worker in [('inventory', 7200, 'OutboxRetentionWorker'),
                                          ('ordering', 7300, 'FailedMessageRetentionWorker'),
                                          ('ordering', 7400, 'ExpiredCartCleanupWorker')]:
            event = {'EventId': event_id, 'Category': f'SurubDirect.{service.title()}.Api.Workers.{worker}',
                     'LogLevel': 'Information', 'State': {'DryRun': True, 'Candidates': 5, 'Removed': 0}}
            parsed = parse_event(service, json.dumps(event))
            self.assertEqual(5, parsed['candidates'])
            for key, value in [('Candidates', True), ('Candidates', 1001), ('Removed', 1), ('DryRun', 'true')]:
                changed = {**event, 'State': {**event['State'], key: value}}
                self.assertIsNone(parse_event(service, json.dumps(changed)))

    def test_unknown_error_type_is_not_saved_as_arbitrary_text(self):
        event = {'EventId': 7301, 'Category': 'SurubDirect.Ordering.Api.Workers.FailedMessageRetentionWorker',
                 'LogLevel': 'Warning', 'State': {'ErrorType': 'secret-password'}}
        self.assertEqual('OtherException', parse_event('ordering', json.dumps(event))['errorType'])
        event['State']['ErrorType'] = {}
        self.assertEqual('OtherException', parse_event('ordering', json.dumps(event))['errorType'])

    def test_threaded_writes_rotation_and_private_permissions(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp) / 'events'
            collector = OperationalCollector(directory, max_bytes=1200, backups=3)
            threads = [threading.Thread(target=lambda: [collector.record('ordering', alert()) for _ in range(30)])
                       for _ in range(3)]
            for thread in threads:
                thread.start()
            for thread in threads:
                thread.join()
            collector.close()
            files = list(directory.glob('events.jsonl*'))
            self.assertLessEqual(len(files), 4)
            self.assertGreater(len(files), 1)
            self.assertEqual(0o700, directory.stat().st_mode & 0o777)
            for file in files:
                self.assertEqual(0o600, file.stat().st_mode & 0o777)
                self.assertLess(file.stat().st_size, 1200)
                for line in file.read_text().splitlines():
                    self.assertEqual('ordering', json.loads(line)['service'])

    def test_reader_filters_limits_and_tolerates_partial_tail(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            collector = OperationalCollector(directory)
            collector.record('inventory', alert('inventory'))
            collector.record('ordering', alert(event_id=7100))
            collector.record('ordering', alert(event_id=7102))
            collector.close()
            with (directory / 'events.jsonl').open('a') as output:
                output.write('{incomplete')
            self.assertEqual('Recovered', read_events(directory, 1)[0]['alertState'])
            self.assertEqual(1, len(read_events(directory, service='inventory')))
            with self.assertRaises(ValueError):
                read_events(directory, 0)

    def test_disk_failure_is_reported_without_stopping_output_drain(self):
        class Sink:
            calls = 0
            def record(self, service, line):
                self.calls += 1
                if self.calls == 1:
                    raise OSError('private filesystem detail')
                return True
        sink = Sink()
        out, errors = io.StringIO(), io.StringIO()
        with redirect_stdout(out), redirect_stderr(errors):
            forward_output('ordering', io.StringIO('one\ntwo\n'), sink)
        self.assertEqual(2, sink.calls)
        self.assertIn('one', out.getvalue())
        self.assertIn('unavailable', errors.getvalue())
        self.assertIn('recovered', errors.getvalue())
        self.assertNotIn('private filesystem', errors.getvalue())

    def test_only_one_process_can_own_the_rotating_journal(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            first = OperationalCollector(directory)
            with self.assertRaises(OSError):
                OperationalCollector(directory)
            first.close()
            second = OperationalCollector(directory)
            self.assertTrue(second.record('ordering', alert()))
            second.close()

    def test_real_sink_surfaces_write_failure(self):
        with tempfile.TemporaryDirectory() as tmp:
            collector = OperationalCollector(Path(tmp))
            with patch.object(collector.handler, 'shouldRollover', side_effect=OSError('full')):
                with self.assertRaises(OSError):
                    collector.record('ordering', alert())
            collector.close()


if __name__ == '__main__':
    unittest.main()
