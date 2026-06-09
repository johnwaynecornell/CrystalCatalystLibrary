#!/usr/bin/env bash
set -euo pipefail

answer=$(bash -e -c '
    feffect -e "\"😃 \" fg_red(\"hello\" blink(\"!!!\")) \" This is a CrystalCatalyst dotnet app\""
    feffect -e "bg_bright_white.fg_blue(\"It is designed as a \" blink(\"Simple Example\")) bg_default"
    feffect -e "\"If you click \" fg_magenta(\"[OK]\" fg_green(\" or \") \"[Confirm]\") \" I will echo back\""

    feffect -e "fg_cyan(\"This proves my utility as a simple dialog.\")"


    ' | MessageWindow -stdin -button "OK" -button "Confirm"
    )

if [ -z "$answer" ]; then echo "User chose to exit"
elif [ "$answer" == "OK" ]; then echo "User pressed OK"
elif [ "$answer" == "Confirm" ]; then echo "User pressed Confirm"
else echo "**ERROR**" >&2
fi
