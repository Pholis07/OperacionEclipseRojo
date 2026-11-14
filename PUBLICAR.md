# Publicar el juego

Publicación principal: **GitHub Pages**. Respaldo local: **nginx en Podman**. Publicar es decisión del equipo: ningún script hace push ni sube nada solo.

## 1. Hacer el build

En Unity: **Herramientas > Build WebGL**. Sale en `Build/WebGL` (carpeta ignorada por git), con Gzip, Decompression Fallback y la plantilla `PantallaCompleta` (el juego ocupa toda la ventana del navegador).

Sin abrir el editor (con Unity cerrado):

```bash
~/Unity/Hub/Editor/6000.6.9f1/Editor/Unity -batchmode -nographics -quit \
  -projectPath /ruta/al/proyecto -buildTarget WebGL -executeMethod ConstructorWebGL.Construir -logFile build.log
```

Último build comprobado: 16.9 MB, 0 errores, jugable en Chromium a 1920x1080 y 1366x768.

## 2. Probarlo en local (respaldo)

```bash
respaldo-nginx/probar.sh            # construye la imagen, la prueba en http://localhost:8080 y la borra
MANTENER=1 respaldo-nginx/probar.sh # la deja corriendo para jugar en el navegador
```

Si el puerto 8080 está ocupado: `PUERTO=8090 respaldo-nginx/probar.sh`.

## 3. Publicar en GitHub Pages

1. Preparar la rama local `gh-pages` con el build (no hace push):

   ```bash
   Herramientas/preparar-pages.sh
   ```

2. Subirla, **cuando el equipo lo decida**:

   ```bash
   git push origin gh-pages
   ```

3. Solo la primera vez, en GitHub: **Settings > Pages > Build and deployment > Source: Deploy from a branch**, rama `gh-pages`, carpeta `/ (root)`, **Save**.

4. Esperar uno o dos minutos y abrir **https://pholis07.github.io/OperacionEclipseRojo/**. Comprobar que carga, que se juega de principio a fin y que la consola del navegador (F12) no tiene errores.

Notas:
- El repositorio es público, así que Pages es gratuito.
- GitHub Pages no manda la cabecera `Content-Encoding: gzip`; por eso el build usa **Decompression Fallback**: el navegador descomprime los archivos. La consola mostrará un aviso informativo sobre esa cabecera; no es un error.
- Para actualizar la publicación: nuevo build, `Herramientas/preparar-pages.sh` y `git push origin gh-pages`.

## Antes de publicar la versión final

- Pasar las pruebas: Window > General > Test Runner, EditMode y PlayMode, «Run All».
- Revisar `CREDITOS.md` (cada recurso de terceros con su licencia) e `IA.md` (uso de IA).
- Confirmar con los organizadores la hora límite, los entregables y si el runtime de Unity cuenta como material de terceros.
