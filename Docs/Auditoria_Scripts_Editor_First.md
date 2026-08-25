# Auditoría de scripts — `codex/editor-first-assets`

Fecha: 2026-08-25

## Resumen ejecutivo

Se auditaron 200 scripts propios del proyecto encontrados bajo `Assets/_Game`: 156 en `Scripts`, 36 en `Editor` y 8 pruebas automatizadas. Durante la auditoría se retiraron 5 clases de compatibilidad vacías y sus `.meta`, por lo que el inventario actual queda en 195 scripts.

Clasificación utilizada:

- **KEEP**: responsabilidad válida y diferenciada.
- **KEEP_EDITOR**: herramienta de autoría/editor que no pertenece al runtime.
- **TEST**: prueba automatizada.
- **DEMO_DEBUG**: soporte de demostración, bootstrap o depuración; no debería formar parte del runtime de producción final.
- **REFACTOR**: responsabilidad válida pero la ubicación/forma actual debe consolidarse.
- **REDUNDANT_REVIEW**: solapa responsabilidad con otro sistema y debe migrarse antes de eliminarse.
- **MIGRATION_TOOL**: herramienta Editor First de migración/reparación; conservar mientras haya escenas/assets antiguos que transformar y retirar cuando la migración esté cerrada.
- **REMOVED**: código vacío/obsoleto retirado de esta rama tras comprobar las escenas principales Editor First.

## Hallazgos principales

1. El HUD tiene dos capas de actualización superpuestas. `RulesOfSurvivalHUD` es el binder canónico del HUD editable, mientras `RulesOfSurvivalHUDFunctionality` vuelve a resolver y actualizar vida, armas, loot y minimapa. `RulesOfSurvivalHUDRuntimePolish` existe, en buena parte, para desactivar periódicamente los componentes heredados. Objetivo: migrar cualquier comportamiento único al HUD canónico y retirar la capa heredada.
2. `LobbySceneBootstrap` todavía construye cámara, luces, plataforma, personaje y, como fallback, UI en tiempo de ejecución. Esto contradice parcialmente el objetivo Editor First. `LobbyStageRemover` borra una plataforma que el propio bootstrap crea; debe desaparecer cuando el entorno quede completamente materializado. `LobbySliderBoundsFix` es otro parche runtime sobre UI generada dinámicamente.
3. Hay bootstraps y hotkeys de prueba dentro de ensamblados/runtime (`DemoBootstrap`, `DamagePracticeTargetBootstrap`, `DeathLootDemoBootstrap`, `LootSpawnerTestBootstrap`, `ParachuteDemoStarter`, `ParachuteTestRuntimeBootstrap`, etc.). Deben aislarse en escenas/tests o assembly de desarrollo, no mezclarse con producción.
4. Existen varios pares/grupos de presenters con responsabilidades solapadas: loot cercano, slots de armas, vitals/weapon HUD y HUD canónico. No se deben borrar de golpe porque algunas escenas antiguas pueden conservar GUID serializados; primero se migra la escena/prefab y después se elimina.
5. Hay numerosas herramientas Editor First `Repair`, `Migration`, `Materializer` y `Cleanup`. Son correctas durante la transición, pero no deberían convertirse en infraestructura permanente cuando la escena/prefab final ya esté materializada.

---

# Inventario completo

## AI

- **KEEP** `Scripts/AI/BattleRoyaleBotController.cs` — Controla comportamiento individual de bots de Battle Royale y localiza el jugador local cuando hace falta.
- **KEEP** `Scripts/AI/BattleRoyaleBotDirector.cs` — Coordina/gestiona el conjunto de bots de una partida.

## Animation

- **KEEP** `Scripts/Animation/PlayerAnimationCoordinator.cs` — Coordina estados/capas de animación del jugador entre locomoción y acciones.
- **KEEP** `Scripts/Animation/PlayerAnimatorDriver.cs` — Traduce estado jugable a parámetros del Animator.
- **KEEP** `Scripts/Animation/PlayerGestureController.cs` — Gestiona selección, reproducción y cancelación de gestos/emotes.

## Audio

- **KEEP** `Scripts/Audio/AirplaneAudioController.cs` — Audio asociado al avión de inicio de partida.
- **KEEP** `Scripts/Audio/CharacterAudioController.cs` — Sonidos del personaje, movimiento y acciones.
- **KEEP** `Scripts/Audio/RandomAudioPlayer.cs` — Reproductor genérico con selección aleatoria de clips.
- **KEEP** `Scripts/Audio/WeaponAudioController.cs` — Audio de disparo/recarga y eventos de armas.

