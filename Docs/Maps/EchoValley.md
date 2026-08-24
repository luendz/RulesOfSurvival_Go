# Echo Valley

Implementación inicial del POI **Echo Valley** para el mapa Battle Royale de `RulesOfSurvival_Go`.

## Referencia de layout

La reconstrucción parte de la distribución documentada del Echo Valley clásico de Rules of Survival:

- 1 edificio principal de 3 pisos.
- 1 edificio de 2 pisos.
- 4 almacenes.
- Bicicletas junto al edificio principal.
- Casas dispersas alrededor del núcleo.
- Ubicación en un valle, alejada de las carreteras principales.
- Rutas de salida hacia el norte, sur, este y oeste.

La geometría actual es propia y procedural. No se incluyen assets extraídos del juego original. Los bloques están preparados para sustituirse por modelos definitivos manteniendo posiciones y gameplay.

## Escena

`Assets/_Game/Scenes/08_EchoValley.unity`

La escena contiene `EchoValley_MapAuthoring`, que genera el entorno automáticamente al abrirse en el editor y también en runtime.

## Sistemas generados

### Terreno

- Valle de aproximadamente 540 x 470 m.
- Cuenca central relativamente plana.
- Elevaciones y crestas periféricas.
- Caminos de tierra y accesos de rotación.
- Patio del edificio principal y patio industrial.

### Núcleo urbano

- `Main_3F_Apartment`: edificio principal de tres niveles, interiores, escaleras, ventanas, cubierta y parapetos.
- `Two_Story_House`: vivienda de dos pisos con interior y escalera.
- `Warehouse_01_West`.
- `Warehouse_02_CenterWest`.
- `Warehouse_03_CenterEast`.
- `Warehouse_04_East`.
- Ocho casas periféricas.

### Cobertura y ambientación

- Cercos.
- Barreras de concreto.
- Cajas y barriles.
- Postes de servicios.
- Señales de Echo Valley.
- Árboles, rocas y vegetación procedural con semilla fija.
- Tres bicicletas visuales en el área del edificio principal.

### Gameplay

- 12 puntos de aparición de jugadores.
- Loot de prioridad alta en el edificio principal.
- Loot medio en edificio de dos pisos y almacenes.
- Loot bajo en casas periféricas.
- 3 puntos de bicicletas.
- 4 puntos de salida/rotación.

Los puntos usan `EchoValleySpawnMarker` y pueden conectarse posteriormente con los sistemas definitivos de loot, vehículos y spawning.

## Estructura generada

```text
EchoValley_Generated
├── 01_Tracks_And_Yards
├── 02_Core_Compound
├── 03_Surrounding_Houses
├── 04_Cover_And_Fences
├── 05_Bicycles
├── 06_Gameplay_Markers
├── 07_Nature
└── 08_Utility_Details
```

## Siguiente nivel de fidelidad

Para pasar de esta reconstrucción estructural a una réplica visual de mayor fidelidad, sustituir las primitivas por prefabs finales conservando los mismos transforms y marcadores. Prioridad recomendada: edificio principal, almacenes, casa de dos pisos, casas periféricas, vegetación y props.
