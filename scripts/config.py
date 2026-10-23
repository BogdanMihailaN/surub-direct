"""Local configuration, passed only to the process that needs it."""
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICES = {
    'catalog': ('backend/src/services/catalog/SurubDirect.Catalog.Api', 5101),
    'inventory': ('backend/src/services/inventory/SurubDirect.Inventory.Api', 5102),
    'identity': ('backend/src/services/identity/SurubDirect.Identity.Api', 5103),
    'ordering': ('backend/src/services/ordering/SurubDirect.Ordering.Api', 5104),
    'purchasing': ('backend/src/services/purchasing/SurubDirect.Purchasing.Api', 5105),
    'gateway': ('backend/src/gateway/SurubDirect.Gateway', 5099),
    'worker': ('backend/src/services/catalog/SurubDirect.Catalog.Worker', None),
}


def settings():
    values = {}
    path = ROOT / '.env'
    if path.exists():
        for line in path.read_text().splitlines():
            if line and not line.startswith('#') and '=' in line:
                name, value = line.split('=', 1)
                values[name] = value
    return values


def environment(service=None):
    env = os.environ.copy()
    config = settings()
    env['PATH'] = os.pathsep.join([str(ROOT / '.tools/dotnet'), str(ROOT / '.tools/node-bin'), env.get('PATH', '')])
    if (ROOT / '.tools/dotnet/dotnet').exists():
        env['DOTNET_ROOT'] = str(ROOT / '.tools/dotnet')
    env.setdefault('DOTNET_CLI_TELEMETRY_OPTOUT', '1')
    env['ASPNETCORE_ENVIRONMENT'] = 'Development'
    env['DOTNET_ENVIRONMENT'] = 'Development'
    env['Logging__LogLevel__Microsoft.EntityFrameworkCore'] = 'Warning'
    env['STORE_API_URL'] = 'http://127.0.0.1:5099'
    socket = Path.home() / '.docker/run/docker.sock'
    if socket.exists():
        env.setdefault('DOCKER_HOST', 'unix://' + str(socket))
    owner = 'catalog' if service == 'worker' else service
    if owner in ('catalog', 'inventory', 'identity', 'ordering', 'purchasing'):
        password = config.get(owner.upper() + '_DB_PASSWORD', '')
        env['ConnectionStrings__' + owner.title()] = (
            f'Host=127.0.0.1;Port=55432;Database=surub_{owner};Username=surub_{owner};Password={password}')
    keys = {
        'catalog': ('CATALOG', 'INVENTORY', 'IDENTITY'),
        'inventory': ('CATALOG', 'INVENTORY', 'IDENTITY', 'RESERVATIONS'),
        'identity': ('IDENTITY',),
        'ordering': ('RESERVATIONS', 'COORDINATOR', 'IDENTITY'),
        'purchasing': ('CATALOG', 'IDENTITY', 'INVENTORY'),
    }
    for key in keys.get(service, ()):
        env['ServiceKeys__' + key.title()] = config.get(key + '_SERVICE_KEY', '')
    if service in ('inventory', 'ordering'):
        env['Logging__Console__FormatterName'] = 'json'
        env['Logging__Console__FormatterOptions__IncludeScopes'] = 'false'
        env['Logging__Console__FormatterOptions__UseUtcTimestamp'] = 'true'
        env['Logging__Console__FormatterOptions__TimestampFormat'] = "yyyy-MM-ddTHH:mm:ss.fffZ"
        password = config.get(service.upper() + '_BROKER_PASSWORD')
        if password:
            env['Messaging__Enabled'] = 'true'
            env['Messaging__Host'] = '127.0.0.1'
            env['Messaging__Port'] = '5672'
            env['Messaging__VirtualHost'] = 'surub-direct'
            env['Messaging__Username'] = 'surub_' + service
            env['Messaging__Password'] = password
    if service == 'catalog':
        env['Media__Root'] = str(ROOT / 'work/media/catalog')
    if service == 'identity':
        env['Admin__Email'] = config.get('ADMIN_EMAIL', '')
        env['Admin__Password'] = config.get('ADMIN_PASSWORD', '')
    if service in SERVICES and SERVICES[service][1]:
        env['ASPNETCORE_URLS'] = 'http://127.0.0.1:' + str(SERVICES[service][1])
    return env