## BattleRoyale

- **KEEP** `Scripts/BattleRoyale/BattleRoyaleManager.cs` — Orquesta estado de partida, jugadores vivos, inicio y final del Battle Royale.
- **KEEP** `Scripts/BattleRoyale/EliminationInfo.cs` — Contrato/datos de una eliminación.
- **KEEP** `Scripts/BattleRoyale/PlayerEliminationController.cs` — Reacciona a muerte/eliminación del jugador y coordina sus efectos.
- **KEEP** `Scripts/BattleRoyale/SafeZoneController.cs` — Lógica de zona segura y sus fases.
- **KEEP** `Scripts/BattleRoyale/SafeZoneVisual.cs` — Representación visual de la zona segura.
- **KEEP** `Scripts/BattleRoyale/SafeZoneWallVisual.cs` — Visual específico de la pared/límite de zona.

## Camera

- **KEEP** `Scripts/Camera/ThirdPersonCamera.cs` — Cámara de tercera persona, seguimiento y orientación.

## Character

- **KEEP** `Scripts/Character/BoneSocketFollower.cs` — Mantiene un objeto siguiendo un hueso/socket del esqueleto.
- **KEEP** `Scripts/Character/PlayerEquipmentVisualPresenter.cs` — Presenta visualmente equipo/armas sobre el modelo del jugador.
- **KEEP** `Scripts/Character/PlayerLeanController.cs` — Lógica de inclinación izquierda/derecha.
- **KEEP** `Scripts/Character/PlayerLeanRigApplier.cs` — Aplica la inclinación al rig/huesos correspondientes.
- **KEEP** `Scripts/Character/PlayerMotor.cs` — Movimiento físico principal del jugador.
- **KEEP** `Scripts/Character/PlayerVisualAdapter.cs` — Adapta/conecta el modelo visual con el controlador de personaje.

## Combat

- **KEEP** `Scripts/Combat/CharacterDeathDissolver.cs` — Efecto de desaparición/disolución tras muerte.
- **DEMO_DEBUG** `Scripts/Combat/DamageDebugControls.cs` — Controles de depuración para forzar/probar daño.
- **KEEP** `Scripts/Combat/DamageHitbox.cs` — Hitbox que recibe y enruta daño.
- **KEEP** `Scripts/Combat/DamageInfo.cs` — Datos de una solicitud/evento de daño.
- **DEMO_DEBUG** `Scripts/Combat/DamagePracticeTargetBootstrap.cs` — Construye/configura objetivo de práctica de daño.
- **KEEP** `Scripts/Combat/DamageResult.cs` — Resultado calculado de la aplicación de daño.
- **KEEP** `Scripts/Combat/DamageRules.cs` — Reglas/cálculos generales del daño.
- **KEEP** `Scripts/Combat/ExplosionDamageSource.cs` — Fuente de daño radial/explosivo.
- **KEEP** `Scripts/Combat/FallDamageReceiver.cs` — Calcula/aplica daño por caída.
- **KEEP** `Scripts/Combat/Health.cs` — Estado y operaciones de vida.
- **KEEP** `Scripts/Combat/IDamageable.cs` — Contrato para objetos que pueden recibir daño.
- **KEEP** `Scripts/Combat/PlayerAimController.cs` — Estado/lógica de apuntado del jugador.
- **KEEP** `Scripts/Combat/PlayerDamageHitboxRig.cs` — Configura/agrupa hitboxes del rig del jugador.
- **REFACTOR** `Scripts/Combat/PlayerDamageRuntimeSetup.cs` — Setup automático de componentes de daño; idealmente materializar en prefab cuando sea estable.
- **DEMO_DEBUG** `Scripts/Combat/PlayerDebugHealthHotkeys.cs` — Hotkeys para modificar vida durante pruebas.
- **KEEP** `Scripts/Combat/ProtectiveEquipment.cs` — Estado/absorción de casco y armadura.
- **DEMO_DEBUG** `Scripts/Combat/VitalsDebugTester.cs` — Pruebas manuales de vida/vitals.

## Core

- **DEMO_DEBUG** `Scripts/Core/DemoBootstrap.cs` — Registra jugadores, añade soporte de muerte y arranca una partida demo; además crea `MatchResultPresenter` si falta.
- **KEEP** `Scripts/Core/GameDataContracts.cs` — Contratos/estructuras de datos compartidas.
- **KEEP** `Scripts/Core/GameTypes.cs` — Enumeraciones/tipos de dominio compartidos.

