#!/usr/bin/env bash
# Ejecuta la app de escritorio (Windows Forms) en Linux usando Wine.
# Uso: ./ejecutar-escritorio-wine.sh            -> publica (si hace falta) y ejecuta
#      ./ejecutar-escritorio-wine.sh --rebuild  -> fuerza volver a publicar tras cambiar código
#      ./ejecutar-escritorio-wine.sh --ventana  -> sin escritorio virtual (ventanas sueltas)
set -euo pipefail
cd "$(dirname "$0")"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export WINEDEBUG="${WINEDEBUG:--all}"

SALIDA="publish/escritorio-win"
EXE="$SALIDA/Ambrosia.Escritorio.exe"

if [[ " $* " == *" --rebuild "* || ! -f "$EXE" ]]; then
  echo ">> Publicando Ambrosia.Escritorio para win-x64 (autocontenido)..."
  dotnet publish src/Ambrosia.Escritorio -c Release -r win-x64 --self-contained true -o "$SALIDA"
fi

RUTA_WIN="$(winepath -w "$PWD/$EXE")"

if [[ " $* " == *" --ventana "* ]]; then
  exec wine "$RUTA_WIN"
else
  # Escritorio virtual: todas las ventanas de la app quedan dentro de una sola ventana fija.
  exec wine explorer /desktop=Ambrosia,1280x800 "$RUTA_WIN"
fi
