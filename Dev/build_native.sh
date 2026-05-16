#!/usr/bin/env bash
set -euo pipefail

if [ ! -f "../JWCEssentials/Dev/NewAge.dev.sh" ]; then
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

while [ "$#" -gt 0 ]; do
    case "$1" in
        --fresh)
            FRESH="1"
            ;;

        --clean|--target-clean)
            CLEAN="1"
            ;;
        --*)
            echo "[newage_build_native] ERROR: Unknown option: $1" >&2
            usage >&2
            exit 1
            ;;

        *)
           if [ -z "$config" ]; then
                config="$1"
            else
                echo "[newage_build_native] ERROR: Unexpected argument: $1" >&2
                usage >&2
                exit 1
            fi
            ;;
    esac
    shift
done

build_directory()
{
  cd "$REPO_DIR/$1"

  if [ "$FRESH" = "1" ]; then
    verbose.sh cmake CMakeLists.txt -DCMAKE_BUILD_TYPE="$config" --fresh
  else
    verbose.sh cmake CMakeLists.txt -DCMAKE_BUILD_TYPE="$config"
  fi

  if [ "$CLEAN" = "1" ]; then
      verbose.sh cmake --build . --config "$config" --target clean
  fi

  verbose.sh cmake --build . --config "$config"
}

build_directory .