## Scripts/Editor

- **KEEP_EDITOR** `Scripts/Editor/Audio/AudioSetupWizard.cs` — Asistente de configuración de audio en Editor.
- **KEEP_EDITOR** `Scripts/Editor/BattleRoyaleSetDressingBuilder.cs` — Construye/materializa set dressing del escenario BR.
- **KEEP_EDITOR** `Scripts/Editor/DedicatedServerBuild.cs` — Automatiza build dedicado/servidor.
- **KEEP_EDITOR** `Scripts/Editor/EnvironmentWallConverter.cs` — Convierte/configura paredes del entorno.
- **KEEP_EDITOR** `Scripts/Editor/LobbyHudAuthoringEditor.cs` — Herramientas Inspector/autoría para HUD del lobby.
- **KEEP_EDITOR** `Scripts/Editor/ParachuteMatchStartBuilder.cs` — Materializa/configura arranque y paracaídas en Editor.
- **KEEP_EDITOR** `Scripts/Editor/PickupAnimationSetup.cs` — Configura animación de pickups.
- **KEEP_EDITOR** `Scripts/Editor/ROSFirstRunInitializer.cs` — Inicialización del proyecto en primera apertura/configuración.
- **KEEP_EDITOR** `Scripts/Editor/ROSProjectSetup.cs` — Setup general del proyecto ROS.
- **KEEP_EDITOR** `Scripts/Editor/WeaponFamilyLootBuilder.cs` — Genera/configura familias de armas y loot asociado.

## Effects

- **KEEP** `Scripts/Effects/ImpactSurface.cs` — Describe/clasifica superficie para impactos y efectos.

## Gameplay

- **KEEP** `Scripts/Gameplay/ConsumableController.cs` — Uso y efectos de consumibles.

## Input

- **KEEP** `Scripts/Input/PlayerInputReader.cs` — Punto central de lectura de Input System para el jugador.

## Interaction

- **KEEP** `Scripts/Interaction/IInteractable.cs` — Contrato de objetos interactuables.
- **KEEP** `Scripts/Interaction/PlayerInteractor.cs` — Detecta y ejecuta interacciones del jugador.

## Inventory

- **KEEP** `Scripts/Inventory/ConsumableDefinition.cs` — Definición de datos de un consumible.
- **KEEP** `Scripts/Inventory/InventoryComponent.cs` — Estado y operaciones del inventario.
- **KEEP** `Scripts/Inventory/InventoryItemDefinition.cs` — Definición base de ítems de inventario.

## Lobby

- **KEEP** `Scripts/Lobby/LobbyBackgroundController.cs` — Control visual del fondo del lobby.
- **KEEP** `Scripts/Lobby/LobbyCameraController.cs` — Cámara del lobby y encuadre del personaje.
- **KEEP** `Scripts/Lobby/LobbyCharacterRotator.cs` — Rotación horizontal del personaje desde la UI/mouse.
- **KEEP** `Scripts/Lobby/LobbyColorGradeEffect.cs` — Ajustes/efecto de gradación visual del lobby.
- **REFACTOR** `Scripts/Lobby/LobbyDirectBRButton.cs` — Acceso directo a BR; revisar junto con la navegación final para evitar rutas duplicadas.
- **KEEP** `Scripts/Lobby/LobbyHudView.cs` — Referencias y binding del HUD editable del lobby.
- **KEEP** `Scripts/Lobby/LobbyIdleAnimator.cs` — Control de idle del personaje de lobby.
- **KEEP** `Scripts/Lobby/LobbyLightingController.cs` — Iluminación del lobby.
- **KEEP** `Scripts/Lobby/LobbyNavigationController.cs` — Navegación entre paneles/menús del lobby.
- **REFACTOR** `Scripts/Lobby/LobbySceneBootstrap.cs` — Orquesta lobby pero aún construye cámara, luces, stage y fallback UI en runtime; reducir a binder/orquestador Editor First.
- **KEEP** `Scripts/Lobby/LobbySession.cs` — Estado de sesión/selección entre lobby y partida.
- **KEEP** `Scripts/Lobby/LobbySettingsController.cs` — Opciones/configuración desde UI del lobby.
- **REDUNDANT_REVIEW** `Scripts/Lobby/LobbySliderBoundsFix.cs` — Parche posterior sobre sliders runtime; incorporar geometría correcta en UI editable.
- **REDUNDANT_REVIEW** `Scripts/Lobby/LobbyStageRemover.cs` — Borra el stage creado por `LobbySceneBootstrap`; debe desaparecer al dejar de crearlo.
- **KEEP** `Scripts/Lobby/LobbyTypes.cs` — Tipos/enums del dominio de lobby.

