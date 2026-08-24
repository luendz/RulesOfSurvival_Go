using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Herramientas de preview del HUD Editor First.
    /// Permiten mostrar temporalmente elementos que normalmente comienzan
    /// ocultos, editarlos desde Hierarchy/Game View y luego restaurar el
    /// estado de inicio esperado por runtime.
    ///
    /// No crea UI en Play Mode y no forma parte de la logica runtime.
    /// </summary>
    public static class EditorFirstHudPreviewTools
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string HudPath =
            "01_RUNTIME_UI/HUD_ROS_EDITABLE";

        [MenuItem("Rules Of Survival/Editor First/HUD Preview/Show All HUD For Editing")]
        public static void ShowAllHudForEditing()
        {
            if (!TryGetHud(out Scene scene, out Transform hud))
                return;

            Transform canvas = hud.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[HUD Preview] No se encontro Canvas dentro de HUD_ROS_EDITABLE.");
                return;
            }

            SetHierarchyActive(canvas.Find("MatchStatePanel"), true);
            SetHierarchyActive(canvas.Find("KillFeedRoot"), true);
            SetHierarchyActive(canvas.Find("DamageDirectionRoot"), true);
            SetHierarchyActive(canvas.Find("EquipmentStatusRoot"), true);
            SetHierarchyActive(canvas.Find("QuickConsumeRoot"), true);
            SetHierarchyActive(canvas.Find("CombatFeedbackRoot"), true);
            SetHierarchyActive(canvas.Find("ConsumableProgressBar"), true);
            SetHierarchyActive(canvas.Find("NearbyObjectIndicator"), true);
            SetHierarchyActive(canvas.Find("DeathLootPanelROS"), true);

            SetPreviewAlpha(canvas, "DamageArrow_Front", 0.9f);
            SetPreviewAlpha(canvas, "DamageArrow_Right", 0.9f);
            SetPreviewAlpha(canvas, "DamageArrow_Back", 0.9f);
            SetPreviewAlpha(canvas, "DamageArrow_Left", 0.9f);
            SetPreviewAlpha(canvas, "DamageFeedback_Front", 0.72f);
            SetPreviewAlpha(canvas, "DamageFeedback_Right", 0.72f);
            SetPreviewAlpha(canvas, "DamageFeedback_Back", 0.72f);
            SetPreviewAlpha(canvas, "DamageFeedback_Left", 0.72f);

            SetText(canvas, "MatchStateTitle", "RUTA DEL AVIÓN");
            SetText(canvas, "MatchStateDetail", "[F / ESPACIO] SALTAR");
            SetText(canvas, "KillFeedRow_0", "TU  ▶  Bot_01");
            SetText(canvas, "KillFeedRow_1", "Bot_04  ▶  Bot_07");
            SetText(canvas, "KillFeedRow_2", "Bot_02  ▶  Bot_09");
            SetText(canvas, "KillFeedRow_3", "Bot_06  ▶  Bot_03");
            SetText(canvas, "KillFeedRow_4", "Bot_08  ▶  Bot_10");
            SetText(canvas, "HelmetStatus", "CASCO L3");
            SetText(canvas, "VestStatus", "CHALECO L3");
            SetText(canvas, "BackpackStatus", "MOCHILA L3");
            SetText(canvas, "ConsumableProgressLabel", "Usando botiquín…");
            SetText(canvas, "HeadshotLabel", "HEADSHOT");

            ConfigureQuickConsumePreview(canvas);
            ConfigureDeathLootPreview(canvas);
            ConfigureNearbyPreview(canvas);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = hud.gameObject;
            EditorGUIUtility.PingObject(hud.gameObject);
            SceneView.RepaintAll();

            Debug.Log(
                "[HUD Preview] Todos los elementos del HUD estan visibles para editar. " +
                "Cuando termines usa: Rules Of Survival > Editor First > HUD Preview > Restore Runtime Start State."
            );
        }

        [MenuItem("Rules Of Survival/Editor First/HUD Preview/Restore Runtime Start State")]
        public static void RestoreRuntimeStartState()
        {
            if (!TryGetHud(out Scene scene, out Transform hud))
                return;

            Transform canvas = hud.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[HUD Preview] No se encontro Canvas dentro de HUD_ROS_EDITABLE.");
                return;
            }

            // Elementos presentes siempre, pero con contenido dinamico oculto.
            SetActive(canvas.Find("KillFeedRoot"), true);
            SetActive(canvas.Find("DamageDirectionRoot"), true);
            SetActive(canvas.Find("EquipmentStatusRoot"), true);
            SetActive(canvas.Find("QuickConsumeRoot"), true);
            SetActive(canvas.Find("CombatFeedbackRoot"), true);

            SetChildrenActive(canvas.Find("KillFeedRoot"), false);
            SetQuickConsumeSlots(canvas, false);

            Transform hitmarker = FindRecursive(canvas, "HitmarkerRoot");
            SetActive(hitmarker, false);

            SetPreviewAlpha(canvas, "DamageArrow_Front", 0f);
            SetPreviewAlpha(canvas, "DamageArrow_Right", 0f);
            SetPreviewAlpha(canvas, "DamageArrow_Back", 0f);
            SetPreviewAlpha(canvas, "DamageArrow_Left", 0f);
            SetPreviewAlpha(canvas, "DamageFeedback_Front", 0f);
            SetPreviewAlpha(canvas, "DamageFeedback_Right", 0f);
            SetPreviewAlpha(canvas, "DamageFeedback_Back", 0f);
            SetPreviewAlpha(canvas, "DamageFeedback_Left", 0f);

            // Elementos que deben comenzar completamente ocultos.
            SetActive(canvas.Find("MatchStatePanel"), false);
            SetActive(canvas.Find("ConsumableProgressBar"), false);
            SetActive(canvas.Find("NearbyObjectIndicator"), false);
            SetActive(canvas.Find("DeathLootPanelROS"), false);

            SetText(canvas, "MatchStateTitle", string.Empty);
            SetText(canvas, "MatchStateDetail", string.Empty);
            for (int i = 0; i < 5; i++)
                SetText(canvas, "KillFeedRow_" + i, string.Empty);

            SetText(canvas, "HelmetStatus", "CASCO —");
            SetText(canvas, "VestStatus", "CHALECO —");
            SetText(canvas, "BackpackStatus", "MOCHILA —");
            SetText(canvas, "ConsumableProgressLabel", string.Empty);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();

            Debug.Log(
                "[HUD Preview] Estado runtime restaurado y escena 08 guardada. " +
                "Los elementos transitorios siguen existiendo fisicamente en Hierarchy."
            );
        }

        [MenuItem("Rules Of Survival/Editor First/HUD Preview/Select Editable HUD")]
        public static void SelectEditableHud()
        {
            if (!TryGetHud(out _, out Transform hud))
                return;

            Selection.activeGameObject = hud.gameObject;
            EditorGUIUtility.PingObject(hud.gameObject);
        }

        private static bool TryGetHud(out Scene scene, out Transform hud)
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            hud = null;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return false;

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            if (presentationRoot == null)
            {
                Debug.LogError(
                    "[HUD Preview] Falta __EDITOR_FIRST_PRESENTATION. Ejecuta primero " +
                    "Rules Of Survival > Editor First > Create Or Repair Functional Test Scene."
                );
                return false;
            }

            hud = presentationRoot.transform.Find(HudPath);
            if (hud == null)
            {
                Debug.LogError(
                    "[HUD Preview] No se encontro HUD_ROS_EDITABLE en la escena 08."
                );
                return false;
            }

            return true;
        }

        private static void ConfigureQuickConsumePreview(Transform canvas)
        {
            string[] names = { "BOTIQUÍN", "VENDAJE", "BEBIDA" };
            string[] counts = { "2", "5", "3" };

            for (int i = 0; i < 3; i++)
            {
                Transform slot = FindRecursive(canvas, "QuickConsumeSlot_" + i);
                if (slot == null)
                    continue;

                slot.gameObject.SetActive(true);
                Text name = slot.Find("Name")?.GetComponent<Text>();
                Text count = slot.Find("Count")?.GetComponent<Text>();
                if (name != null) name.text = names[i];
                if (count != null) count.text = counts[i];
            }
        }

        private static void ConfigureDeathLootPreview(Transform canvas)
        {
            Transform root = canvas.Find("DeathLootPanelROS");
            if (root == null)
                return;

            Text title = root.Find("Title/Text")?.GetComponent<Text>();
            if (title != null)
                title.text = "CAJA DE JUGADOR";

            Text footer = root.Find("Footer/Text")?.GetComponent<Text>();
            if (footer != null)
                footer.text = "1/7  •  RUEDA  •  F RECOGER  •  ESC";

            for (int i = 0; i < 7; i++)
            {
                Transform row = root.Find("Row_" + i);
                if (row == null)
                    continue;

                row.gameObject.SetActive(true);
                Text itemName = row.Find("Name")?.GetComponent<Text>();
                Text amount = row.Find("Amount")?.GetComponent<Text>();
                if (itemName != null)
                    itemName.text = i == 0 ? "M4A1" : "OBJETO " + (i + 1);
                if (amount != null)
                    amount.text = i == 0 ? "x1" : "x" + (i + 1);
            }
        }

        private static void ConfigureNearbyPreview(Transform canvas)
        {
            Transform root = canvas.Find("NearbyObjectIndicator");
            if (root == null)
                return;

            Text text = root.Find("Text")?.GetComponent<Text>();
            if (text != null)
                text.text = "OBJETO CERCANO";
        }

        private static void SetQuickConsumeSlots(Transform canvas, bool active)
        {
            for (int i = 0; i < 3; i++)
            {
                Transform slot = FindRecursive(canvas, "QuickConsumeSlot_" + i);
                SetActive(slot, active);
            }
        }

        private static void SetHierarchyActive(Transform root, bool active)
        {
            if (root == null)
                return;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            root.gameObject.SetActive(active);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    children[i].gameObject.SetActive(active);
            }
        }

        private static void SetChildrenActive(Transform root, bool active)
        {
            if (root == null)
                return;

            for (int i = 0; i < root.childCount; i++)
                root.GetChild(i).gameObject.SetActive(active);
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }

        private static void SetPreviewAlpha(Transform root, string name, float alpha)
        {
            Transform target = FindRecursive(root, name);
            Image image = target != null ? target.GetComponent<Image>() : null;
            if (image == null)
                return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static void SetText(Transform root, string objectName, string value)
        {
            Transform target = FindRecursive(root, objectName);
            Text text = target != null ? target.GetComponent<Text>() : null;
            if (text != null)
                text.text = value;
        }

        private static Transform FindRecursive(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == objectName)
                    return all[i];
            }

            return null;
        }
    }
}
