RulesOfSurvival_Go! - CombatTest Animator Fix

Este parche corrige el prefab base Player_Prototype para que:
- PlayerAnimatorDriver tenga referencia directa al Animator del personaje.
- AC_Player_Prototype.controller quede asignado al Animator del MainCharacter.
- Apply Root Motion permanezca desactivado.

Esto permite que 04_CombatTest, que usa Player_Prototype base, comparta la misma configuracion de Animator que las variantes usadas por 03_CharacterTest.

Instalacion:
1. Salir de Play Mode.
2. Copiar la carpeta Assets sobre la raiz del proyecto.
3. Reemplazar los archivos cuando Windows lo solicite.
4. Volver a Unity y esperar recompilacion/importacion.
5. Abrir 04_CombatTest y probar Play.
