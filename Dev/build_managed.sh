#!/usr/bin/env bash
set -euo pipefail

if [ ! -f "../JWCEssentials/Dev/NewAge.dev.sh" ]; then
  echo "Expected run from NewAge/Repo" >&2
  exit 1
fi

. "$NewAge/JWCEssentials/Dev/NewAge.dev.sh"

config=""
FRESH="0"
CLEAN="0"
REPO_DIR=$(pwd)

usage() {
    cat <<EOF
Usage:
  ./Dev/build_managed.sh [Debug|Release] [--fresh] [--clean]

Options:
  --fresh
      Run dotnet build --no-restore.

  --clean, --target-clean
      Run dotnet clean before building.
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        -h|--help|help)
            usage
            exit 0
            ;;

        --fresh)
            FRESH="1"
            ;;

        --clean|--target-clean)
            CLEAN="1"
            ;;
        --*)
            echo "[build_managed] ERROR: Unknown option: $1" >&2
            usage >&2
            exit 1
            ;;

        *)
           if [ -z "$config" ]; then
                config="$1"
            else
                echo "[build_managed] ERROR: Unexpected argument: $1" >&2
                usage >&2
                exit 1
            fi
            ;;
    esac
    shift
done

if [ -z "$config" ]; then
    config="${NewAge_Config:-Debug}"
fi

export NewAge_Config="$config"

NEWAGE_BUILD_FRESH="$FRESH"
NEWAGE_BUILD_CLEAN="$CLEAN"
newage_managed_build_directory Project/CrystalCatalystLibrary.net
