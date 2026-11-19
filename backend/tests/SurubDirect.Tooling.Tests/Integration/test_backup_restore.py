"""Opt-in recovery rehearsal against local Docker; creates only unique temporary databases."""
import os
import subprocess
import sys
import tempfile
import urllib.request
from contextlib import ExitStack
import unittest
from pathlib import Path
from uuid import uuid4

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / 'scripts'))
from backups.commands import postgres, verify
from backups.media import MEDIA_FILE, read_media
from restore_rehearsal import restored_api, restored_catalog


@unittest.skipUnless(os.environ.get('LOCAL_BACKUP_DIR'), 'Set LOCAL_BACKUP_DIR to a trusted local backup')
class BackupRestoreTests(unittest.TestCase):
    def sql(self, database, sql):
        return postgres(['psql', '-X', '-v', 'ON_ERROR_STOP=1', '-U', 'surub_direct',
                         '-d', database, '-tA'], input=sql.encode(), stdout=subprocess.PIPE).stdout.decode().strip()

    def test_all_five_archives_restore_transactionally_under_the_service_role(self):
        directory = Path(os.environ['LOCAL_BACKUP_DIR'])
        manifest = verify(directory)
        with ExitStack() as resources:
            restored = {}
            for entry in manifest['databases']:
                with self.subTest(database=entry['database']):
                    target = 'surub_restore_test_' + uuid4().hex
                    role = entry['database']  # Fixed allowlist enforced by verify().
                    self.sql('postgres', f'CREATE DATABASE {target} OWNER {role};')
                    resources.callback(self.sql, 'postgres', f'DROP DATABASE {target};')
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
                    restored[role] = target

            if manifest['formatVersion'] == 2:
                self.verify_restored_catalog(directory, restored, resources)

    def verify_restored_catalog(self, directory, restored, resources):
        media = Path(resources.enter_context(tempfile.TemporaryDirectory(prefix='surub-restored-media-')))
        names = read_media(directory / MEDIA_FILE, media)
        catalog_db = restored['surub_catalog']
        images = self.sql(catalog_db, """
            SELECT i."Id" || '|' || i."ProductId" || '|' || p."Status"
            FROM catalog."ProductImages" i JOIN catalog."Products" p ON p."Id" = i."ProductId"
            ORDER BY i."Id";
        """).splitlines()
        for image in images:
            image_id, _, _ = image.split('|')
            stem = image_id.replace('-', '')
            self.assertIn(stem + '.webp', names)
            self.assertIn(stem + '-thumb.webp', names)

        inventory = resources.enter_context(restored_api('inventory', restored['surub_inventory'], Messaging__Enabled='false'))
        origin = resources.enter_context(restored_api('catalog', catalog_db, Services__Inventory=inventory, Media__Root=str(media)))
        expected = int(self.sql(catalog_db, 'SELECT count(*) FROM catalog."Products" WHERE "Status" = 1;'))
        self.assertEqual(expected, restored_catalog(origin)['total'])
        published = next((image for image in images if image.endswith('|1')), None)
        if published:
            image_id, product_id, _ = published.split('|')
            for size, suffix in [('full', '.webp'), ('thumbnail', '-thumb.webp')]:
                with urllib.request.urlopen(f'{origin}/api/v1/products/{product_id}/images/{image_id}/{size}', timeout=10) as response:
                    self.assertEqual('image/webp', response.headers.get_content_type())
                    self.assertEqual((media / (image_id.replace('-', '') + suffix)).read_bytes(), response.read())


if __name__ == '__main__':
    unittest.main()
