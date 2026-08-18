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

## Estado actual

La locomoción, cámara, crouch, aim y el prototipo de rifle ya forman la base jugable. El rifle incluye munición, recarga, raycast de impacto, muzzle flash, tracer, partículas de impacto, bullet hole y recoil. Los sistemas de Battle Royale, vehículos, paracaídas, inventario y loot son bases que todavía requieren integración y pruebas de gameplay.

## Control de versiones

El proyecto incluye `.gitignore` y `.gitattributes` apropiados para Unity. Para recursos binarios grandes se recomienda habilitar Git LFS en el repositorio antes de añadir grandes cantidades de FBX, audio, vídeo o texturas fuente.
