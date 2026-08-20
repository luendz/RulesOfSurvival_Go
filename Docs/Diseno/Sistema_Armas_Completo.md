# Sistema de armas completo

Esta fase inicia el sexto punto del orden recomendado del plan maestro sobre la base de disparo, daño, equipamiento y efectos que ya existía en el proyecto.

## Núcleo jugable implementado

- Cada arma define los modos de disparo que admite: semiautomático, ráfaga y automático.
- `B` cambia al siguiente modo permitido. En mando se usa la cruceta hacia abajo.
- El modo semiautomático exige soltar el gatillo antes del siguiente disparo.
- El modo ráfaga completa la cantidad configurada por el arma y exige una nueva pulsación para iniciar otra.
- El modo automático mantiene la cadencia mientras el disparo permanece presionado.
- Cambiar de modo cancela cualquier ráfaga pendiente.
- La recarga táctica y la recarga con cargador vacío tienen duraciones independientes.
- El recoil vertical, horizontal y sus velocidades de recuperación se configuran por arma.
- El panel de arma muestra el modo activo como `SEMI`, `RAFAGA` o `AUTO`.

## Configuración del rifle de prueba

- Cargador: 30.
- Modos disponibles: semiautomático, ráfaga de 3 y automático.
- Recarga táctica: 2.2 segundos.
- Recarga vacía: 2.7 segundos.
- Cambio de modo: `B`.

## Armas disponibles en el loot

Se agregó un arma jugable por cada familia que actualmente cuenta con modelo 3D en el proyecto:

- Fusil de asalto: M4A1, 31 de daño, 30 balas y modos `SEMI`/`AUTO`.
- Subfusil: MP7, 22 de daño, 30 balas y modos `SEMI`/`AUTO`.
- Francotirador: AWM, 105 de daño, 5 balas y modo `SEMI`.
- Escopeta: M1887, 16 de daño por perdigón, 2 cartuchos y 8 perdigones por disparo.
- Pistola: Desert Eagle, 52 de daño, 7 balas y modo `SEMI`.

Cada arma usa su FBX existente tanto al aparecer en el suelo como al equiparse. También define cadencia, alcance, dispersión, retroceso, tamaño del impacto, trazadora y munición de reserva propios. La M1887 procesa cada perdigón por separado, por lo que un disparo puede producir hasta ocho impactos y agujeros de bala independientes.

No se agregó una ametralladora ligera porque todavía no existe un modelo 3D de esa familia dentro de `Assets/_Game/Art/Weapons`.

## Prueba rápida

1. Entrar en Play Mode con el rifle equipado.
2. Mantener disparo en `AUTO` y confirmar fuego continuo.
3. Presionar `B` hasta `SEMI`; mantener disparo y confirmar que solo sale una bala hasta soltar el botón.
4. Presionar `B` hasta `RAFAGA`; hacer clic una vez y confirmar tres disparos.
5. Recargar con balas restantes y luego con el cargador vacío; la segunda recarga debe tardar medio segundo más.
6. Recoger M4A1, MP7, AWM, M1887 y Desert Eagle del área de prueba y confirmar que cambia el modelo, nombre, cargador, reserva y modo permitido.
7. Disparar la M1887 contra una superficie cercana y confirmar la dispersión de varios impactos con un solo cartucho consumido.

## Siguientes bloques del mismo paso

- Munición compatible compartida con el inventario.
- Perfiles de alcance, velocidad, caída y daño por familia de arma.
- Accesorios funcionales y miras.
- Sonidos, animaciones y efectos individuales cuando estén disponibles los recursos finales.