## Loot

- **KEEP** `Scripts/Loot/DeathLootContainer.cs` — Contenedor de loot generado al morir.
- **DEMO_DEBUG** `Scripts/Loot/DeathLootDemoBootstrap.cs` — Configuración/arranque de demo de death loot.
- **KEEP** `Scripts/Loot/DeathLootHalo.cs` — Halo/realce visual del contenedor de muerte.
- **KEEP** `Scripts/Loot/DeathLootVisualDefinition.cs` — Datos visuales del contenedor de muerte.
- **KEEP** `Scripts/Loot/LootDropController.cs` — Generación/caída de loot desde entidades.
- **KEEP** `Scripts/Loot/LootPickup.cs` — Objeto recogible e interacción de pickup.
- **REFACTOR** `Scripts/Loot/LootRuntimeSetup.cs` — Setup automático de loot; materializar en prefabs cuando sea estable.
- **KEEP** `Scripts/Loot/LootSpawner.cs` — Spawning de loot según reglas/tablas.
- **DEMO_DEBUG** `Scripts/Loot/LootSpawnerTestBootstrap.cs` — Bootstrap específico para probar el spawner.
- **KEEP** `Scripts/Loot/LootTableDefinition.cs` — Tabla/reglas de probabilidades y contenido de loot.
- **KEEP** `Scripts/Loot/PlayerLootEquipment.cs` — Equipamiento del jugador derivado de pickups/loot.

## Network

- **KEEP** `Scripts/Network/NetworkGameFacade.cs` — Fachada/abstracción de red para desacoplar gameplay del backend de networking.

## Parachute

- **REFACTOR** `Scripts/Parachute/BattleRoyaleMatchStartBootstrap.cs` — Bootstrap del flujo inicial; materializar referencias y reducir creación dinámica.
- **KEEP** `Scripts/Parachute/MatchStartController.cs` — Orquesta avión/salto/inicio de partida.
- **KEEP** `Scripts/Parachute/ParachuteController.cs` — Control de vuelo y estado de paracaídas.
- **DEMO_DEBUG** `Scripts/Parachute/ParachuteDemoStarter.cs` — Entrada rápida para demo/prueba de paracaídas.
- **KEEP** `Scripts/Parachute/ParachuteFlightMath.cs` — Cálculos matemáticos de vuelo/descenso.
- **DEMO_DEBUG** `Scripts/Parachute/ParachuteTestRuntimeBootstrap.cs` — Bootstrap runtime exclusivo de pruebas.

## Teams

- **KEEP** `Scripts/Teams/TeamComponent.cs` — Identidad/equipo de una entidad para reglas de combate/partida.

## UI

