#!/usr/bin/env python3
"""Local entry point for independent services, Angular apps and verification."""
import argparse
import os
import signal
import subprocess
import sys
import time
from config import ROOT, SERVICES, environment
from tasks.broker import provision_broker
from tasks.database import provision, migrate_legacy, verify_isolation


def run(args, service=None, cwd=ROOT):
    subprocess.run(args, cwd=cwd, env=environment(service), check=True)


def command_for(service):
    return ['dotnet', 'run', '--no-build', '--no-launch-profile', '--project', SERVICES[service][0]]


def serve_all():
    children = []
    try:
        for service in ('identity', 'inventory', 'catalog', 'ordering', 'purchasing', 'gateway', 'worker'):
            children.append(subprocess.Popen(command_for(service), cwd=ROOT, env=environment(service), start_new_session=True))
        while all(p.poll() is None for p in children):
            time.sleep(1)
        raise RuntimeError('A service stopped. Check its log above.')
    finally:
        for child in children:
            if child.poll() is None:
                os.killpg(child.pid, signal.SIGTERM)
        for child in children:
            try:
                child.wait(timeout=10)
            except subprocess.TimeoutExpired:
                os.killpg(child.pid, signal.SIGKILL)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=[*SERVICES, 'api', 'up', 'db', 'migrate-legacy', 'init', 'seed-demo', 'storefront', 'admin', 'build', 'test', 'e2e', 'check-migrations', 'verify-isolation', 'broker'])
    args = parser.parse_args()
    command = 'gateway' if args.command == 'api' else args.command
    if command == 'broker':
        provision_broker()
    elif command == 'db':
        provision()
    elif command == 'migrate-legacy':
        migrate_legacy()
    elif command == 'verify-isolation':
        verify_isolation()
    elif command == 'up':
        serve_all()
    elif command in ('init', 'seed-demo'):
        for service in ('identity', 'inventory', 'catalog', 'purchasing', 'ordering'):
            extra = ['--', '--initialize']
            if command == 'seed-demo' and service == 'catalog':
                extra += ['--seed-demo']
            run(command_for(service) + extra, service)
    elif command in SERVICES:
        run(command_for(command), command)
    elif command in ('storefront', 'admin'):
        run(['npm', 'exec', '--', 'ng', 'serve', command], cwd=ROOT / 'frontend')
    elif command == 'build':
        run(['dotnet', 'build', 'backend/SurubDirect.sln', '--nologo'])
        run(['npm', 'run', 'build'], cwd=ROOT / 'frontend')
    elif command == 'test':
        run(['dotnet', 'test', 'backend/SurubDirect.sln', '--nologo'])
    elif command == 'e2e':
        run(['npm', 'exec', '--', 'playwright', 'test'], cwd=ROOT / 'frontend')
    elif command == 'check-migrations':
        for module, owner, context in [('Catalog', 'catalog', 'CatalogDbContext'), ('Pricing', 'catalog', 'PricingDbContext'), ('Inventory', 'inventory', 'InventoryDbContext'), ('Identity', 'identity', 'StoreIdentityDbContext'), ('Purchasing', 'purchasing', 'PurchasingDbContext'), ('Ordering', 'ordering', 'OrderingDbContext')]:
            project = f'backend/src/services/{owner}/SurubDirect.{module}.Infrastructure'
            run(['dotnet', 'ef', 'migrations', 'has-pending-model-changes', '--context', context, '--project', project, '--startup-project', SERVICES[owner][0]], owner)


if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        pass
    except subprocess.CalledProcessError as error:
        # Do not print subprocess input: provisioning input can contain secrets.
        print('Command failed with exit code', error.returncode, file=sys.stderr)
        sys.exit(error.returncode)
