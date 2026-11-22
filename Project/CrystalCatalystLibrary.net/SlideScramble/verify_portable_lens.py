#!/usr/bin/env python3
"""Reproduce the original 4x4 centered portable-lens verification fixture.

Symbols are TILE IDENTITIES, not fixed cell coordinates. Each glyph/direction
instruction rotates the entire CURRENT row/column containing that glyph.
An unmarked cell in the atlas retains its starting value; it is not empty.

No dependencies outside Python's standard library.
"""

SHAPES = {"○": (1, 1), "☆": (2, 1), "△": (1, 2), "□": (2, 2)}
START = [
    [(4, 4), (1, 4), (2, 4), (3, 4)],
    [(4, 1), (1, 1), (2, 1), (3, 1)],
    [(4, 2), (1, 2), (2, 2), (3, 2)],
    [(4, 3), (1, 3), (2, 3), (3, 3)],
]
LENS = {(1, 1), (2, 1), (1, 2), (2, 2)}


def moves(text):
    return [(token[0], token[1]) for token in text.split()]


COLUMN_SWAPS = {
    "Up left": moves("△↑ □↑ □← □↓ △→ △→ △↓ ☆←"),
    "Down left": moves("○↓ ☆↓ ☆← ☆↑ ○→ ○→ ○↑ □←"),
    "Up right": moves("□↑ △↑ △→ △↓ □← □← □↓ ○→"),
    "Down right": moves("☆↓ ○↓ ○→ ○↑ ☆← ☆← ☆↑ △→"),
}
ROW_SWAPS = {
    "Left up": moves("☆← □← □↑ □→ ☆↓ ☆↓ ☆→ △↑"),
    "Right up": moves("○→ △→ △↑ △← ○↓ ○↓ ○← ☆↑"),
    "Left down": moves("□← ☆← ☆↓ ☆→ □↑ □↑ □→ ○↓"),
    "Right down": moves("△→ ○→ ○↓ ○← △↑ △↑ △← □↓"),
}
# The dot means 'original value of this cell' in the handwritten diagram.
SHUFFLES = {
    "Lens.CircleSquare.A": ("○→ □↑ □← ○↓", "□○·☆"),
    "Lens.StarTriangle.A": ("☆← △↑ △→ ☆↓", "☆△○·"),
    "Lens.CircleSquare.B": ("○↓ □← □↑ ○→", "□·○△"),
    "Lens.StarTriangle.B": ("☆↓ △→ △↑ ☆←", "·△□☆"),
    "Lens.TriangleStar.A": ("△→ ☆↓ ☆← △↑", "·□☆△"),
    "Lens.SquareCircle.A": ("□← ○↓ ○→ □↑", "△·□○"),
    "Lens.TriangleStar.B": ("△↑ ☆← ☆↓ △→", "△○☆·"),
    "Lens.SquareCircle.B": ("□↑ ○→ ○↓ □←", "☆□·○"),
}


def verify_formula_symmetries():
    """Check the structural relationships described in PortableLens.md."""
    opposite = {"↑": "↓", "↓": "↑", "←": "→", "→": "←"}
    expected_symbol_pattern = (0, 1, 1, 1, 0, 0, 0, 2)
    for family, formulas in [("column", COLUMN_SWAPS), ("row", ROW_SWAPS)]:
        for name, seq in formulas.items():
            assert len(seq) == 8, (family, name, "length")
            x, y, z = seq[0][0], seq[1][0], seq[7][0]
            a, b = seq[0][1], seq[2][1]
            symbols = [x, y, z]
            expected = [
                (symbols[expected_symbol_pattern[i]], direction)
                for i, direction in enumerate([
                    a, a, b, opposite[a], opposite[b], opposite[b], opposite[a], b
                ])
            ]
            assert seq == expected, (family, name, seq, expected)
    for prefix in ["CircleSquare", "StarTriangle", "TriangleStar", "SquareCircle"]:
        a = moves(SHUFFLES[f"Lens.{prefix}.A"][0])
        b = moves(SHUFFLES[f"Lens.{prefix}.B"][0])
        assert [glyph for glyph, _ in a] == [glyph for glyph, _ in b], prefix
        assert [direction for _, direction in a] == [
            direction for _, direction in reversed(b)
        ], prefix
    print("PASS all eight swap formula patterns and all four A/B traversal symmetries.")


def verify_both_swaps():
    """Compose verified swap formulas with symbols rebound to lens positions."""
    original = [row.copy() for row in START]
    positions = [(1, 1), (2, 1), (1, 2), (2, 2)]
    expected = [
        [START[2][2], START[2][1]],
        [START[1][2], START[1][1]],
    ]

    def apply(board, seq):
        # Every swap formula is written for the Given arrangement: rebind
        # shapes to CURRENT cell contents before this complete formula.
        current = dict(zip("○☆△□", (board[y][x] for x, y in positions)))
        for glyph, direction in seq:
            identity = current[glyph]
            x, y = next((x, y) for y, row in enumerate(board)
                        for x, tile in enumerate(row) if tile == identity)
            if direction in "←→":
                shift = (1 if direction == "←" else -1) % len(board[y])
                board[y] = board[y][shift:] + board[y][:shift]
            else:
                column = [row[x] for row in board]
                shift = (1 if direction == "↑" else -1) % len(column)
                column = column[shift:] + column[:shift]
                for row, tile in zip(board, column):
                    row[x] = tile

    for names in [("Column", "Row"), ("Row", "Column")]:
        board = [row.copy() for row in original]
        for name in names:
            apply(board, COLUMN_SWAPS["Up left"] if name == "Column"
                  else ROW_SWAPS["Left up"])
        assert [board[1][1:3], board[2][1:3]] == expected, names
        assert all(board[y][x] == original[y][x]
                   for y in range(4) for x in range(4)
                   if (x, y) not in LENS), names
        # Both is a half turn (diagonal exchange), so doing it twice
        # must restore the original board and surrounding cells.
        for name in names:
            apply(board, COLUMN_SWAPS["Up left"] if name == "Column"
                  else ROW_SWAPS["Left up"])
        assert board == original, names
    print("PASS Both = Column+Row = Row+Column; Both twice = Given.")