- **REFACTOR** `Scripts/UI/BattleRoyalePanelUI.cs` — Panel BR heredado/auxiliar; revisar convivencia con HUD ROS canónico.
- **REFACTOR** `Scripts/UI/BattleRoyaleStartMenu.cs` — Menú de inicio BR; revisar ruta única desde lobby Editor First.
- **KEEP** `Scripts/UI/BotHealthBar.cs` — Barra de vida sobre/para bots.
- **KEEP** `Scripts/UI/CombatFeedbackPresenter.cs` — Feedback de impactos/combate.
- **KEEP** `Scripts/UI/CombatWeaponHud.cs` — HUD específico del arma/combat state.
- **KEEP** `Scripts/UI/CompassUI.cs` — Presentación de brújula.
- **KEEP** `Scripts/UI/DamageDirectionIndicator.cs` — Indicadores direccionales de daño.
- **KEEP** `Scripts/UI/DamageNumberSpawner.cs` — Genera números de daño.
- **KEEP** `Scripts/UI/DeathLootPanelPresenter.cs` — Presenta contenido/interacción del loot de muerte.
- **KEEP** `Scripts/UI/EditorFirstHudRuntimeRoot.cs` — Marcador/raíz canónica para localizar el HUD Editor First.
- **KEEP** `Scripts/UI/EquipmentStatusPresenter.cs` — Estado visual de equipamiento/protección.
- **KEEP** `Scripts/UI/GestureWheelUI.cs` — Menú circular de gestos y selección.
- **REDUNDANT_REVIEW** `Scripts/UI/HudPresenter.cs` — Presenter HUD genérico; comprobar responsabilidades restantes frente al HUD ROS canónico.
- **KEEP** `Scripts/UI/InteractionPromptUI.cs` — Prompt visual de interacción.
- **KEEP** `Scripts/UI/KillFeedPresenter.cs` — Feed de eliminaciones.
- **KEEP** `Scripts/UI/LootIconHelper.cs` — Resolución/ayudas para iconos de loot.
- **KEEP** `Scripts/UI/MatchResultPresenter.cs` — Pantalla/feedback de resultado de partida.
- **KEEP** `Scripts/UI/MatchStartHud.cs` — Información del flujo inicial/avión/paracaídas.
- **KEEP** `Scripts/UI/MinimapSystem.cs` — Sistema/presentación de minimapa.
- **REDUNDANT_REVIEW** `Scripts/UI/NearbyLootPresenter.cs` — Presenter heredado de loot cercano; el `RuntimePolish` lo deshabilita en el HUD canónico.
- **REDUNDANT_REVIEW** `Scripts/UI/PlayerWeaponSlotsHudPresenter.cs` — Actualiza 5 slots físicos, pero el HUD canónico también actualiza slots; migrar cualquier detalle único y consolidar.
- **KEEP** `Scripts/UI/QuickConsumePresenter.cs` — Acceso rápido/estado visual de consumibles.
- **KEEP** `Scripts/UI/RulesOfSurvivalHUD.cs` — Binder principal del HUD ROS editable; fuente canónica a conservar.
- **REMOVED** `Scripts/UI/RulesOfSurvivalHUDFinePolish.cs` — Clase vacía de compatibilidad; retirada.
- **REDUNDANT_REVIEW** `Scripts/UI/RulesOfSurvivalHUDFunctionality.cs` — Segunda capa que vuelve a resolver/actualizar status, armas, loot y minimapa; consolidar en `RulesOfSurvivalHUD`.
- **REMOVED** `Scripts/UI/RulesOfSurvivalHUDNavigationPresenter.cs` — Clase vacía de compatibilidad; retirada.
- **REDUNDANT_REVIEW** `Scripts/UI/RulesOfSurvivalHUDNearbyLootPresenter.cs` — Presenter específico adicional de loot cercano; revisar frente al binding integrado del HUD.
- **REMOVED** `Scripts/UI/RulesOfSurvivalHUDPlayerStatusLayout.cs` — Clase vacía de compatibilidad; retirada.
- **REMOVED** `Scripts/UI/RulesOfSurvivalHUDPlayerStatusPresenter.cs` — Clase vacía de compatibilidad; retirada.
- **REDUNDANT_REVIEW** `Scripts/UI/RulesOfSurvivalHUDRuntimePolish.cs` — Shim que periódicamente desactiva componentes/hierarquías legacy; debe desaparecer después de la migración definitiva.
- **REMOVED** `Scripts/UI/RulesOfSurvivalHUDStabilityFix.cs` — Clase vacía de compatibilidad; retirada.
- **KEEP** `Scripts/UI/SafeZoneWarningUI.cs` — Avisos visuales de zona segura/peligro.
- **REDUNDANT_REVIEW** `Scripts/UI/VitalsPanelUI.cs` — Panel heredado de vida/vitals; el RuntimePolish lo deshabilita en el HUD canónico.
- **KEEP** `Scripts/UI/WeaponCrosshairPresenter.cs` — Presentación de mirilla según arma/estado.
- **REDUNDANT_REVIEW** `Scripts/UI/WeaponPanelUI.cs` — Panel de arma heredado; el RuntimePolish lo deshabilita en el HUD canónico.
- **KEEP** `Scripts/UI/WeaponSlotsPresenter.cs` — Presenter de slots usado por la capa de compatibilidad Editor First; mantener hasta cerrar consolidación.
- **KEEP** `Scripts/UI/ZoneTimerUI.cs` — Temporizador visual de la zona.

## Vehicles

- **KEEP** `Scripts/Vehicles/SimpleVehicleController.cs` — Conducción/control básico de vehículos.
- **KEEP** `Scripts/Vehicles/VehicleSeat.cs` — Gestión de asiento/ocupante e interacción con vehículo.

## Weapons

