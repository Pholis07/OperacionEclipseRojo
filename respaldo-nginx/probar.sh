#!/usr/bin/env bash
# Construye la imagen con el build WebGL, la corre en http://localhost:PUERTO (8080 por defecto) y comprueba
# que responde. Uso: ./probar.sh [ruta del build]   (por defecto ../Build/WebGL)
# Con MANTENER=1 deja el contenedor corriendo para jugar en el navegador; si no, lo borra al salir.
set -euo pipefail
cd "$(dirname "$0")"
BUILD="${1:-../Build/WebGL}"
PUERTO="${PUERTO:-8080}"
NOMBRE=eclipse-rojo-respaldo

[ -f "$BUILD/index.html" ] || { echo "ERROR: no hay build en $BUILD (haz antes Herramientas > Build WebGL)"; exit 1; }
ss -ltn "sport = :$PUERTO" | grep -q LISTEN && { echo "ERROR: el puerto $PUERTO está ocupado (usa PUERTO=otro ./probar.sh)"; exit 1; }

rm -rf sitio && cp -r "$BUILD" sitio

# En esta máquina crun falla con «join keyctl: Disk quota exceeded» (se llena el cupo de keyrings del
# kernel). Correr sin keyring lo evita, sin tocar la configuración del sistema.
CONF_SIN_KEYRING="$(mktemp)"
printf '[containers]\nkeyring = false\n' > "$CONF_SIN_KEYRING"
export CONTAINERS_CONF_OVERRIDE="$CONF_SIN_KEYRING"
[ "${MANTENER:-0}" = 1 ] || trap 'podman rm -f "$NOMBRE" >/dev/null 2>&1 || true' EXIT

podman build -t eclipse-rojo-web .
podman rm -f "$NOMBRE" >/dev/null 2>&1 || true
podman run -d -p "$PUERTO:80" --name "$NOMBRE" eclipse-rojo-web
sleep 2
curl -s --fail "http://localhost:$PUERTO/" | grep -q "Eclipse Rojo"
curl -s --fail -o /dev/null "http://localhost:$PUERTO/Build/WebGL.loader.js"
echo "OK: el respaldo responde en http://localhost:$PUERTO"
