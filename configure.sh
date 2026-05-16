#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$SCRIPT_DIR"
REPO_REL_PATH="CrystalCatalystLibrary"

NEWAGE_CONFIGURE_SCOPE="$REPO_REL_PATH configure"
REGISTER_REPO_ROOT="0"
NEWAGE_ARG=""

while [ "$#" -gt 0 ]; do
    case "$1" in
        --newage)
            shift
            [ "$#" -gt 0 ] || { echo "--newage requires a path" >&2; exit 1; }
            NEWAGE_ARG="$1"
            ;;
        --register-repo-root)
            REGISTER_REPO_ROOT="1"
            ;;
        -h|--help)
            cat <<EOF
Usage:
  ./configure.sh [--newage PATH] [--register-repo-root]

Options:
  --newage PATH
      Use PATH as the NewAge workspace for this configure run.

  --register-repo-root
      Allow configure to create a live registration from:
        \$NewAge/JWCEssentials
      to this checkout when this checkout is not physically located there.
EOF
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 1
            ;;
    esac
    shift
done

if [ -n "$NEWAGE_ARG" ]; then
    export NewAge="$NEWAGE_ARG"
fi

export NEWAGE_REGISTER_REPO_ROOT="$REGISTER_REPO_ROOT"
if [ -z "${NewAge:-}" ]; then
    echo "[$NEWAGE_CONFIGURE_SCOPE] ERROR: NewAge is not set." >&2
    echo "[$NEWAGE_CONFIGURE_SCOPE] Run JWCEssentials/configure.sh first." >&2
    echo "[$NEWAGE_CONFIGURE_SCOPE] See: https://github.com/johnwaynecornell/JWCEssentials" >&2
    exit 1
fi

if [ ! -f "$NewAge/JWCEssentials/Dev/NewAge.dev.sh" ]; then
    echo "[$NEWAGE_CONFIGURE_SCOPE] ERROR: JWCEssentials development helpers were not found." >&2
    echo "[$NEWAGE_CONFIGURE_SCOPE] Expected: $NewAge/JWCEssentials/Dev/NewAge.dev.sh" >&2
    echo "[$NEWAGE_CONFIGURE_SCOPE] Run JWCEssentials/configure.sh first." >&2
    echo "[$NEWAGE_CONFIGURE_SCOPE] See: https://github.com/johnwaynecornell/JWCEssentials" >&2
    exit 1
fi

. "$NewAge/JWCEssentials/Dev/NewAge.dev.sh"

newage_register_repo_root "$REPO_ROOT" "$REPO_REL_PATH"

newage_register_directory \
    "$REPO_ROOT/include/$REPO_REL_PATH" \
    "$NewAge/include/$REPO_REL_PATH" \
    required

newage_log "Workspace setup complete."