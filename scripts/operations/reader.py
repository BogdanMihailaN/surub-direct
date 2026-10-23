"""Read recent local operational events without requiring live applications."""
import json
from collections import deque
from operations.collector import BACKUP_COUNT


def read_events(directory, limit=100, service=None):
    if not 1 <= limit <= 1000:
        raise ValueError('Limit must be between 1 and 1000.')
    if service not in (None, 'inventory', 'ordering'):
        raise ValueError('Unknown service.')
    result = deque(maxlen=limit)
    files = [directory / ('events.jsonl.' + str(i)) for i in range(BACKUP_COUNT, 0, -1)]
    files.append(directory / 'events.jsonl')
    for path in files:
        try:
            with path.open(encoding='utf-8') as source:
                for line in source:
                    try:
                        event = json.loads(line)
                    except ValueError:
                        continue
                    if isinstance(event, dict) and event.get('version') == 1 and (service is None or event.get('service') == service):
                        result.append(event)
        except FileNotFoundError:
            continue
    return list(result)
