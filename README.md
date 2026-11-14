# Operación Eclipse Rojo

Juego de un jugador hecho para el NASA Space Apps Challenge 2026 (reto Space Mission Design Game), equipo GangBangers.

Una señal térmica rara aparece en una región de Marte y una tormenta de polvo dificulta la investigación. El jugador diseña una misión: elige órbita, lanzador e instrumentos sin pasarse de los límites de masa, potencia, costo y espacios, y recibe un informe que explica el resultado. Los indicios del juego no son prueba de agua líquida actual ni de vida.

Los valores del juego son ficticios: no son capacidades ni precios oficiales de NASA.

## Requisitos

- Unity 6000.6.9f1 (Unity 6 LTS) con WebGL Build Support.

## Estructura

- `Assets/Scripts/Datos`, `Logica`, `UI`: código del juego (assembly `SpaceMission`).
- `Assets/Datos`: ScriptableObjects con los valores.
- `Assets/Escenas`: `Menu`, `Mision` y `Resultado`.
- `Assets/Tests/EditMode`: pruebas (assembly `SpaceMission.Tests`).

## Estado

En construcción. Las fases se registran en `IA.md`.
