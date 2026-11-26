#!/usr/bin/env python3
"""Readable, compact, lossless whitespace formatting for SlideScramble JSON.

Usage:
  python3 slidescramble_pretty.py save.json
  python3 slidescramble_pretty.py challenges.json --output challenges.pretty.json

Rules: grid rows, move records, and short scalar arrays appear on one line;
operations and challenge entries retain multi-line structure. JSON data is
unchanged (apart from JSON object whitespace), including key order.
"""
import argparse
import json
from pathlib import Path

INDENT = 2


def compact(value):
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"))


def pretty(value, level=0, parent_key=None):
    """Format a parsed object without modifying any of its values."""
    pad = " " * (INDENT * level)
    child_pad = " " * (INDENT * (level + 1))
    if isinstance(value, dict):
        if not value:
            return "{}"
        # Move records and operation parameters are short and best read in one line.
        if parent_key in ("moves", "parameters"):
            return compact(value)
        fields = []
        for key, item in value.items():
            fields.append(child_pad + json.dumps(key, ensure_ascii=False) + ": "
                          + pretty(item, level + 1, key))
        return "{\n" + ",\n".join(fields) + "\n" + pad + "}"
    if isinstance(value, list):
        if not value:
            return "[]"
        # Every initialCells row and every move object occupies exactly one line.
        # Arrays of scalars (coordinates, operation permissions) are also compact.
        if parent_key in ("initialCells", "moves") and all(
                isinstance(element, list if parent_key == "initialCells" else dict)
                for element in value):
            return "[\n" + ",\n".join(
                child_pad + compact(element) for element in value) + "\n" + pad + "]"
        if all(not isinstance(item, (list, dict)) for item in value):
            return compact(value)
        return "[\n" + ",\n".join(
            child_pad + pretty(item, level + 1, parent_key if parent_key == "moves" else None)
            for item in value) + "\n" + pad + "]"
    return compact(value)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    parser.add_argument("--output", type=Path, help="Output file; defaults to <stem>.pretty.json")
    args = parser.parse_args()
    target = args.output or args.input.with_name(args.input.stem + ".pretty.json")
    if target.resolve() == args.input.resolve():
        parser.error("Refusing to overwrite input; provide a different --output path.")
    data = json.loads(args.input.read_text(encoding="utf-8"))
    target.write_text(pretty(data) + "\n", encoding="utf-8")
    assert json.loads(target.read_text(encoding="utf-8")) == data
    print(f"Wrote {target}")


if __name__ == "__main__":
    main()
