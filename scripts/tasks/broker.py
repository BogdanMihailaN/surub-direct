"""Provision the local broker and scoped service accounts without logging credentials."""
import base64
import json
import secrets
import subprocess
import urllib.request
from urllib.parse import quote
from config import ROOT, settings, environment


def provision_broker():
    config = settings()
    required = ('RABBITMQ_ADMIN_PASSWORD', 'INVENTORY_BROKER_PASSWORD', 'ORDERING_BROKER_PASSWORD')
    additions = [name + '=' + secrets.token_hex(32) for name in required if not config.get(name)]
    if additions:
        with (ROOT / '.env').open('a') as handle:
            handle.write('\n' + '\n'.join(additions) + '\n')
        (ROOT / '.env').chmod(0o600)
    subprocess.run(['docker', 'compose', 'up', '-d', '--wait', 'rabbitmq'], cwd=ROOT, env=environment(), check=True)
    config = settings()
    token = base64.b64encode(('surub_admin:' + config['RABBITMQ_ADMIN_PASSWORD']).encode()).decode()

    def put(path, payload):
        request = urllib.request.Request('http://127.0.0.1:15672/api/' + path,
            data=json.dumps(payload).encode(), method='PUT',
            headers={'Authorization': 'Basic ' + token, 'Content-Type': 'application/json'})
        with urllib.request.urlopen(request, timeout=10) as response:
            response.read()

    vhost = 'surub-direct'
    put('vhosts/' + quote(vhost, safe=''), {})
    exchange = r'inventory\.reservations\.v1'
    queues = r'ordering\.reservation-outcomes\.v1(\.dead)?'
    resources = '^(' + exchange + '|' + queues + ')$'
    for service in ('inventory', 'ordering'):
        user = 'surub_' + service
        put('users/' + user, {'password': config[service.upper() + '_BROKER_PASSWORD'], 'tags': ''})
        # Bindings need read access to their exchange and write access to their queue.
        read = '^' + exchange + '$' if service == 'inventory' else resources
        put('permissions/' + quote(vhost, safe='') + '/' + user,
            {'configure': resources, 'write': resources, 'read': read})
    print('RabbitMQ ready; Inventory and Ordering have separate scoped accounts.')
