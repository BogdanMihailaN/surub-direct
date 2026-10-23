"""Allowlisted operational metadata from .NET JSON console output."""
import json
from datetime import datetime, timezone

ALERTS = {
    'inventory': {'outbox-unavailable', 'outbox-age-unknown', 'outbox-delayed'},
    'ordering': {'broker-probe-unavailable', 'broker-unavailable', 'ready-backlog',
                 'dead-letter', 'registry-unavailable', 'failures-awaiting-review',
                 'replays-awaiting-publication'},
}
ERROR_TYPES = {'TimeoutException', 'OperationCanceledException', 'TaskCanceledException',
               'DbUpdateException', 'PostgresException', 'NpgsqlException',
               'InvalidOperationException', 'IOException', 'SocketException'}
TRANSITIONS = {7100: ('Raised', 'Warning'), 7101: ('Reminder', 'Warning'),
               7102: ('Recovered', 'Information')}


def parse_event(service, line):
    if service not in ALERTS or len(line) > 65536:
        return None
    try:
        data = json.loads(line)
    except (ValueError, TypeError):
        return None
    if not isinstance(data, dict) or not isinstance(data.get('State'), dict):
        return None
    event_id, category, state = data.get('EventId'), data.get('Category'), data['State']
    if type(event_id) is not int:
        return None
    prefix = 'SurubDirect.' + service.title() + '.Api.Workers.'
    fields = {}
    if event_id in TRANSITIONS and category == prefix + 'MessagingMonitorWorker':
        transition, level = TRANSITIONS[event_id]
        code = state.get('AlertCode')
        if not isinstance(code, str) or code not in {service + '.' + name for name in ALERTS[service]}:
            return None
        if state.get('AlertState') != transition:
            return None
        fields = {'alertCode': code, 'alertState': transition}
    else:
        batch_id = 7200 if service == 'inventory' else 7300
        worker = 'OutboxRetentionWorker' if service == 'inventory' else 'FailedMessageRetentionWorker'
        if category != prefix + worker:
            return None
        if event_id == batch_id:
            level = 'Information'
            if type(state.get('DryRun')) is not bool:
                return None
            if any(type(state.get(key)) is not int or not 0 <= state[key] <= 1000
                   for key in ('Candidates', 'Removed')):
                return None
            if state['Removed'] > state['Candidates'] or (state['DryRun'] and state['Removed']):
                return None
            fields = {'dryRun': state['DryRun'], 'candidates': state['Candidates'], 'removed': state['Removed']}
        elif event_id == batch_id + 1:
            level = 'Warning'
            error_type = state.get('ErrorType')
            fields = {'errorType': error_type if isinstance(error_type, str) and error_type in ERROR_TYPES else 'OtherException'}
        else:
            return None
    if data.get('LogLevel') != level:
        return None
    received = datetime.now(timezone.utc).isoformat()
    timestamp = received
    try:
        parsed = datetime.fromisoformat(data.get('Timestamp', '').replace('Z', '+00:00'))
        if parsed.tzinfo is not None:
            timestamp = parsed.astimezone(timezone.utc).isoformat()
    except (ValueError, TypeError, AttributeError):
        pass
    return {'version': 1, 'timestamp': timestamp, 'receivedAt': received,
            'service': service, 'eventId': event_id, 'level': level, **fields}
