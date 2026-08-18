RulesOfSurvival_Go! - Weapon Equipment v3.1

Fix:
- WeaponMount is now serialized permanently on Player_Prototype/WeaponRoot/PrototypeRifle.
- Its Hand / Back01 / Back02 / Hip offsets are visible and persistent in the Inspector outside Play Mode.
- WeaponEquipmentController keeps the runtime fallback for future weapons that do not yet have WeaponMount.

Install:
1. Exit Play Mode.
2. Close Unity (recommended).
3. Copy the Assets folder over the project root and replace files.
4. Open Unity and wait for compilation/import.
5. Open 04_CombatTest.
6. Select Player_Prototype > WeaponRoot > PrototypeRifle.
7. Weapon Mount should now be visible in the Inspector.

Do not tune offsets in Play Mode if you want them to persist. Tune the prefab outside Play Mode, then test 1 / X.
