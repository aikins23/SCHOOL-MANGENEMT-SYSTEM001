#!/usr/bin/env python3
"""Rewrite selected redacted secret fingerprints from an isolated Git mirror."""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import subprocess
import sys


REPLACEMENT = b"RETIRED_SECRET_REMOVED"
SQL_PASSWORD = re.compile(
    rb"(?i)(^|;)(\s*(?:Password|Pwd)\s*=\s*)(?P<secret>[^;'\"&<>\s]+)"
)
QUOTED_ASSIGNMENT = re.compile(
    rb"(?im)^(?P<prefix>\s*(?:(?:const|readonly|static|var|string)\s+)*"
    rb"(?:[\"'])?(?P<key>[A-Za-z0-9_.-]*(?:password|secret|api[_-]?key|"
    rb"access[_-]?token|private[_-]?key)[A-Za-z0-9_.-]*)(?:[\"'])?\s*[:=]\s*[\"'])"
    rb"(?P<secret>[^\"'\r\n]+)(?P<suffix>[\"'])"
)
CREDENTIAL_URI = re.compile(
    rb"(?i)(?P<prefix>(?:sqlserver|postgres(?:ql)?|mysql)://[^:/\s]+:)"
    rb"(?P<secret>[^@/\s]+)(?P<suffix>@)"
)


def fingerprint(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest().upper()[:12]


def replace_targeted(match: re.Match[bytes], targets: set[str]) -> bytes:
    secret = match.group("secret").strip()
    if fingerprint(secret) not in targets:
        return match.group(0)

    full = match.group(0)
    start = match.start("secret") - match.start(0)
    end = match.end("secret") - match.start(0)
    return full[:start] + REPLACEMENT + full[end:]


def sanitize_blob(data: bytes, targets: set[str]) -> tuple[bytes, int]:
    replacements = 0

    def replace(match: re.Match[bytes]) -> bytes:
        nonlocal replacements
        updated = replace_targeted(match, targets)
        if updated != match.group(0):
            replacements += 1
        return updated

    data = SQL_PASSWORD.sub(replace, data)
    data = QUOTED_ASSIGNMENT.sub(replace, data)
    data = CREDENTIAL_URI.sub(replace, data)
    return data, replacements


def rewrite_fast_export(
    source: subprocess.Popen[bytes],
    destination: subprocess.Popen[bytes],
    targets: set[str],
) -> tuple[int, int]:
    assert source.stdout is not None
    assert destination.stdin is not None

    blob_pending = False
    changed_blobs = 0
    replacement_count = 0

    def read_exact(size: int) -> bytes:
        chunks: list[bytes] = []
        remaining = size
        while remaining:
            chunk = source.stdout.read(remaining)
            if not chunk:
                raise RuntimeError("Unexpected end of git fast-export data block")
            chunks.append(chunk)
            remaining -= len(chunk)
        return b"".join(chunks)

    while True:
        line = source.stdout.readline()
        if not line:
            break

        if line == b"blob\n":
            blob_pending = True
            destination.stdin.write(line)
            continue

        if line.startswith(b"data "):
            size = int(line[5:].strip())
            payload = read_exact(size)
            # Git permits the LF after a counted data block to be omitted.
            if source.stdout.peek(1)[:1] == b"\n":
                source.stdout.read(1)

            if blob_pending:
                payload, replacements = sanitize_blob(payload, targets)
                if replacements:
                    changed_blobs += 1
                    replacement_count += replacements

            destination.stdin.write(f"data {len(payload)}\n".encode("ascii"))
            destination.stdin.write(payload)
            destination.stdin.write(b"\n")
            blob_pending = False
            continue

        destination.stdin.write(line)

    destination.stdin.close()
    return changed_blobs, replacement_count


def run(command: list[str], *, capture: bool = False) -> str:
    result = subprocess.run(
        command,
        check=True,
        stdout=subprocess.PIPE if capture else None,
        stderr=subprocess.PIPE if capture else None,
        text=True,
    )
    return result.stdout.strip() if capture else ""


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=pathlib.Path)
    parser.add_argument("--destination", required=True, type=pathlib.Path)
    parser.add_argument("--manifest", required=True, type=pathlib.Path)
    args = parser.parse_args()

    source = args.source.resolve()
    destination = args.destination.resolve()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    targets = {item["fingerprint"].upper() for item in manifest["retiredSecrets"]}

    if destination.exists():
        raise RuntimeError(f"Destination already exists: {destination}")
    if not source.exists():
        raise RuntimeError(f"Source mirror does not exist: {source}")
    if not targets:
        raise RuntimeError("The retired-secret manifest is empty")

    run(["git", "init", "--bare", str(destination)])

    exporter = subprocess.Popen(
        ["git", "-C", str(source), "fast-export", "--all", "--signed-tags=strip"],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    importer = subprocess.Popen(
        ["git", "-C", str(destination), "fast-import", "--force", "--quiet"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )

    try:
        changed_blobs, replacement_count = rewrite_fast_export(exporter, importer, targets)
    except Exception:
        exporter.kill()
        importer.kill()
        raise

    export_error = exporter.stderr.read().decode("utf-8", errors="replace") if exporter.stderr else ""
    import_output = importer.stdout.read().decode("utf-8", errors="replace") if importer.stdout else ""
    import_error = importer.stderr.read().decode("utf-8", errors="replace") if importer.stderr else ""
    export_code = exporter.wait()
    import_code = importer.wait()
    if export_code != 0:
        raise RuntimeError(f"git fast-export failed: {export_error}")
    if import_code != 0:
        raise RuntimeError(f"git fast-import failed: {import_error}\n{import_output}")
    if replacement_count == 0:
        raise RuntimeError("No targeted retired-secret fingerprints were found")

    run(["git", "-C", str(destination), "fsck", "--full", "--strict"])
    remote = run(
        ["git", "-C", str(source), "config", "--get", "remote.origin.url"],
        capture=True,
    )
    if remote:
        run(["git", "-C", str(destination), "remote", "add", "origin", remote])

    print(f"Rewritten blobs: {changed_blobs}")
    print(f"Retired secret occurrences replaced: {replacement_count}")
    print(f"Clean mirror: {destination}")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"History rewrite failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
