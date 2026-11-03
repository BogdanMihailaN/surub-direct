"""Read-only PostgreSQL backup operations; never restore into application databases."""
import json
import os
import subprocess
from datetime import datetime, timezone
from pathlib import Path
from uuid import uuid4
from config import ROOT, environment
from backups.manifest import DATABASES, digest, validate


def postgres(args, **kwargs):
    return subprocess.run(['docker', 'compose', 'exec', '-T', 'postgres', *args],
                          cwd=ROOT, env=environment(), check=True, stderr=subprocess.PIPE, **kwargs)


def private_file(path):
    return os.fdopen(os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600), 'wb')


def verify(directory):
    if Path(directory).name.endswith('.partial'):
        raise ValueError('Backup creation did not complete; partial directories cannot be verified as backups.')
    return verify_contents(directory)


def verify_contents(directory):
    data = validate(directory)
    for entry in data['databases']:
        with (Path(directory) / entry['file']).open('rb') as archive:
            postgres(['pg_restore', '--list'], stdin=archive, stdout=subprocess.DEVNULL)
    return data


def create():
    parent = ROOT / 'work/backups'
    parent.mkdir(parents=True, exist_ok=True)
    name = 'services-' + datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ') + '-' + uuid4().hex[:8]
    pending = parent / (name + '.partial')
    pending.mkdir(mode=0o700)
    data = {'formatVersion': 1, 'createdAt': datetime.now(timezone.utc).isoformat(),
            'consistency': 'per-database', 'databases': []}
    # A failure leaves a private .partial directory, never a completed backup.
    for owner in DATABASES:
        archive = pending / (owner + '.dump')
        with private_file(archive) as output:
            postgres(['pg_dump', '-U', 'surub_direct', '-d', 'surub_' + owner,
                      '--format=custom'], stdout=output)
            output.flush()
            os.fsync(output.fileno())
        data['databases'].append({'database': 'surub_' + owner, 'file': archive.name,
                                  'bytes': archive.stat().st_size, 'sha256': digest(archive)})
    with private_file(pending / 'manifest.json') as output:
        output.write((json.dumps(data, indent=2) + '\n').encode())
        output.flush()
        os.fsync(output.fileno())
    verify_contents(pending)
    target = parent / name
    pending.rename(target)
    return target
