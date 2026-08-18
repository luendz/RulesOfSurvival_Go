RulesOfSurvival_Go v5 Hotfix 1

Corrige los errores CS1061 donde PlayerMotor, PlayerAnimatorDriver y ThirdPersonCamera no encontraban WeaponEquipmentController.CombatState.

Causa:
El parche v5 reemplazó WeaponEquipmentController por una variante anterior que no incluía CombatState, aunque otros scripts ya dependían de esa propiedad.

Este hotfix conserva:
- CombatState: Unarmed / HipFire / Aiming / Reloading
- PrimarySlot1 / PrimarySlot2 / SidearmSlot
- SetWeaponInSlot / SlotChanged
- Runtime Debug
- Integración visual v5: PlayerAimController, HudPresenter, WeaponEffects y WeaponRecoil automáticos

Instalación:
1. Salir de Play Mode.
2. Copiar Assets sobre la raíz del proyecto.
3. Aceptar reemplazo.
4. Esperar recompilación.
