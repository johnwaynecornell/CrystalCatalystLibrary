#!/usr/bin/env bash
set -euo pipefail

if [ ! -f "../JWCEssentials/Dev/NewAge.dev.sh" ]; then
  echo "Expected run from NewAge/Repo" >&2
  exit 1
fi

. "$NewAge/JWCEssentials/Dev/NewAge.dev.sh"

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
  ./Dev/build_native.sh [Debug|Release] [--fresh] [--clean]

Options:
  --fresh
      Remove CMake configure/cache files before configuring.

  --clean, --target-clean
      Run the generated build system's clean target before building.
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
            echo "[build_native] ERROR: Unknown option: $1" >&2
            usage >&2
            exit 1
            ;;

        *)
           if [ -z "$config" ]; then
                config="$1"
            else
                echo "[build_native] ERROR: Unexpected argument: $1" >&2
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

if [ -z "${NewAge_Lane:-}" ]; then
    export NewAge_Lane="$(newage_resolve_platform_lane)"
fi

build_directory()
{
  local source_dir="$REPO_DIR/$1"
  local build_dir="$source_dir/build/$NewAge_Config/$NewAge_Lane"

  echo "[build_native] Using build directory: $build_dir"
  mkdir -p "$build_dir"
  cd "$build_dir"

  if [ "$FRESH" = "1" ]; then
    verbose.sh cmake -S "$source_dir" -B . -DCMAKE_BUILD_TYPE="$NewAge_Config" --no-warn-unused-cli --fresh
  else
    verbose.sh cmake -S "$source_dir" -B . -DCMAKE_BUILD_TYPE="$NewAge_Config" --no-warn-unused-cli
  fi

  if [ "$CLEAN" = "1" ]; then
      verbose.sh cmake --build . --config "$NewAge_Config" --target clean
  fi

  verbose.sh cmake --build . --config "$NewAge_Config"
}

build_directory .