- **KEEP** `Scripts/Weapons/PlayerAuxiliaryWeaponSlots.cs` — Slots auxiliares (p. ej. melee/granadas) y selección.
- **KEEP** `Scripts/Weapons/PlayerWeaponSlotRules.cs` — Reglas de qué armas/objetos admite cada slot.
- **KEEP** `Scripts/Weapons/WeaponAmmoConnector.cs` — Sincroniza arma/equipamiento con munición/inventario.
- **KEEP** `Scripts/Weapons/WeaponBallistics.cs` — Trayectoria, raycast/proyectil y cálculos balísticos.
- **KEEP** `Scripts/Weapons/WeaponController.cs` — Estado operativo del arma: disparo, munición, recarga, modos.
- **KEEP** `Scripts/Weapons/WeaponDefinition.cs` — Datos/configuración de un arma.
- **KEEP** `Scripts/Weapons/WeaponEffects.cs` — VFX/feedback visual del arma.
- **KEEP** `Scripts/Weapons/WeaponEquipmentController.cs` — Equipar, cambiar y administrar armas del jugador.
- **KEEP** `Scripts/Weapons/WeaponLeftHandIKController.cs` — IK de mano izquierda sobre el arma.
- **KEEP** `Scripts/Weapons/WeaponMount.cs` — Puntos/transformaciones de montaje del arma.
- **KEEP** `Scripts/Weapons/WeaponRecoil.cs` — Retroceso y recuperación.

## World

- **KEEP** `Scripts/World/AirdropController.cs` — Flujo/movimiento/estado de airdrops.
- **KEEP** `Scripts/World/AirplaneController.cs` — Movimiento/control del avión inicial.
- **KEEP** `Scripts/World/AirplaneFlightEffects.cs` — Efectos visuales asociados al vuelo.
- **KEEP** `Scripts/World/BattleRoyaleGroundGradient.cs` — Tratamiento/gradiente visual del suelo BR.
- **REFACTOR** `Scripts/World/BattleRoyaleSetDressingBootstrap.cs` — Set dressing runtime; preferir materialización Editor First una vez estable.
- **KEEP** `Scripts/World/DoorController.cs` — Control genérico de puerta/interacción.
- **REDUNDANT_REVIEW** `Scripts/World/EchoValleyDoor.cs` — Implementación específica de puertas Echo Valley; revisar solape con `DoorController`.
- **REDUNDANT_REVIEW** `Scripts/World/EchoValleyDoorRuntime.cs` — Capa/runtime adicional para puertas Echo Valley; consolidar si sólo adapta creación dinámica.
- **KEEP_EDITOR/REFACTOR** `Scripts/World/EchoValleyMapAuthoring.cs` — Autoría/configuración del mapa; valorar mover a ensamblado Editor si usa API de autoría.
- **KEEP** `Scripts/World/GreyboxBuilding.cs` — Componente de edificios greybox/prototipo.
- **KEEP** `Scripts/World/PingMarker.cs` — Marcador/ping del mundo.
- **REFACTOR** `Scripts/World/WorldPOIBootstrap.cs` — Bootstrap de puntos de interés; materializar objetos definitivos cuando sea posible.

---

# Herramientas externas `Assets/_Game/Editor`

