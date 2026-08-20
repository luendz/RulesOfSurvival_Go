# Muerte, eliminación y fin de partida

Esta fase implementa el primer bloque del plan maestro y deja cerrado el ciclo mínimo de una eliminación dentro de una partida Battle Royale.

## Flujo implementado

1. `Health` conserva el último `DamageInfo` y cambia el jugador de `Alive` a `Dead` una sola vez.
2. `PlayerEliminationController` bloquea movimiento, interacción, apuntado, disparo, recarga, cambio de arma y lectura de input.
3. La cámara pasa a una vista fija posterior a la muerte sin depender del input del jugador.
4. El inventario completo se transfiere a un `DeathLootContainer` interactuable.
5. `BattleRoyaleManager` descuenta al jugador del total de vivos, identifica al eliminador, registra su baja y asigna la posición final.
6. Cuando queda un solo jugador vivo, la partida cambia a `Finished` y registra al ganador.
7. `MatchResultPresenter` muestra eliminación o victoria, posición final y eliminador cuando corresponde.

## Animación de muerte

La lógica de gameplay no depende de un clip externo. La versión actual aplica una caída provisional en código sobre el visual del personaje; por eso esta fase puede probarse de principio a fin sin conseguir primero una animación.

Para la calidad visual final se necesita un clip de muerte Humanoid compatible con el esqueleto del personaje y con licencia válida para el proyecto. Ese clip puede provenir de un paquete propio, de una biblioteca autorizada o de una animación creada específicamente. Al recibirlo, el trabajo dentro del proyecto será:

1. importar el FBX o archivo de animación;
2. configurarlo como `Humanoid` y retargetearlo al avatar del jugador;
3. añadir el estado `Dead` al Animator Controller;
4. crear la transición sin salida desde locomoción/combate;
5. validar arma, colisiones, cámara y posición final del cuerpo;
6. desactivar la caída provisional conservando intacta la lógica de eliminación.

En resumen: la animación provisional la crea el proyecto; el clip final de calidad debe suministrarse o elegirse con su licencia, y su integración queda a cargo del proyecto.

## Contratos principales

- Una muerte solo emite una eliminación, incluso si llegan daños posteriores.
- Un suicidio o daño de zona no acredita una baja a la propia víctima.
- La posición de la víctima equivale a la cantidad de supervivientes después de morir más uno.
- La caja de muerte recibe el inventario completo o no realiza la transferencia.
- El final de partida se emite una sola vez y conserva la referencia al ganador.
- Reiniciar la partida limpia ganador, última eliminación, posiciones y contador de bajas.

## Validación automatizada

- EditMode: 17 pruebas para contratos de datos, daño, muerte, inventario y loot.
- PlayMode: 2 pruebas para el flujo de eliminación/final de partida y el daño de zona segura.
