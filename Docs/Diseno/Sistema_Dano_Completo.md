# Sistema de daño completo

Esta fase implementa el tercer punto del orden recomendado del plan maestro. Todas las fuentes de daño comparten un contexto común, pasan por las mismas reglas de vida y generan un resultado resuelto para gameplay, HUD y audio.

## Fuentes de daño

- `Firearm`: disparos procesados por `WeaponController`.
- `Explosion`: daño radial con caída lineal desde el radio interior hasta el borde.
- `Fall`: daño progresivo según la velocidad vertical de aterrizaje.
- `SafeZone`: daño por segundo que ignora protecciones.
- `Generic`: compatibilidad con herramientas y sistemas anteriores.

`DamageInfo` conserva cantidad base, punto, dirección, instigador, tipo y zona corporal. `DamageResult` informa multiplicador, daño modificado, absorción, daño real a vida, headshot y resultado fatal.

## Zonas de impacto

Los disparos usan los siguientes multiplicadores provisionales:

- cabeza: `x2.00`;
- torso: `x1.00`;
- brazos: `x0.75`;
- piernas: `x0.65`.

`PlayerDamageHitboxRig` genera en runtime hitboxes independientes para cada zona sin modificar escenas ni prefabs. Si un personaje todavía no tiene hitboxes específicas, su collider principal se interpreta como torso para mantener compatibilidad.

## Casco y chaleco

`ProtectiveEquipment` admite niveles 1, 2 y 3 con reducción y durabilidad propias:

- nivel 1: 30 % de reducción;
- nivel 2: 40 % de reducción;
- nivel 3: 55 % de reducción.

El casco protege la cabeza. El chaleco protege torso y brazos, y también absorbe daño de explosión. Cada punto absorbido consume un punto de durabilidad; al llegar a cero la pieza se rompe. Caídas y Safe Zone ignoran casco y chaleco.

Las durabilidades y los porcentajes son valores de prototipo centralizados en `ProtectiveEquipment`; podrán balancearse sin cambiar los contratos del sistema.

## Feedback

- Hitmarker blanco al impactar, amarillo para headshot y rojo para impacto fatal.
- Indicador rojo en el borde de pantalla según la dirección del daño recibido.
- Evento `Health.Damaged` con el resultado resuelto para futuros HUD, estadísticas y red.
- Puntos de extensión para sonidos de impacto y para un prefab de sangre. Si no hay sangre asignada, se conserva el efecto de impacto existente y no se crea un bullet hole sobre personajes.

## Demostración rápida

Al ejecutar `07_BattleRoyaleTest` se crea después del inicio de la partida un objetivo de práctica que no altera el contador de vivos. Sus colores identifican las protecciones: amarillo para casco nivel 2 y azul para chaleco nivel 2.

También se habilitan controles temporales sobre el jugador local:

- `F5`: equipar casco y chaleco nivel 2;
- `F6`: simular disparo al torso;
- `F7`: simular headshot;
- `F8`: simular explosión cercana;
- `F9`: simular aterrizaje peligroso.

El objetivo, las hitboxes, el feedback y los controles se crean solo en Play Mode. No se guardan objetos adicionales en la escena.

## Contratos validados

- Solo los disparos aplican multiplicadores por zona corporal.
- Casco y chaleco nunca absorben más daño que su durabilidad disponible.
- El daño por caída y Safe Zone ignora protecciones.
- Un mismo personaje recibe una sola aplicación por detonación aunque tenga varios colliders.
- El daño de explosión disminuye hasta cero en el límite exterior.
- El evento de daño expone la cantidad real descontada y si el impacto fue fatal.

Unity detecta 28 pruebas EditMode en el proyecto, de las cuales 10 cubren específicamente multiplicadores, protecciones, durabilidad, eventos, caídas y explosiones.
