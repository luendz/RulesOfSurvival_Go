# RulesOfSurvival_Go!

Proyecto base Battle Royale 3D desarrollado en Unity 6000.3.11f1.

## Abrir el proyecto

Abra esta carpeta desde Unity Hub usando Unity 6000.3.11f1. No es necesario versionar ni copiar `Library`, `Temp`, `Logs` u `obj`; Unity las regenera.

## Estructura principal

- `Assets/_Game/Code`: código del juego, agrupado por sistema.
- `Assets/_Game/Code/Editor`: herramientas manuales de autoría y validación; nunca se ejecutan automáticamente.
- `Assets/_Game/Prefabs`: objetos completos y configurados para instanciar.
- `Assets/_Game/Scenes`: composición explícita de cada flujo jugable.
- `Assets/_Game/Data`: definiciones y configuración compartida.
- `Assets/_Game/Tests`: contratos EditMode y PlayMode.

## Reglas de arquitectura

- `PlayerAnimationCoordinator` es el único escritor de parámetros del Animator del jugador.
- Las referencias estructurales se asignan en prefabs o escenas desde el Inspector; un script no se repara con `Find*`, `Resources.Load` ni `AddComponent` durante Play Mode.
- El runtime puede instanciar entidades transitorias ya configuradas —bots, loot y efectos—, pero no construir jerarquías permanentes ni interfaces.
- El HUD vive como una única jerarquía editable. No hay scripts de pulido, materializadores ni paneles heredados compitiendo entre sí.
- Los errores de configuración deben fallar de forma visible y ser detectados por la validación del proyecto.

## Escenas de prueba

Use las escenas de `_Game/Scenes` para validar cada sistema de forma aislada antes de integrarlo en una partida completa.

## Pruebas automatizadas

El proyecto incluye pruebas EditMode y PlayMode en `Assets/_Game/Tests`. Se ejecutan desde `Window > General > Test Runner` en Unity. Los contratos y el alcance inicial están documentados en `Docs/Diseno/Base_Pruebas_y_Contratos.md`.

## Controles (equipo de armas)

- `1` — equipar arma primaria slot 1
- `2` — equipar arma primaria slot 2 (si existe)
- `3` — equipar arma secundaria / sidearm (si existe)
- `X` — guardar arma actual
- Click izquierdo — disparar (solo con arma equipada)
- Click derecho — apuntar / ADS (solo con arma equipada)

## Estado actual

La locomoción, cámara, crouch, aim y el prototipo de rifle ya forman la base jugable. El rifle incluye munición, recarga, raycast de impacto, muzzle flash, tracer, partículas de impacto, bullet hole y recoil.

El ciclo de eliminación ya integra estado de muerte, bloqueo de controles, atribución de bajas, posición final, ganador, cámara posterior a la muerte y transferencia del inventario a una caja de loot. La caja permite inspeccionar stacks, recoger objetos concretos, transferir solo la cantidad que cabe y recoger todo lo posible desde una interfaz que bloquea temporalmente el control de gameplay. La representación visual de muerte usa una caída provisional generada en código; el clip humanoide final queda desacoplado para poder importarlo y retargetearlo más adelante. Los vehículos, el paracaídas y el loot de mundo avanzado siguen siendo bases que requieren integración y pruebas de gameplay.

El daño distingue disparos, explosiones, caídas y Safe Zone; aplica multiplicadores de cabeza, torso, brazos y piernas; y admite casco y chaleco de tres niveles con reducción y durabilidad. El feedback provisional incluye hitmarker, headshot, impacto fatal e indicador de dirección. El detalle funcional está en `Docs/Diseno/Sistema_Dano_Completo.md`.

El loot de mundo incluye catálogo por tipo y rareza, generación ponderada por partida, objetos cercanos, recogida manual y parcial según capacidad, equipamiento de armas y protecciones, mochilas con capacidad y descarte al suelo. El detalle funcional está en `Docs/Diseno/Loot_Interaccion_Completo.md`.

### Prueba rápida del daño

En `07_BattleRoyaleTest` aparece durante Play Mode un objetivo de práctica con zonas corporales y protecciones. Los controles locales son `F5` para equipar casco/chaleco nivel 2, `F6` para daño de torso, `F7` para headshot, `F8` para explosión y `F9` para caída.

### Prueba rápida del loot

En `07_BattleRoyaleTest` aparece un área de loot unos diez metros delante del jugador. `F` recoge cualquier objeto seleccionado, `G` tira una unidad del último stack y `Shift + G` tira el stack completo.

## Control de versiones

El proyecto incluye `.gitignore` y `.gitattributes` apropiados para Unity. Para recursos binarios grandes se recomienda habilitar Git LFS en el repositorio antes de añadir grandes cantidades de FBX, audio, vídeo o texturas fuente.

## Historial de cambios

Notas consolidadas de los parches previos (antes repartidas en varios `README_*.txt`). Las instrucciones de instalación tipo "copiar Assets sobre la raíz" ya no aplican al trabajar en este repo único.

### Weapon Equipment System v1
- `WeaponEquipmentController` con slots (PrimarySlot1 / PrimarySlot2 / SidearmSlot) y `WeaponMount` por arma.
- Auto detección de sockets `Weapon_RightHand` / `Weapon_Back_01` / `Weapon_Back_02` / `Weapon_Hip` y auto binding a huesos Humanoid.
- `HasRifle` sincronizado con el Animator; Aim/ADS y `WeaponController` desactivados cuando no hay arma equipada.

### Combat V2
- Spread independiente para Hip Fire y ADS, dinámico según caminar/correr/sprint/agachado/aire.
- Bloom por disparo sostenido con recuperación automática.
- Valores de precisión movidos a `WeaponDefinition` para permitir comportamiento por arma.
- El recoil de cámara afecta los disparos siguientes porque el Aim usa la rotación real de la cámara.

### Weapon Equipment v3.1
- `WeaponMount` serializado de forma permanente en `Player_Prototype/WeaponRoot/PrototypeRifle`; offsets Hand/Back01/Back02/Hip visibles y persistentes en el Inspector fuera de Play Mode.
- `WeaponEquipmentController` mantiene el fallback en runtime para armas futuras sin `WeaponMount`.

### Combat Visual Integration v5
- Crosshair y HUD de combate sin requerir un Canvas en la escena.
- `PlayerAimController`, `WeaponEffects` y `WeaponRecoil` auto-creados/configurados por slot.
- Muzzle flash y tracer en runtime; prefabs de impacto y bullet-hole cargados desde Resources.
- `WeaponController` reconecta de forma lazy las referencias de Aim/Muzzle/Effects/Recoil.

### v5 Hotfix 1
- Corrige errores CS1061 donde `PlayerMotor`, `PlayerAnimatorDriver` y `ThirdPersonCamera` no encontraban `WeaponEquipmentController.CombatState`.
- Restaura `CombatState` (Unarmed / HipFire / Aiming / Reloading), los slots y la integración visual v5.

### CombatTest Animator Fix
- `PlayerAnimatorDriver` con referencia directa al Animator del personaje y `AC_Player_Prototype.controller` asignado.
- Apply Root Motion desactivado, de modo que `04_CombatTest` comparte la configuración de Animator de las variantes de `03_CharacterTest`.
