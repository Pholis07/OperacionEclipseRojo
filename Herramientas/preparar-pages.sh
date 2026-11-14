#!/usr/bin/env bash
# Prepara la rama LOCAL gh-pages con el build WebGL para publicarlo en GitHub Pages.
# NO hace push: publicar es decisión del equipo (ver PUBLICAR.md).
# Uso: Herramientas/preparar-pages.sh   (antes: Herramientas > Build WebGL en Unity)
set -euo pipefail
cd "$(dirname "$0")/.."
BUILD=Build/WebGL
[ -f "$BUILD/index.html" ] || { echo "ERROR: falta el build en $BUILD (Herramientas > Build WebGL)"; exit 1; }

DIR="$(mktemp -d)"
trap 'git worktree remove --force "$DIR" >/dev/null 2>&1 || true' EXIT
if git show-ref --verify --quiet refs/heads/gh-pages; then
  git worktree add -q "$DIR" gh-pages
else
  git worktree add -q --detach "$DIR"
  git -C "$DIR" checkout -q --orphan gh-pages
  git -C "$DIR" rm -rq --cached . && git -C "$DIR" clean -fdxq
fi

find "$DIR" -mindepth 1 -maxdepth 1 ! -name .git -exec rm -rf {} +
cp -r "$BUILD"/. "$DIR"/
touch "$DIR/.nojekyll"   # que GitHub sirva los archivos tal cual
git -C "$DIR" add -A
if git -C "$DIR" commit -q -m "Publicación: build WebGL de main $(git rev-parse --short HEAD)"; then
  echo "Listo: la rama local gh-pages tiene el build de $(git rev-parse --short HEAD) ($(du -sh "$BUILD" | cut -f1))."
else
  echo "La rama gh-pages ya tenía este mismo build: no hay nada nuevo."
fi
echo "Para publicar: git push origin gh-pages   (y ver PUBLICAR.md)"
