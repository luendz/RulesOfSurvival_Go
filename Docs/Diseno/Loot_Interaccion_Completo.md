# Loot e interacción

Esta fase implementa el cuarto punto del orden recomendado del plan maestro. Amplía la base existente sin adelantar el panel de inventario completo, que corresponde al punto siguiente.

## Flujo jugable

- `PlayerInteractor` detecta los objetos cercanos, prioriza el más importante y expone la lista al HUD.
- `F` recoge el objeto seleccionado o abre una caja de un jugador eliminado.
- Todo objeto requiere una pulsación explícita de `F`, incluida la munición y los vendajes.
- Los objetos que no caben completos se recogen parcialmente y el resto permanece en el mundo.
- Cascos, chalecos y mochilas se equipan al recogerlos; el objeto reemplazado vuelve al suelo.
- Las mochilas aumentan la capacidad total a 140, 180 o 220 según el nivel.
- Las armas se asignan a un slot libre y se equipan inmediatamente. Si los slots primarios están ocupados, se reemplaza el slot activo.
- `G` tira una unidad del último stack del inventario; `Shift + G` tira el stack completo.

## Catálogo inicial

El catálogo incluye rifle, cinco tipos de munición, vendaje, botiquín, cascos y chalecos de tres niveles, mochilas de tres niveles, granada, mira de punto rojo y una baliza especial. Cada definición mantiene un identificador estable, rareza, modo de recogida, peso y modelo de mundo cuando existe un recurso compatible.

## Generación

`LootSpawner` admite puntos explícitos o un área aleatoria, separación mínima, alineación con el suelo y una tabla ponderada. La rareza queda expresada tanto en la definición del objeto como en su peso dentro de la tabla. Cada ejecución vuelve a sortear las entradas.

El prefab de demostración garantiza una muestra de arma, protección, mochila, medicamento, granada y accesorio; después añade diez resultados aleatorios. Esto permite validar el sistema en pocos minutos sin depender del azar.

## Prueba rápida

1. Abrir `07_BattleRoyaleTest` y entrar en Play Mode.
2. Caminar unos diez metros hacia delante hasta el área de loot.
3. Confirmar que aparece la lista `OBJETOS CERCANOS`.
4. Usar `F` sobre rifle, casco, chaleco, mochila, botiquín, granada o accesorio.
5. Pasar junto a munición o vendajes, verificar que permanecen en el suelo y recogerlos con `F`.
6. Usar `G` y confirmar que una unidad vuelve al suelo sin recogerse de inmediato.

## Límites de esta fase

El uso de consumibles, lanzamiento de granadas, efectos de accesorios, slots visuales de armadura y el panel completo con drag and drop se implementan en sus fases específicas. En esta fase los objetos quedan correctamente definidos, generados, recogidos, equipados o almacenados.
