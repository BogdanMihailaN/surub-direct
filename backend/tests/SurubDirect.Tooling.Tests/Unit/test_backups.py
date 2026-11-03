import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / 'scripts'))
from backups.commands import create, verify
from backups.manifest import DATABASES, digest, validate


class BackupTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.calls = []

    def postgres(self, args, **kwargs):
        self.calls.append(args)
        if args[0] == 'pg_dump':
            kwargs['stdout'].write(b'PGDMP-private-test-data')
        else:
            self.assertEqual(['pg_restore', '--list'], args)
            self.assertTrue(kwargs['stdin'].read().startswith(b'PGDMP'))

    def create(self):
        with patch('backups.commands.ROOT', self.root), patch('backups.commands.postgres', self.postgres):
            return create()

    def manifest(self, folder, mutate):
        path = folder / 'manifest.json'
        value = json.loads(path.read_text())
        mutate(value)
        path.write_text(json.dumps(value))

    def test_complete_backup_has_private_files_all_databases_and_only_read_commands(self):
        folder = self.create()
        self.assertEqual(0o700, folder.stat().st_mode & 0o777)
        for path in folder.iterdir():
            self.assertEqual(0o600, path.stat().st_mode & 0o777)
        data = validate(folder)
        self.assertEqual({'surub_' + x for x in DATABASES}, {x['database'] for x in data['databases']})
        self.assertEqual(5, sum(args[0] == 'pg_dump' for args in self.calls))
        self.assertEqual(5, sum(args[0] == 'pg_restore' for args in self.calls))
        self.assertNotIn('private-test-data', (folder / 'manifest.json').read_text())
        self.assertEqual('per-database', data['consistency'])

    def test_corruption_and_truncation_are_detected_before_postgres(self):
        for content in (b'', b'XGDMP-private-test-data'):
            with self.subTest(content=content):
                folder = self.create()
                (folder / 'ordering.dump').write_bytes(content)
                with patch('backups.commands.postgres') as command, self.assertRaises(ValueError):
                    verify(folder)
                command.assert_not_called()

    def test_manifest_rejects_missing_duplicate_traversal_and_wrong_database(self):
        mutations = [lambda x: x['databases'].pop(),
                     lambda x: x['databases'].__setitem__(1, x['databases'][0]),
                     lambda x: x['databases'][0].update(file='../outside.dump'),
                     lambda x: x['databases'][0].update(database='surub_direct'),
                     lambda x: x.update(formatVersion=99)]
        for mutate in mutations:
            folder = self.create()
            self.manifest(folder, mutate)
            with self.assertRaises(ValueError):
                validate(folder)

    def test_symlink_archives_and_manifests_are_rejected(self):
        for filename in ('ordering.dump', 'manifest.json'):
            folder = self.create()
            path = folder / filename
            external = self.root / ('external-' + filename)
            path.rename(external)
            path.symlink_to(external)
            with self.assertRaises(ValueError):
                validate(folder)

    def test_dump_failure_never_publishes_a_complete_backup(self):
        with patch('backups.commands.ROOT', self.root), patch('backups.commands.postgres', side_effect=subprocess.CalledProcessError(1, ['pg_dump'])):
            with self.assertRaises(subprocess.CalledProcessError):
                create()
        folders = list((self.root / 'work/backups').iterdir())
        self.assertEqual(1, len(folders))
        self.assertTrue(folders[0].name.endswith('.partial'))
        self.assertFalse((folders[0] / 'manifest.json').exists())
        with self.assertRaises(ValueError):
            verify(folders[0])

    def test_invalid_postgres_archive_does_not_publish_even_when_hash_matches(self):
        def invalid(args, **kwargs):
            if args[0] == 'pg_restore':
                raise subprocess.CalledProcessError(1, args)
            self.postgres(args, **kwargs)
        with patch('backups.commands.ROOT', self.root), patch('backups.commands.postgres', invalid):
            with self.assertRaises(subprocess.CalledProcessError):
                create()
        self.assertTrue(all(x.name.endswith('.partial') for x in (self.root / 'work/backups').iterdir()))

    def test_separate_runs_never_overwrite_existing_backups(self):
        first, second = self.create(), self.create()
        self.assertNotEqual(first, second)
        self.assertEqual(digest(first / 'catalog.dump'), digest(second / 'catalog.dump'))

    def test_manifest_is_bounded_and_invalid_json_is_rejected(self):
        for text in ('x' * 16385, '{broken', '[]'):
            folder = self.create()
            (folder / 'manifest.json').write_text(text)
            with self.assertRaises(ValueError):
                validate(folder)
