"""Bounded local JSONL sink. General logs, scopes and exception messages are excluded."""
import json
import fcntl
import logging
import os
from logging.handlers import RotatingFileHandler
from operations.events import parse_event

MAX_BYTES = 5 * 1024 * 1024
BACKUP_COUNT = 3


class PrivateRotatingHandler(RotatingFileHandler):
    def _open(self):
        fd = os.open(self.baseFilename, os.O_CREAT | os.O_APPEND | os.O_WRONLY, 0o600)
        os.fchmod(fd, 0o600)
        return os.fdopen(fd, 'a', encoding='utf-8')

    def handleError(self, record):
        # Let the supervisor report collector failure without stopping services.
        raise


class OperationalCollector:
    def __init__(self, directory, max_bytes=MAX_BYTES, backups=BACKUP_COUNT):
        directory.mkdir(parents=True, exist_ok=True, mode=0o700)
        directory.chmod(0o700)
        self.lock_fd = os.open(directory / '.collector.lock', os.O_CREAT | os.O_RDWR, 0o600)
        try:
            os.fchmod(self.lock_fd, 0o600)
            fcntl.flock(self.lock_fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
            self.handler = PrivateRotatingHandler(directory / 'events.jsonl',
                                                 maxBytes=max_bytes, backupCount=backups,
                                                 encoding='utf-8')
            self.handler.setFormatter(logging.Formatter('%(message)s'))
        except Exception:
            os.close(self.lock_fd)
            raise

    def record(self, service, line):
        event = parse_event(service, line)
        if event is None:
            return False
        record = logging.LogRecord('operations', logging.INFO, '', 0,
                                   json.dumps(event, ensure_ascii=False, separators=(',', ':')),
                                   (), None)
        self.handler.handle(record)
        return True

    def close(self):
        try:
            self.handler.close()
        finally:
            os.close(self.lock_fd)