- **KEEP_EDITOR** `Editor/EditorFirstBattleRoyaleBotMaterializer.cs` — Materializa bots BR en escena.
- **KEEP_EDITOR** `Editor/EditorFirstBattleRoyaleSceneMaterializer.cs` — Materializa estructura principal de escena BR Editor First.
- **KEEP_EDITOR** `Editor/EditorFirstConsumableHudMaterializer.cs` — Materializa HUD de consumibles.
- **KEEP_EDITOR** `Editor/EditorFirstCrosshairMaterializer.cs` — Materializa/configura mirilla editable.
- **MIGRATION_TOOL** `Editor/EditorFirstCrouchAimUpperBodyMaterializer.cs` — Materializa configuración de upper body para crouch/aim.
- **KEEP_EDITOR** `Editor/EditorFirstDamageNumberMaterializer.cs` — Materializa soporte de números de daño.
- **MIGRATION_TOOL** `Editor/EditorFirstEmptyPlayerLoadoutMaterializer.cs` — Materializa estado inicial de loadout vacío.
- **KEEP_EDITOR** `Editor/EditorFirstFunctionalTestSceneBuilder.cs` — Construye/mantiene escena funcional de validación Editor First.
- **KEEP_EDITOR** `Editor/EditorFirstGestureHudHintMaterializer.cs` — Materializa pistas/UI de gestos.
- **MIGRATION_TOOL** `Editor/EditorFirstHealingUpperBodyMaterializer.cs` — Materializa configuración de animación upper-body para curación.
- **KEEP_EDITOR** `Editor/EditorFirstHudAndPlayerMaterializer.cs` — Materializa elementos físicos del HUD y componentes del jugador en escena 08.
- **KEEP_EDITOR** `Editor/EditorFirstHudBehaviorMaterializer.cs` — Materializa comportamiento/componentes necesarios del HUD.
- **MIGRATION_TOOL** `Editor/EditorFirstHudCompatibilityMaterializer.cs` — Añade presenters de compatibilidad al HUD mientras conviven capas antiguas/nuevas.
- **MIGRATION_TOOL** `Editor/EditorFirstHudHierarchyCleanup.cs` — Limpieza de jerarquía HUD durante migración.
- **KEEP_EDITOR** `Editor/EditorFirstHudPreviewTools.cs` — Herramientas de previsualización del HUD en Editor.
- **MIGRATION_TOOL** `Editor/EditorFirstLobbyBattleRoyaleTargetRepair.cs` — Repara target/ruta BR del lobby.
- **KEEP_EDITOR** `Editor/EditorFirstLootViewsMaterializer.cs` — Materializa vistas de loot.
- **KEEP_EDITOR** `Editor/EditorFirstMainPlayerRuntimeSupportMaterializer.cs` — Materializa soporte runtime necesario en el jugador principal.
- **MIGRATION_TOOL** `Editor/EditorFirstMenuCleanup.cs` — Limpieza de menús heredados durante transición.
- **KEEP_EDITOR** `Editor/EditorFirstNumberedSetupMenu.cs` — Menú numerado para ejecutar pasos de setup Editor First.
- **DEMO_DEBUG/KEEP_EDITOR** `Editor/EditorFirstPlayerDebugHealthMaterializer.cs` — Materializa soporte de depuración de vida; no debe entrar en producción final.
- **KEEP_EDITOR** `Editor/EditorFirstPlayerEquipmentVisualMaterializer.cs` — Materializa presentación física de equipo del jugador.
- **KEEP_EDITOR** `Editor/EditorFirstPresentationBuilder.cs` — Builder de presentación/jerarquía visual Editor First.
- **MIGRATION_TOOL** `Editor/EditorFirstReloadUpperBodyRepair.cs` — Repara/migra configuración de recarga upper-body.
- **MIGRATION_TOOL** `Editor/EditorFirstRifleAmmoMigration.cs` — Migración puntual de datos/configuración de munición de rifle.
- **MIGRATION_TOOL** `Editor/EditorFirstRosWeaponSlotSerializedRepair.cs` — Repara datos serializados de slots de armas ROS.
- **KEEP_EDITOR** `Editor/EditorFirstRosWeaponSlotVisualMaterializer.cs` — Materializa visuales de slots ROS.
- **KEEP_EDITOR** `Editor/EditorFirstRosWeaponSlotsMaterializer.cs` — Materializa slots ROS en escena/HUD.
- **MIGRATION_TOOL** `Editor/EditorFirstStartMenuControllerNormalizer.cs` — Normaliza controlador del menú de inicio durante migración.
- **MIGRATION_TOOL** `Editor/EditorFirstStartMenuSceneRepair.cs` — Repara referencias/estructura del start menu en escena.
- **MIGRATION_TOOL** `Editor/EditorFirstUnifiedAnimationLegacyMigration.cs` — Migra configuración de animación legacy al sistema unificado.
- **KEEP_EDITOR** `Editor/EditorFirstUnifiedAnimationMaterializer.cs` — Materializa sistema de animación unificada.
- **MIGRATION_TOOL** `Editor/EditorFirstUnifiedAnimationStateRepair.cs` — Repara estados del sistema de animación unificada.
- **KEEP_EDITOR** `Editor/EditorFirstWeaponBackMountMaterializer.cs` — Materializa punto de arma en espalda.
- **KEEP_EDITOR** `Editor/EditorFirstWeaponEffectsMaterializer.cs` — Materializa efectos de armas.
- **KEEP_EDITOR** `Editor/GestureAnimatorConfigurator.cs` — Configura Animator/controlador para gestos.

---

# Tests

## EditMode

