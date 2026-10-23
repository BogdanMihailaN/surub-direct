"""Provision isolated local databases; copy legacy schemas only into empty targets."""
import secrets
import subprocess
from datetime import datetime
from pathlib import Path
from config import ROOT, environment, settings

OWNERS = {'catalog': ('catalog', 'pricing'), 'inventory': ('inventory',), 'identity': ('identity',), 'ordering': (), 'purchasing': ()}


def run(args, **kwargs):
    return subprocess.run(args, cwd=ROOT, env=environment(), check=True, **kwargs)


def psql(database, sql):
    result = run(['docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-X', '-v', 'ON_ERROR_STOP=1', '-U', 'surub_direct', '-d', database, '-tA'],
                 input=sql, text=True, capture_output=True)
    return result.stdout.strip()


def prepare_secrets():
    config = settings()
    path = ROOT / '.env'
    if not path.exists():
        path.write_text('POSTGRES_PASSWORD=' + secrets.token_hex(24) + '\nADMIN_EMAIL=admin@surubdirect.local\nADMIN_PASSWORD=' + secrets.token_hex(24) + 'aA!1\n')
    additions = []
    for name in ['RABBITMQ_ADMIN_PASSWORD'] + [s.upper() + '_DB_PASSWORD' for s in OWNERS] + [s + '_SERVICE_KEY' for s in ('CATALOG', 'INVENTORY', 'IDENTITY', 'RESERVATIONS', 'COORDINATOR')]:
        if not config.get(name):
            additions.append(name + '=' + secrets.token_hex(32))
    if additions:
        with path.open('a') as handle:
            handle.write('\n' + '\n'.join(additions) + '\n')
    path.chmod(0o600)


def provision():
    prepare_secrets()
    run(['docker', 'compose', 'up', '-d', '--wait', 'postgres'])
    config = settings()
    for owner in OWNERS:
        name = 'surub_' + owner
        password = config[owner.upper() + '_DB_PASSWORD'].replace("'", "''")
        exists = psql('surub_direct', f"SELECT 1 FROM pg_roles WHERE rolname='{name}';")
        if not exists:
            psql('surub_direct', f"CREATE ROLE {name} LOGIN PASSWORD '{password}' NOSUPERUSER NOCREATEDB NOCREATEROLE;")
        if not psql('surub_direct', f"SELECT 1 FROM pg_database WHERE datname='{name}';"):
            psql('surub_direct', f'CREATE DATABASE {name} OWNER {name};')
        psql('surub_direct', f'REVOKE ALL ON DATABASE {name} FROM PUBLIC; GRANT CONNECT ON DATABASE {name} TO {name};')
    # The original database remains a recoverable legacy copy, not a shared runtime store.
    psql('surub_direct', 'REVOKE CONNECT ON DATABASE surub_direct FROM PUBLIC;')
    print(f'{len(OWNERS)} databases provisioned with separate owners and connection permissions.')


def migrate_legacy():
    """Run with the former monolith stopped. Never merge into or overwrite nonempty targets."""
    provision()
    backup = ROOT / 'work/backups' / datetime.now().strftime('%Y%m%d-%H%M%S')
    backup.mkdir(parents=True, exist_ok=False)
    for owner, schemas in OWNERS.items():
        if not schemas:
            continue
        source_exists = psql('surub_direct', "SELECT count(*) FROM pg_namespace WHERE nspname IN (" + ','.join("'" + s + "'" for s in schemas) + ');')
        target = 'surub_' + owner
        target_tables = psql(target, "SELECT count(*) FROM pg_tables WHERE schemaname NOT IN ('pg_catalog','information_schema');")
        if source_exists == '0' or target_tables != '0':
            print(f'{owner}: no legacy schemas or target already initialized; skipped.')
            continue
        dump = backup / (owner + '.sql')
        args = ['docker', 'compose', 'exec', '-T', 'postgres', 'pg_dump', '-U', 'surub_direct', '-d', 'surub_direct', '--no-owner', '--no-privileges']
        for schema in schemas:
            args += ['--schema', schema]
        with dump.open('wb') as handle:
            run(args, stdout=handle)
        dump.chmod(0o600)
        # SET ROLE makes every restored object owned by the service, not the provisioner.
        sql = f'SET ROLE {target};\n' + dump.read_text()
        run(['docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-X', '-1', '-v', 'ON_ERROR_STOP=1', '-U', 'surub_direct', '-d', target], input=sql, text=True, capture_output=True)
        for schema in schemas:
            tables = psql('surub_direct', f"SELECT tablename FROM pg_tables WHERE schemaname='{schema}';").splitlines()
            for table in tables:
                query = f'SELECT count(*) FROM "{schema}"."{table}";'
                if psql('surub_direct', query) != psql(target, query):
                    raise RuntimeError(f'Row count mismatch: {schema}.{table}')
        print(f'{owner}: copied schemas and verified every table row count.')
    print('Legacy database preserved. Private backups saved in work/backups.')


def verify_isolation():
    for owner in OWNERS:
        for target in OWNERS:
            allowed = psql('surub_direct', f"SELECT has_database_privilege('surub_{owner}', 'surub_{target}', 'CONNECT');")
            if (allowed == 't') != (owner == target):
                raise RuntimeError(f'Unexpected connection permission: {owner} -> {target}')
        if psql('surub_direct', f"SELECT has_database_privilege('surub_{owner}', 'surub_direct', 'CONNECT');") != 'f':
            raise RuntimeError(f'{owner} can connect to the legacy database')
        print(f'{owner}: own database allowed, other service databases and legacy database denied.')
