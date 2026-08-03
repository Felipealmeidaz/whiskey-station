#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
# SPDX-License-Identifier: AGPL-3.0-or-later

"""Audita a paridade de IDs e variáveis entre en-US e pt-BR.

Uso: python3 Tools/localization/audit_ptbr.py [--fail-on-missing]
"""

from __future__ import annotations

import argparse
import re
from collections import defaultdict
from pathlib import Path


MESSAGE = re.compile(r"^(-?[A-Za-z][A-Za-z0-9_-]*)\s*=")
ATTRIBUTE = re.compile(r"^\s+\.([A-Za-z][A-Za-z0-9_-]*)\s*=")
VARIABLE = re.compile(r"\$([A-Za-z][A-Za-z0-9_-]*)")


def entries(path: Path) -> dict[str, set[str]]:
    """Returns Fluent message/attribute IDs and the variables used by each."""
    result: dict[str, set[str]] = {}
    current: str | None = None
    message_id: str | None = None

    for line in path.read_text(encoding="utf-8").splitlines():
        if line.lstrip().startswith("#"):
            continue

        message = MESSAGE.match(line)
        if message:
            message_id = message.group(1)
            current = message_id
            result[current] = set(VARIABLE.findall(line))
            continue

        attribute = ATTRIBUTE.match(line)
        if attribute and message_id:
            current = f"{message_id}.{attribute.group(1)}"
            result[current] = set(VARIABLE.findall(line))
            continue

        if current:
            result[current].update(VARIABLE.findall(line))

    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fail-on-missing", action="store_true",
                        help="return a non-zero exit status if any source ID is absent")
    parser.add_argument("--verbose", action="store_true",
                        help="list every missing ID, file and variable mismatch")
    args = parser.parse_args()

    root = Path(__file__).resolve().parents[2]
    source_root = root / "Resources" / "Locale" / "en-US"
    target_root = root / "Resources" / "Locale" / "pt-BR"
    source_files = sorted(path for path in source_root.rglob("*.ftl") if path.is_file())

    missing_files: list[Path] = []
    missing_entries: dict[Path, list[str]] = defaultdict(list)
    variable_mismatches: dict[Path, list[str]] = defaultdict(list)
    source_total = translated_total = 0

    for source in source_files:
        relative = source.relative_to(source_root)
        target = target_root / relative
        source_entries = entries(source)
        source_total += len(source_entries)

        if not target.exists():
            missing_files.append(relative)
            continue

        target_entries = entries(target)
        translated_total += len(set(source_entries).intersection(target_entries))
        for key, source_variables in source_entries.items():
            if key not in target_entries:
                missing_entries[relative].append(key)
                continue
            if source_variables != target_entries[key]:
                variable_mismatches[relative].append(
                    f"{key}: en-US={sorted(source_variables)}, pt-BR={sorted(target_entries[key])}")

    missing_count = source_total - translated_total
    coverage = (translated_total / source_total * 100) if source_total else 100.0
    print(f"PT-BR: {translated_total}/{source_total} IDs presentes ({coverage:.2f}%).")
    print(f"Arquivos ausentes: {len(missing_files)}; IDs ausentes: {missing_count}; "
          f"variáveis divergentes: {sum(map(len, variable_mismatches.values()))}.")

    if args.verbose:
        for relative, keys in sorted(missing_entries.items()):
            print(f"FALTANDO {relative}: {', '.join(keys)}")
        for relative, mismatches in sorted(variable_mismatches.items()):
            for mismatch in mismatches:
                print(f"VARIÁVEL {relative}: {mismatch}")

        if missing_files:
            print("ARQUIVOS AUSENTES:")
            print("\n".join(str(path) for path in missing_files))

    return 1 if args.fail_on_missing and (missing_count or variable_mismatches) else 0


if __name__ == "__main__":
    raise SystemExit(main())
