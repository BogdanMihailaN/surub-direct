"""Validate the fixed local backup format before handing archives to PostgreSQL."""
import hashlib
import json
from pathlib import Path

DATABASES = ('catalog', 'inventory', 'identity', 'ordering', 'purchasing')


def digest(path):
    result = hashlib.sha256()
    with path.open('rb') as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b''):
            result.update(chunk)
    return result.hexdigest()


def validate(directory):
    directory = Path(directory)
    manifest = directory / 'manifest.json'
    if directory.is_symlink() or manifest.is_symlink() or not manifest.is_file():
        raise ValueError('A completed backup directory with a regular manifest is required.')
    if manifest.stat().st_size > 16384:
        raise ValueError('Backup manifest is too large.')
    data = json.loads(manifest.read_text())
    if not isinstance(data, dict) or data.get('formatVersion') != 1 or data.get('consistency') != 'per-database':
        raise ValueError('Unsupported backup manifest.')
    entries = data.get('databases')
    if not isinstance(entries, list) or len(entries) != len(DATABASES):
        raise ValueError('The backup must contain all five service databases.')
    expected = {owner + '.dump' for owner in DATABASES}
    seen = set()
    for entry in entries:
        if not isinstance(entry, dict):
            raise ValueError('Invalid database entry.')
        name = entry.get('file')
        if not isinstance(name, str) or name not in expected or name in seen:
            raise ValueError('Unexpected or duplicate archive name.')
        seen.add(name)
        if entry.get('database') != 'surub_' + name.removesuffix('.dump'):
            raise ValueError('Database/archive mapping does not match.')
        path = directory / name
        if path.is_symlink() or not path.is_file():
            raise ValueError('An archive is missing or is not a regular file.')
        if type(entry.get('bytes')) is not int or entry['bytes'] <= 0 or path.stat().st_size != entry['bytes']:
            raise ValueError('Archive size does not match the manifest.')
        if entry.get('sha256') != digest(path):
            raise ValueError('Archive checksum does not match the manifest.')
    return data
