# Base de pruebas y contratos de datos

Esta base protege los sistemas existentes antes de ampliar el ciclo de partida.

## Contratos de datos

Las definiciones jugables exponen `IGameDataDefinition`:

- `StableId`: identificador persistente para inventario, guardado y red.
- `Confidence`: procedencia o nivel de confianza del dato de balance.

Los identificadores válidos usan únicamente letras minúsculas ASCII, números y los separadores `_`, `-` y `.`. La longitud máxima es de 64 caracteres. Un identificador no debe depender del nombre visible ni de la ruta del asset.

Los niveles de confianza son:

- `Unknown`: procedencia todavía no clasificada.
- `Prototype`: valor provisional del proyecto.
- `Verified`: confirmado para la versión histórica objetivo.
- `Community`: recopilado de fuentes comunitarias.
- `Estimated`: estimación pendiente de verificación.
- `Contradictory`: existen fuentes incompatibles.

## Pruebas automatizadas

### EditMode

- Validación de identificadores estables.
- Contrato compartido de definiciones.
- Daño, absorción de armadura y evento de muerte.
- Capacidad, stacks y eliminación de inventario.
- Selección ponderada y determinista de tablas de loot.

### PlayMode

- Actualización de jugadores vivos y final de partida.
- Daño de Safe Zone a jugadores fuera del radio.

## Ejecución

En Unity, abrir `Window > General > Test Runner` y ejecutar primero EditMode y después PlayMode.

Por línea de comandos:

```text
Unity.exe -batchmode -projectPath <ruta> -runTests -testPlatform EditMode -testResults <resultado.xml> -quit
Unity.exe -batchmode -projectPath <ruta> -runTests -testPlatform PlayMode -testResults <resultado.xml> -quit
```

Cada corrección futura debe incluir una prueba que falle antes del cambio y pase después cuando el comportamiento pueda validarse automáticamente.
