using ROS.Game.BattleRoyale;
using ROS.Game.CameraSystem;
using ROS.Game.Core;
using ROS.Game.Input;
using ROS.Game.Parachute;
using ROS.Game.UI;
using ROS.Game.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROS.Game.EditorTools
{
    /// <summary>
    /// Repara la escena funcional Editor First y deja el flujo de inicio de BR
    /// materializado como objetos reales de escena: menu, avion, paracaidas,
    /// MatchStartController y EventSystem. No depende del bootstrap runtime de 07.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorFirstStartMenuSceneRepair
    {
        private const string ScenePath =
            "Assets/_Game/Scenes/08_EditorFirstFunctionalTest.unity";

        private const string MenuPath =
            "01_RUNTIME_UI/MENU_BATTLE_ROYALE_EDITABLE";

        private const string ParachuteResource =
            "Parachute/PF_ParachuteVisual";

        private const string AirplaneResource =
            "Parachute/PF_AirplaneStart";

        private static readonly Vector3 RouteStart =
            new Vector3(-90f, 105f, -35f);

        private static readonly Vector3 RouteEnd =
            new Vector3(90f, 105f, 35f);

        static EditorFirstStartMenuSceneRepair()
        {
            EditorApplication.delayCall += RepairFunctionalSceneMenu;
        }

        [MenuItem("Rules Of Survival/Editor First/Repair Functional Test Menu")]
        public static void RepairFunctionalSceneMenu()
        {
            if (Application.isPlaying || EditorApplication.isCompiling)
                return;

            if (!System.IO.File.Exists(ScenePath))
                return;

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

            if (openedTemporarily)
            {
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Additive
                );
            }

            if (!scene.IsValid() || !scene.isLoaded)
                return;

            bool changed = false;

            GameObject presentationRoot =
                EditorFirstBattleRoyaleSceneMaterializer.FindPresentationRoot(scene);

            Transform physicalMenu =
                presentationRoot != null
                    ? presentationRoot.transform.Find(MenuPath)
                    : null;

            PlayerInputReader input = FindInScene<PlayerInputReader>(scene);
            BattleRoyaleManager manager = FindInScene<BattleRoyaleManager>(scene);
            ThirdPersonCamera playerCamera = FindInScene<ThirdPersonCamera>(scene);

            MatchStartController sequence = null;
            if (input != null && manager != null)
            {
                sequence = EnsurePhysicalMatchStartFlow(
                    scene,
                    input,
                    manager,
                    ref changed
                );
            }
            else
            {
                Debug.LogError(
                    "[Editor First] No se puede preparar el inicio BR: " +
                    "faltan PlayerInputReader o BattleRoyaleManager en la escena 08."
                );
            }

            if (physicalMenu != null)
            {
                BattleRoyaleStartMenu controller = FindController(scene, physicalMenu);
                if (controller == null)
                {
                    controller = physicalMenu.gameObject.AddComponent<BattleRoyaleStartMenu>();
                    changed = true;
                }

                Button startButton = FindNamedButton(
                    physicalMenu,
                    "StartMatchButton"
                );
                Button freeroamButton = FindNamedButton(
                    physicalMenu,
                    "FreeroamButton"
                );

                SerializedObject serialized = new SerializedObject(controller);
                changed |= SetObjectReference(
                    serialized,
                    "viewRoot",
                    physicalMenu.gameObject
                );
                changed |= SetObjectReference(
                    serialized,
                    "startMatchButton",
                    startButton
                );
                changed |= SetObjectReference(
                    serialized,
                    "freeroamButton",
                    freeroamButton
                );
                changed |= SetObjectReference(serialized, "sequence", sequence);
                changed |= SetObjectReference(serialized, "input", input);
                changed |= SetObjectReference(
                    serialized,
                    "playerCamera",
                    playerCamera
                );

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            DemoBootstrap demo = FindInScene<DemoBootstrap>(scene);
            if (demo != null)
                demo.SetBeginOnStart(false);

            changed |= EnsureEventSystem(scene);

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[Editor First] Flujo de inicio Battle Royale materializado y conectado en 08_EditorFirstFunctionalTest."
                );
            }

            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static MatchStartController EnsurePhysicalMatchStartFlow(
            Scene scene,
            PlayerInputReader input,
            BattleRoyaleManager manager,
            ref bool changed
        )
        {
            ParachuteController parachute = input.GetComponent<ParachuteController>();
            if (parachute == null)
            {
                parachute = input.gameObject.AddComponent<ParachuteController>();
                changed = true;
            }

            GameObject parachuteVisual = null;
            Transform existingVisual = input.transform.Find("BattleRoyaleParachuteVisual");
            if (existingVisual != null)
            {
                parachuteVisual = existingVisual.gameObject;
            }
            else
            {
                GameObject parachutePrefab = Resources.Load<GameObject>(ParachuteResource);
                if (parachutePrefab != null)
                {
                    parachuteVisual = PrefabUtility.InstantiatePrefab(parachutePrefab) as GameObject;
                    if (parachuteVisual != null)
                    {
                        SceneManager.MoveGameObjectToScene(parachuteVisual, scene);
                        parachuteVisual.name = "BattleRoyaleParachuteVisual";
                        parachuteVisual.transform.SetParent(input.transform, false);
                        parachuteVisual.transform.localPosition = new Vector3(0f, 3.2f, 0f);
                        parachuteVisual.transform.localRotation = Quaternion.identity;

                        if (parachuteVisual.transform.childCount > 0)
                        {
                            parachuteVisual.transform.GetChild(0).localRotation =
                                Quaternion.Euler(ParachuteController.ModelEulerAngles);
                        }

                        parachuteVisual.SetActive(false);
                        changed = true;
                    }
                }
                else
                {
                    Debug.LogError(
                        "[Editor First] No se encontro el prefab de paracaidas: " +
                        ParachuteResource
                    );
                }
            }

            if (parachuteVisual != null)
            {
                SerializedObject parachuteSerialized = new SerializedObject(parachute);
                changed |= SetObjectReference(
                    parachuteSerialized,
                    "parachuteVisual",
                    parachuteVisual
                );
                parachuteSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(parachute);
            }

            AirplaneController airplane = FindInScene<AirplaneController>(scene);
            if (airplane == null)
            {
                GameObject airplanePrefab = Resources.Load<GameObject>(AirplaneResource);
                if (airplanePrefab == null)
                {
                    Debug.LogError(
                        "[Editor First] No se encontro el prefab de avion: " +
                        AirplaneResource
                    );
                    return null;
                }

                GameObject airplaneObject =
                    PrefabUtility.InstantiatePrefab(airplanePrefab) as GameObject;

                if (airplaneObject == null)
                    return null;

                SceneManager.MoveGameObjectToScene(airplaneObject, scene);
                airplaneObject.name = "Airplane_BattleRoyale";

                if (airplaneObject.transform.childCount > 0)
                {
                    airplaneObject.transform.GetChild(0).localRotation =
                        Quaternion.Euler(AirplaneController.ModelEulerAngles);
                }

                airplane = airplaneObject.GetComponent<AirplaneController>();
                if (airplane == null)
                    airplane = airplaneObject.AddComponent<AirplaneController>();

                if (airplaneObject.GetComponent<AirplaneFlightEffects>() == null)
                    airplaneObject.AddComponent<AirplaneFlightEffects>();

                changed = true;
            }

            // Fuerza a que PassengerAnchor exista fisicamente y quede guardado.
            Transform passengerAnchor = airplane.PassengerAnchor;
            if (passengerAnchor != null)
                EditorUtility.SetDirty(passengerAnchor.gameObject);

            airplane.PrepareRoute(RouteStart, RouteEnd);
            EditorUtility.SetDirty(airplane);

            MatchStartController sequence = FindInScene<MatchStartController>(scene);
            if (sequence == null)
            {
                GameObject flowObject = new GameObject("BattleRoyaleMatchStart");
                SceneManager.MoveGameObjectToScene(flowObject, scene);
                sequence = flowObject.AddComponent<MatchStartController>();
                changed = true;
            }

            SerializedObject sequenceSerialized = new SerializedObject(sequence);
            changed |= SetObjectReference(sequenceSerialized, "matchManager", manager);
            changed |= SetObjectReference(sequenceSerialized, "airplane", airplane);
            changed |= SetObjectReference(
                sequenceSerialized,
                "playerParachute",
                parachute
            );
            changed |= SetObjectReference(sequenceSerialized, "input", input);
            changed |= SetBool(sequenceSerialized, "startOnStart", false);
            changed |= SetFloat(sequenceSerialized, "warmupDuration", 0f);
            changed |= SetFloat(sequenceSerialized, "flightDuration", 28f);
            changed |= SetVector3(sequenceSerialized, "routeStart", RouteStart);
            changed |= SetVector3(sequenceSerialized, "routeEnd", RouteEnd);
            sequenceSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(sequence);

            return sequence;
        }

        private static BattleRoyaleStartMenu FindController(
            Scene scene,
            Transform physicalMenu
        )
        {
            BattleRoyaleStartMenu attached =
                physicalMenu.GetComponent<BattleRoyaleStartMenu>();
            if (attached != null)
                return attached;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                BattleRoyaleStartMenu[] candidates =
                    roots[i].GetComponentsInChildren<BattleRoyaleStartMenu>(true);

                for (int j = 0; j < candidates.Length; j++)
                {
                    BattleRoyaleStartMenu candidate = candidates[j];
                    if (candidate != null)
                        return candidate;
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T[] components = roots[i].GetComponentsInChildren<T>(true);
                if (components.Length > 0)
                    return components[0];
            }
            return null;
        }

        private static Button FindNamedButton(Transform root, string name)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == name)
                    return buttons[i];
            }
            return null;
        }

        private static bool SetObjectReference(
            SerializedObject serialized,
            string propertyName,
            Object value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            return true;
        }

        private static bool SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
                return false;

            property.boolValue = value;
            return true;
        }

        private static bool SetFloat(
            SerializedObject serialized,
            string propertyName,
            float value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || Mathf.Approximately(property.floatValue, value))
                return false;

            property.floatValue = value;
            return true;
        }

        private static bool SetVector3(
            SerializedObject serialized,
            string propertyName,
            Vector3 value
        )
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.vector3Value == value)
                return false;

            property.vector3Value = value;
            return true;
        }

        private static bool EnsureEventSystem(Scene scene)
        {
            EventSystem existing = FindInScene<EventSystem>(scene);
            GameObject eventObject;
            bool changed = false;

            if (existing == null)
            {
                eventObject = new GameObject("EventSystem_EditorFirst");
                SceneManager.MoveGameObjectToScene(eventObject, scene);
                existing = eventObject.AddComponent<EventSystem>();
                changed = true;
            }
            else
            {
                eventObject = existing.gameObject;
            }

            InputSystemUIInputModule inputModule =
                eventObject.GetComponent<InputSystemUIInputModule>();

            if (inputModule == null)
            {
                BaseInputModule[] oldModules =
                    eventObject.GetComponents<BaseInputModule>();

                for (int i = 0; i < oldModules.Length; i++)
                {
                    if (oldModules[i] != null &&
                        !(oldModules[i] is InputSystemUIInputModule))
                    {
                        Object.DestroyImmediate(oldModules[i]);
                    }
                }

                inputModule = eventObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
                changed = true;
            }

            EditorUtility.SetDirty(eventObject);
            return changed;
        }
    }
}
