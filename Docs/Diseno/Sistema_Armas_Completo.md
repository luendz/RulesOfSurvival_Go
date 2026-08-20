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

## Prueba rápida

1. Entrar en Play Mode con el rifle equipado.
2. Mantener disparo en `AUTO` y confirmar fuego continuo.
3. Presionar `B` hasta `SEMI`; mantener disparo y confirmar que solo sale una bala hasta soltar el botón.
4. Presionar `B` hasta `RAFAGA`; hacer clic una vez y confirmar tres disparos.
5. Recargar con balas restantes y luego con el cargador vacío; la segunda recarga debe tardar medio segundo más.

## Siguientes bloques del mismo paso

- Munición compatible compartida con el inventario.
- Perfiles de alcance, velocidad, caída y daño por familia de arma.
- Accesorios funcionales y miras.
- Sonidos, animaciones y efectos individuales cuando estén disponibles los recursos finales.
