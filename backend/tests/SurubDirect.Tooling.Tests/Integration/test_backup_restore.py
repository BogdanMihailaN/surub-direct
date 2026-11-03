"""Opt-in recovery rehearsal against local Docker; creates only unique temporary databases."""
import os
import subprocess
import sys
import unittest
from pathlib import Path
from uuid import uuid4

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / 'scripts'))
from backups.commands import postgres, verify


@unittest.skipUnless(os.environ.get('LOCAL_BACKUP_DIR'), 'Set LOCAL_BACKUP_DIR to a trusted local backup')
class BackupRestoreTests(unittest.TestCase):
    def sql(self, database, sql):
        return postgres(['psql', '-X', '-v', 'ON_ERROR_STOP=1', '-U', 'surub_direct',
                         '-d', database, '-tA'], input=sql.encode(), stdout=subprocess.PIPE).stdout.decode().strip()

    def test_all_five_archives_restore_transactionally_under_the_service_role(self):
        directory = Path(os.environ['LOCAL_BACKUP_DIR'])
        manifest = verify(directory)
        for entry in manifest['databases']:
            with self.subTest(database=entry['database']):
                # The destination is generated here, never supplied by a manifest or caller.
                target = 'surub_restore_test_' + uuid4().hex
                role = entry['database']  # Fixed allowlist already enforced by verify().
                self.sql('postgres', f'CREATE DATABASE {target} OWNER {role};')
                try:
                    self.sql('postgres', f'REVOKE ALL ON DATABASE {target} FROM PUBLIC;')
                    with (directory / entry['file']).open('rb') as archive:
                        postgres(['pg_restore', '-U', 'surub_direct', '--dbname', target,
                                  '--role', role, '--no-owner', '--no-privileges',
                                  '--exit-on-error', '--single-transaction'],
                                 stdin=archive, stdout=subprocess.DEVNULL)
                    tables = int(self.sql(target, "SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema');"))
                    self.assertGreater(tables, 0)
                    foreign_owners = int(self.sql(target, "SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog', 'information_schema') AND tableowner <> '" + role + "';"))
                    self.assertEqual(0, foreign_owners)
                finally:
                    # Only this test's newly created database is removed, even on restore failure.
                    self.sql('postgres', f'DROP DATABASE {target};')


if __name__ == '__main__':
    unittest.main()
