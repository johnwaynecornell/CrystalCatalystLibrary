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


if __name__ == "__main__":
    for name, sequence in COLUMN_SWAPS.items():
        check(f"Column swap ({name})", sequence, "☆○□△")
    for name, sequence in ROW_SWAPS.items():
        check(f"Row swap ({name})", sequence, "△□○☆")
    for name, (text, expected) in SHUFFLES.items():
        check(name, moves(text), expected)
    print("Verified 16 of 16 sequences on the centered 4x4 fixture.")
