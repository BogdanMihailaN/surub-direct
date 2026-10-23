"""Drain child output continuously; collector outages must not stop a business service."""
import sys


def forward_output(service, stream, collector):
    failed = False
    for line in stream:
        # Preserve the existing console output as well as the allowlisted local sink.
        try:
            sys.stdout.write(line)
            sys.stdout.flush()
        except (BrokenPipeError, OSError):
            pass
        if collector is None:
            continue
        try:
            stored = collector.record(service, line)
            if failed and stored:
                print('Operational collector recovered.', file=sys.stderr)
                failed = False
        except (OSError, ValueError):
            if not failed:
                print('Operational collector unavailable; local events may be lost. Services continue.',
                      file=sys.stderr)
                failed = True
