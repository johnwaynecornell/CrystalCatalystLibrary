#!/usr/bin/env bash
set -euo pipefail

feffect -e -file $NewAge/JWCEssentials/examples/feffect/first.feffect "bg_bright_white.fg_blue('this part demonstrates composition with other content')"  | MessageWindow -stdin
