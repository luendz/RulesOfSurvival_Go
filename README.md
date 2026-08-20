# RulesOfSurvival_Go!

Proyecto base Battle Royale 3D desarrollado en Unity 6000.3.11f1.

## Abrir el proyecto

Abra esta carpeta desde Unity Hub usando Unity 6000.3.11f1. No es necesario versionar ni copiar `Library`, `Temp`, `Logs` u `obj`; Unity las regenera.

## Estructura principal

- `Assets/_Game/Scripts/Character`: movimiento y representación del jugador.
- `Assets/_Game/Scripts/Camera`: cámara en tercera persona, aim y recoil visual.
- `Assets/_Game/Scripts/Weapons`: definición, disparo, efectos y recoil de armas.
- `Assets/_Game/Scripts/Combat`: vida, daño y apuntado.
- `Assets/_Game/Scripts/Inventory`, `Loot`, `Interaction`: base de inventario y pickups.
- `Assets/_Game/Scripts/BattleRoyale`: estado de partida y zona segura.
- `Assets/_Game/Scripts/Vehicles`, `Parachute`, `World`: prototipos de sistemas posteriores.

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

La locomoción, cámara, crouch, aim y el prototipo de rifle ya forman la base jugable. El rifle incluye munición, recarga, raycast de impacto, muzzle flash, tracer, partículas de impacto, bullet hole y recoil. Los sistemas de Battle Royale, vehículos, paracaídas, inventario y loot son bases que todavía requieren integración y pruebas de gameplay.

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
