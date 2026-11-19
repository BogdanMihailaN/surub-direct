"""Start only isolated Catalog and Inventory instances on restored databases."""
import json
import os
import socket
import subprocess
import time
import urllib.error
import urllib.request
from contextlib import contextmanager
from pathlib import Path
from config import ROOT, SERVICES, environment


@contextmanager
def restored_api(name, database, **overrides):
    with socket.socket() as listener:
        listener.bind(('127.0.0.1', 0))
        port = listener.getsockname()[1]
    origin = f'http://127.0.0.1:{port}'
    env = environment(name)
    key = 'ConnectionStrings__' + name.title()
    env[key] = env[key].replace(f'Database=surub_{name};', f'Database={database};', 1)
    if f'Database={database};' not in env[key]:
        raise ValueError('The rehearsal must use a temporary database.')
    env.update(overrides, ASPNETCORE_URLS=origin)
    source = ROOT / SERVICES[name][0]
    build_root = Path(os.environ.get('RECOVERY_BUILD_ROOT', str(ROOT)))
    project = build_root / SERVICES[name][0]
    dll = project / 'bin/Debug/net10.0' / (project.name + '.dll')
    child = subprocess.Popen(['dotnet', str(dll)], env=env, cwd=source,
                             stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    try:
        for _ in range(50):
            if child.poll() is not None:
                raise RuntimeError(f'{name} stopped during recovery rehearsal.')
            try:
                # A response of any status here confirms the private HTTP listener.
                with urllib.request.urlopen(origin + '/', timeout=1):
                    break
            except urllib.error.HTTPError:
                break
            except OSError:
                time.sleep(0.2)
        else:
            raise RuntimeError(f'{name} did not start for recovery rehearsal.')
        yield origin
    finally:
        if child.poll() is None:
            child.terminate()
            try:
                child.wait(timeout=10)
            except subprocess.TimeoutExpired:
                child.kill()
                child.wait()


def restored_catalog(origin):
    with urllib.request.urlopen(origin + '/api/v1/products?pageSize=1', timeout=10) as response:
        return json.load(response)
