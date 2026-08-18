RulesOfSurvival_Go! - Weapon Equipment System v1

Controles:
1 = equipar arma primaria slot 1
2 = equipar arma primaria slot 2 (si existe)
3 = equipar arma secundaria/sidearm (si existe)
X = guardar arma actual

Incluye:
- WeaponEquipmentController
- WeaponMount por arma
- Auto deteccion de Weapon_RightHand / Weapon_Back_01 / Weapon_Back_02 / Weapon_Hip
- Auto binding de sockets a huesos Humanoid
- HasRifle sincronizado con Animator
- Aim/ADS desactivado cuando no hay arma equipada
- WeaponController desactivado cuando el arma esta guardada
- Base preparada para multiples armas

Prueba inicial recomendada en 04_CombatTest:
1) Al entrar debe equipar Slot 1 automaticamente.
2) X debe guardar el arma en Weapon_Back_01 y volver a locomocion sin rifle.
3) Click derecho con arma guardada no debe entrar en ADS.
4) Click izquierdo con arma guardada no debe disparar.
5) 1 debe volver a equipar el arma y reactivar Aim/disparo.
6) 2 y 3 no hacen nada todavia si esos slots estan vacios.

Nota: los offsets visuales del arma se ajustan mediante WeaponMount. La version actual usa offsets genericos;
cuando coloquemos el M4/AK definitivos ajustaremos esos valores por prefab.