- **TEST** `Tests/EditMode/DamageSystemTests.cs` — Pruebas del sistema de daño.
- **TEST** `Tests/EditMode/GameDataContractTests.cs` — Pruebas de contratos/tipos de datos.
- **TEST** `Tests/EditMode/HealthTests.cs` — Pruebas de vida.
- **TEST** `Tests/EditMode/InventoryComponentTests.cs` — Pruebas del inventario.
- **TEST** `Tests/EditMode/LootInteractionTests.cs` — Pruebas de interacción con loot.
- **TEST** `Tests/EditMode/LootTableDefinitionTests.cs` — Pruebas de tablas de loot.
- **TEST** `Tests/EditMode/ParachuteMatchStartTests.cs` — Pruebas de paracaídas/inicio de partida.

## PlayMode

- **TEST** `Tests/PlayMode/BattleRoyaleFlowPlayModeTests.cs` — Prueba integrada del flujo BR en Play Mode.

---

# Candidatos a eliminación / consolidación

## Eliminados ya

Los siguientes cinco scripts eran clases `MonoBehaviour` vacías cuyo comentario indicaba expresamente que su responsabilidad ya vive en el prefab/hierarquía editable. Sus GUID no estaban presentes en `07_BattleRoyaleTest` ni `08_EditorFirstFunctionalTest` al auditar la rama, y fueron retirados junto con sus `.meta`:

1. `RulesOfSurvivalHUDFinePolish.cs`
2. `RulesOfSurvivalHUDNavigationPresenter.cs`
3. `RulesOfSurvivalHUDPlayerStatusLayout.cs`
4. `RulesOfSurvivalHUDPlayerStatusPresenter.cs`
5. `RulesOfSurvivalHUDStabilityFix.cs`

## Consolidación HUD prioritaria

Orden recomendado:

1. Tomar `RulesOfSurvivalHUD` como única fuente de actualización de HUD.
2. Comparar funciones únicas de `RulesOfSurvivalHUDFunctionality` y migrarlas al canónico.
3. Consolidar loot cercano: `NearbyLootPresenter` / `RulesOfSurvivalHUDNearbyLootPresenter` / binding integrado de `RulesOfSurvivalHUD`.
4. Consolidar slots: `PlayerWeaponSlotsHudPresenter` / `WeaponSlotsPresenter` / binding integrado del canónico.
5. Retirar `VitalsPanelUI` y `WeaponPanelUI` de las escenas ya migradas.
6. Cuando ninguna jerarquía legacy exista, retirar `RulesOfSurvivalHUDRuntimePolish`.

## Consolidación Lobby prioritaria

1. Materializar cámara, luces, personaje y entorno de `08_Lobby` en escena/prefab.
2. Reducir `LobbySceneBootstrap` a resolver referencias, vincular sesión y comenzar partida.
3. Desactivar/eliminar el fallback `BuildUi()` una vez todas las escenas objetivo tengan `LobbyHudView` autorado.
4. Dejar de crear `Lobby Character Stage`; entonces eliminar `LobbyStageRemover`.
5. Autorizar correctamente sliders en escena; entonces eliminar `LobbySliderBoundsFix`.

## Código de demo/debug

Mover a un assembly/carpeta de desarrollo o conservar únicamente en escenas de prueba:

- `DemoBootstrap`
- `DamageDebugControls`
- `DamagePracticeTargetBootstrap`
- `PlayerDebugHealthHotkeys`
- `VitalsDebugTester`
- `DeathLootDemoBootstrap`
- `LootSpawnerTestBootstrap`
- `ParachuteDemoStarter`
- `ParachuteTestRuntimeBootstrap`
- `EditorFirstPlayerDebugHealthMaterializer`

## Herramientas de migración que deben tener fecha de salida

Cuando todas las escenas/prefabs hayan sido convertidos y validados, revisar para retirada: `EditorFirstHudCompatibilityMaterializer`, `EditorFirstHudHierarchyCleanup`, `EditorFirstLobbyBattleRoyaleTargetRepair`, `EditorFirstMenuCleanup`, `EditorFirstReloadUpperBodyRepair`, `EditorFirstRifleAmmoMigration`, `EditorFirstRosWeaponSlotSerializedRepair`, `EditorFirstStartMenuControllerNormalizer`, `EditorFirstStartMenuSceneRepair`, `EditorFirstUnifiedAnimationLegacyMigration` y `EditorFirstUnifiedAnimationStateRepair`.

---

# Estado objetivo

La rama debería terminar con esta regla simple:

- **Runtime**: sólo gameplay y binding de objetos ya existentes.
- **Editor**: materializa/configura objetos y assets.
- **Tests/Debug**: aislados del runtime de producción.
- **HUD/Lobby**: una jerarquía editable + un binder principal por dominio; sin scripts periódicos dedicados a apagar otros scripts.
