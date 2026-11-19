"""Archive and restore only Catalog's generated full-size and thumbnail files."""
import re
import shutil
import tarfile
from pathlib import Path


MEDIA_FILE = 'catalog-media.tar'
IMAGE_NAME = re.compile(r'[0-9a-f]{32}(?:-thumb)?\.webp')


def archive_media(root, output):
    root = Path(root)
    if root.is_symlink():
        raise ValueError('Catalog media root must be a regular directory.')
    with tarfile.open(fileobj=output, mode='w') as archive:
        if not root.exists():
            return
        for path in sorted(root.iterdir()):
            if not IMAGE_NAME.fullmatch(path.name):
                continue
            if path.is_symlink() or not path.is_file():
                raise ValueError('Catalog media contains an invalid image path.')
            archive.add(path, arcname=path.name, recursive=False)


def read_media(path, destination=None):
    names = set()
    with tarfile.open(path, 'r:') as archive:
        for entry in archive:
            if (not entry.isfile() or not IMAGE_NAME.fullmatch(entry.name) or
                    entry.name in names or entry.size <= 0):
                raise ValueError('Invalid or duplicate Catalog media entry.')
            names.add(entry.name)
            with archive.extractfile(entry) as source:
                if destination is None:
                    while source.read(1024 * 1024):
                        pass
                else:
                    with (Path(destination) / entry.name).open('xb') as output:
                        shutil.copyfileobj(source, output)
    return names
