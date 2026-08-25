using UnityEngine;

namespace ROS.Game.EditorTools
{
    public static class EditorFirstNumberedSetupMenu
    {
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

        public static void Step01()
        {
            EditorFirstUnifiedAnimationMaterializer.Materialize();
        }

        public static void Step02()
        {
            bool changed = EditorFirstPlayerDebugHealthMaterializer.Materialize();
            Debug.Log(
                changed
                    ? "[Editor First] F5 agregado al jugador principal: -5 HP directos."
                    : "[Editor First] F5 ya estaba configurado en el jugador principal."
            );
        }

        public static void Step03()
        {
            EditorFirstReloadUpperBodyRepair.Repair();
        }

        public static void Step04()
        {
            EditorFirstFunctionalTestSceneBuilder.EnsureFunctionalTestScene();
        }
    }
}
