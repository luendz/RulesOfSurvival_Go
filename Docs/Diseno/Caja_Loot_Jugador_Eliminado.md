# Caja de loot del jugador eliminado

Esta fase completa la caja básica creada por el flujo de muerte y la convierte en una fuente de loot seleccionable, limitada por la capacidad real del inventario del jugador que saquea.

## Flujo funcional

1. Al morir, el inventario completo del jugador se conserva en un `DeathLootContainer` identificado con el nombre de su propietario.
2. Un jugador vivo puede abrir la caja aunque no tenga espacio para llevarse todo.
3. `DeathLootPanelPresenter` muestra los stacks disponibles, sus cantidades, tipo, peso y la ocupación actual de la mochila.
4. Cada fila permite recoger ese objeto. Si el stack completo no cabe, se transfiere automáticamente la cantidad máxima posible.
5. El botón `Recoger todo lo posible` recorre los stacks y transfiere únicamente lo que admite la capacidad restante.
6. Mientras el panel está abierto, `PlayerInputReader` bloquea movimiento, cámara, disparo y acciones de combate, además de liberar el cursor.
7. El panel se cierra con su botón, con `Escape`, si el jugador muere, si se aleja más de cuatro metros o si la caja queda vacía.
8. Una caja vacía desactiva sus colliders y se elimina de la escena.

## Contratos de inventario

- `GetAmount` devuelve la cantidad total de una definición sumando todos sus stacks.
- `GetMaxAddableAmount` calcula cuántas unidades admite la capacidad restante, incluyendo objetos sin peso.
- `TransferTo` nunca supera la cantidad solicitada, la cantidad existente ni la capacidad de destino.
- Si una transferencia no puede completarse, el inventario origen no pierde objetos.
- `TransferAllPossibleTo` puede dejar un remanente legítimo en la caja cuando la mochila está llena.
- Los límites de stack se normalizan a un mínimo de una unidad para evitar datos inválidos que bloqueen el inventario.

## Alcance visual

La interfaz actual es funcional y se genera en runtime para no depender de una escena o prefab concreto. La presentación visual definitiva —iconografía, animaciones, navegación con mando y diseño final del inventario— se consolidará durante la fase de inventario/HUD, sin cambiar estos contratos de transferencia.

## Validación

- EditMode cubre la transferencia parcial limitada por peso y capacidad.
- PlayMode cubre la creación de la caja, identificación del propietario, apertura del panel, bloqueo de input, saqueo parcial, saqueo restante y destrucción de la caja vacía.