def replay(sequence):
    board = [row.copy() for row in START]
    for glyph, direction in sequence:
        identity = SHAPES[glyph]
        x, y = next((x, y) for y, row in enumerate(board)
                    for x, value in enumerate(row) if value == identity)
        if direction in "←→":
            shift = 1 if direction == "←" else -1
            k = shift % len(board[y])
            board[y] = board[y][k:] + board[y][:k]
        else:
            shift = 1 if direction == "↑" else -1
            column = [row[x] for row in board]
            k = shift % len(column)
            column = column[k:] + column[:k]
            for row, value in zip(board, column):
                row[x] = value
    return board


def check(name, sequence, expected):
    board = replay(sequence)
    reverse = {identity: glyph for glyph, identity in SHAPES.items()}
    cells = [(1, 1), (2, 1), (1, 2), (2, 2)]
    for i, (x, y) in enumerate(cells):
        if expected[i] == "·":
            assert board[y][x] == START[y][x], (
                f"{name}: blank at {(x, y)} must retain the original value")
        else:
            assert reverse.get(board[y][x]) == expected[i], (
                f"{name}: expected {expected[i]} at {(x, y)}; found {board[y][x]}")
    for y, row in enumerate(board):
        for x, value in enumerate(row):
            if (x, y) not in LENS:
                assert value == START[y][x], (
                    f"{name}: outside-lens cell {(x, y)} changed")
    print(f"PASS {name}: {''.join(reverse.get(board[y][x], '?') for x, y in cells)}")


def verify_portable_positions(width, height):
    """Test all entries at every translated position, including wrapped corners."""
    patterns = (
        [(f"Column swap ({name})", seq, "☆○□△")
         for name, seq in COLUMN_SWAPS.items()]
        + [(f"Row swap ({name})", seq, "△□○☆")
           for name, seq in ROW_SWAPS.items()]
        + [(name, moves(spec), result)
           for name, (spec, result) in SHUFFLES.items()]
    )
    checked = 0
    symbols = list(SHAPES)
    for top in range(height):
        for left in range(width):
            cells = [
                (left, top),
                ((left + 1) % width, top),
                (left, (top + 1) % height),
                ((left + 1) % width, (top + 1) % height),
            ]
            identities = dict(zip(symbols, cells))
            board_initial = [[(x, y) for x in range(width)] for y in range(height)]
            local_cells = set(cells)
            for label, seq, expected in patterns:
                board = [row.copy() for row in board_initial]
                for glyph, direction in seq:
                    identity = identities[glyph]
                    x, y = next((x, y) for y, row in enumerate(board)
                                for x, value in enumerate(row) if value == identity)
                    if direction in "←→":
                        k = (1 if direction == "←" else -1) % width
                        board[y] = board[y][k:] + board[y][:k]
                    else:
                        k = (1 if direction == "↑" else -1) % height
                        col = [row[x] for row in board]
                        col = col[k:] + col[:k]
                        for row, value in zip(board, col):
                            row[x] = value
                for i, (x, y) in enumerate(cells):
                    # Blank in the source means the original tile was left here.
                    want = board_initial[y][x] if expected[i] == "·" else identities[expected[i]]
                    assert board[y][x] == want, (
                        f"{width}x{height} at {(left, top)}: {label}, lens cell {i}")
                for y in range(height):
                    for x in range(width):
                        if (x, y) not in local_cells:
                            assert board[y][x] == board_initial[y][x], (
                                f"{width}x{height} at {(left, top)}: {label}, "
                                f"outside cell {(x, y)}")
                checked += 1
    return checked


if __name__ == "__main__":
    verify_formula_symmetries()
    verify_both_swaps()
    for name, sequence in COLUMN_SWAPS.items():
        check(f"Column swap ({name})", sequence, "☆○□△")
    for name, sequence in ROW_SWAPS.items():
        check(f"Row swap ({name})", sequence, "△□○☆")
    for name, (text, expected) in SHUFFLES.items():
        check(name, moves(text), expected)
    print("Verified 16 of 16 sequences on the centered 4x4 fixture.")
    total = 0
    for width, height in [(3, 3), (4, 4), (5, 5), (5, 3), (7, 6)]:
        count = verify_portable_positions(width, height)
        total += count
        print(f"PASS portability {width}x{height}: {count} cases")
    print(f"Verified {total} translated/wrapped portability cases.")
