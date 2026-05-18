#!/usr/bin/env bash
set -euo pipefail

if [ ! -f "$NewAge/JWCEssentials/Dev/NewAge.dev.sh" ]; then
  echo "Expected run from NewAge/Repo" >&2
  exit 1
fi

if [ ! -f "CMakeLists.txt" ]; then
  echo "Expected run from NewAge workspace root" >&2
  exit 1
fi

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

build_directory()
{
  cd "$REPO_DIR/$1"

  if [ "$FRESH" = "1" ]; then
    verbose.sh dotnet build --no-restore
  fi

  if [ "$CLEAN" = "1" ]; then
      verbose.sh dotnet clean --configuration "$NewAge_Config"
  fi

  verbose.sh dotnet build --configuration "$NewAge_Config"
}

build_directory Project/JWCEssentials.net
