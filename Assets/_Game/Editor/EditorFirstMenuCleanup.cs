using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Mantiene el menu de Unity enfocado en las herramientas Editor First que
    /// siguen siendo utiles. Los comandos legacy permanecen implementados para
    /// que los procesos automaticos puedan reutilizarlos, pero ya no se exponen
    /// como opciones manuales del editor.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstMenuCleanup
    {
        private static readonly string[] ObsoleteMenuPaths =
        {
            // Menus raiz legacy.
            "ROS Battle Royale",
            "ROS",

            // Flujo numerado de consolidacion/migracion ya completado.
            "Rules Of Survival/Editor First/00 - Ejecutar consolidacion completa",
            "Rules Of Survival/Editor First/01 - Consolidar Animator Upper Lower",
            "Rules Of Survival/Editor First/02 - Materializar F5 menos 5 HP",
            "Rules Of Survival/Editor First/03 - Verificar Reload Upper Body",
            "Rules Of Survival/Editor First/04 - Reparar escena funcional 08",

            // Herramientas internas de materializacion, migracion y reparacion.
            "Rules Of Survival/Editor First/Apply Standard Back Weapon Mounts",
            "Rules Of Survival/Editor First/Clean Obsolete HUD Hierarchy",
            "Rules Of Survival/Editor First/Configure Crouch Aim Upper Body",
            "Rules Of Survival/Editor First/Configure Healing Upper Body",
            "Rules Of Survival/Editor First/Consolidate Player Upper Lower Animation",
            "Rules Of Survival/Editor First/Create Or Repair Functional Test Scene",
            "Rules Of Survival/Editor First/Ensure Editable Damage Number Style",
            "Rules Of Survival/Editor First/Ensure Editable Weapon Crosshair",
            "Rules Of Survival/Editor First/Ensure Editable Weapon Effects",
            "Rules Of Survival/Editor First/Ensure HUD Behaviors On Prefab",
            "Rules Of Survival/Editor First/Ensure Unified Nearby Loot",
            "Rules Of Survival/Editor First/Fix BR Menu Cursor Controller",
            "Rules Of Survival/Editor First/Fix Lobby BR Target",
            "Rules Of Survival/Editor First/Materialize Battle Royale Bots",
            "Rules Of Survival/Editor First/Materialize Consumable HUD",
            "Rules Of Survival/Editor First/Materialize Gesture HUD Hint",
            "Rules Of Survival/Editor First/Materialize HUD And Main Player",
            "Rules Of Survival/Editor First/Materialize HUD Compatibility Components",
            "Rules Of Survival/Editor First/Materialize Main Player Runtime Support",
            "Rules Of Survival/Editor First/Materialize Missing Presentation Assets",
            "Rules Of Survival/Editor First/Materialize Player Equipment Visuals",
            "Rules Of Survival/Editor First/Materialize ROS Weapon Slot Visuals",
            "Rules Of Survival/Editor First/Materialize ROS Weapon Slots",
            "Rules Of Survival/Editor First/Migrate Legacy 5.56 To Rifle Ammo",
            "Rules Of Survival/Editor First/Migrate Legacy Upper Body Layers",
            "Rules Of Survival/Editor First/Open Original Battle Royale Editable Hierarchy",
            "Rules Of Survival/Editor First/Put Everything In Original Battle Royale Hierarchy",
            "Rules Of Survival/Editor First/Repair Consolidated Upper Body Motions",
            "Rules Of Survival/Editor First/Repair Functional Test Menu",
            "Rules Of Survival/Editor First/Repair Serialized ROS Weapon Slots",
            "Rules Of Survival/Editor First/Reset Main Player To Empty Loadout",
            "Rules Of Survival/Editor First/Select Editable HUD Prefab",
            "Rules Of Survival/Editor First/Validate Runtime Presentation Creation"
        };

        private static readonly MethodInfo RemoveMenuItemMethod =
            typeof(Menu).GetMethod(
                "RemoveMenuItem",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null
            );

        static EditorFirstMenuCleanup()
        {
            // Espera a que Unity termine de registrar los MenuItem declarativos.
            EditorApplication.delayCall += FirstCleanupPass;
        }

        private static void FirstCleanupPass()
        {
            CleanupMenus();

            // Segunda pasada para comandos registrados tarde por otros scripts.
            EditorApplication.delayCall += CleanupMenus;
        }

        private static void CleanupMenus()
        {
            if (RemoveMenuItemMethod == null)
            {
                Debug.LogWarning(
                    "[Editor First] Unity no expuso RemoveMenuItem; no se pudo limpiar el menu legacy."
                );
                return;
            }

            for (int i = 0; i < ObsoleteMenuPaths.Length; i++)
            {
                try
                {
                    RemoveMenuItemMethod.Invoke(null, new object[] { ObsoleteMenuPaths[i] });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[Editor First] No se pudo ocultar el menu '" +
                        ObsoleteMenuPaths[i] + "': " + exception.Message
                    );
                }
            }
        }

        [MenuItem("Rules Of Survival/Editor First/Validate Editor First", false, 100)]
        public static void ValidateEditorFirst()
        {
            MethodInfo validateMethod = typeof(EditorFirstPresentationBuilder).GetMethod(
                "ValidateRuntimePresentationCreation",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (validateMethod == null)
            {
                Debug.LogError(
                    "[Editor First] No se encontro la validacion de presentacion Editor First."
                );
                return;
            }

            validateMethod.Invoke(null, null);
        }

        [MenuItem("Rules Of Survival/Editor First/Repair - Rebuild Editor First", false, 101)]
        public static void RepairOrRebuildEditorFirst()
        {
            EditorFirstFunctionalTestSceneBuilder.EnsureFunctionalTestScene();
        }
    }
}
