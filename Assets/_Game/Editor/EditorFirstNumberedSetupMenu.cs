using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    public static class EditorFirstNumberedSetupMenu
    {
        [MenuItem("Rules Of Survival/Editor First/00 - Ejecutar consolidacion completa")]
        public static void RunAll()
        {
            Step01();
            Step02();
            Step03();
            Step04();

            Debug.Log(
                "[Editor First] Secuencia completa ejecutada: 01 Animator, 02 F5, 03 Reload, 04 Escena 08."
            );
        }

        [MenuItem("Rules Of Survival/Editor First/01 - Consolidar Animator Upper Lower")]
        public static void Step01()
        {
            EditorFirstUnifiedAnimationMaterializer.Materialize();
        }

        [MenuItem("Rules Of Survival/Editor First/02 - Materializar F5 menos 5 HP")]
        public static void Step02()
        {
            bool changed = EditorFirstPlayerDebugHealthMaterializer.Materialize();
            Debug.Log(
                changed
                    ? "[Editor First] F5 agregado al jugador principal: -5 HP directos."
                    : "[Editor First] F5 ya estaba configurado en el jugador principal."
            );
        }

        [MenuItem("Rules Of Survival/Editor First/03 - Verificar Reload Upper Body")]
        public static void Step03()
        {
            EditorFirstReloadUpperBodyRepair.Repair();
        }

        [MenuItem("Rules Of Survival/Editor First/04 - Reparar escena funcional 08")]
        public static void Step04()
        {
            EditorFirstFunctionalTestSceneBuilder.EnsureFunctionalTestScene();
        }
    }
}
