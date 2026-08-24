using System.Collections;
using System.Collections.Generic;
using ROS.Game.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.World
{
    public sealed class EchoValleyDoor : MonoBehaviour
    {
        private static readonly List<EchoValleyDoor> ActiveDoorsInternal =
            new List<EchoValleyDoor>();

        public static IReadOnlyList<EchoValleyDoor> ActiveDoors =>
            ActiveDoorsInternal;

        [SerializeField] private float openAngle = 95f;
        [SerializeField] private float rotationSpeed = 240f;
        [SerializeField] private float hingeDirection = 1f;

        private Quaternion _closedLocalRotation;
        private Quaternion _targetLocalRotation;
        private EchoValleyDoor _linkedDoor;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _closedLocalRotation = transform.localRotation;
            _targetLocalRotation = _closedLocalRotation;
        }

        private void OnEnable()
        {
            if (!ActiveDoorsInternal.Contains(this))
            {
                ActiveDoorsInternal.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveDoorsInternal.Remove(this);
        }

        private void Update()
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                _targetLocalRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        public void Configure(
            float angle,
            float speed,
            float direction
        )
        {
            openAngle = Mathf.Clamp(angle, 45f, 130f);
            rotationSpeed = Mathf.Max(30f, speed);
            hingeDirection = Mathf.Sign(direction);
            if (Mathf.Approximately(hingeDirection, 0f))
            {
                hingeDirection = 1f;
            }

            _closedLocalRotation = transform.localRotation;
            _targetLocalRotation = _closedLocalRotation;
        }

        public void Link(EchoValleyDoor other)
        {
            _linkedDoor = other;
        }

        public void Toggle(Vector3 actorPosition)
        {
            bool nextState = !_isOpen;
            SetOpen(nextState, actorPosition, true);
        }

        private void SetOpen(
            bool open,
            Vector3 actorPosition,
            bool propagate
        )
        {
            _isOpen = open;

            if (open)
            {
                Vector3 toActor = actorPosition - transform.position;
                float actorSide = Vector3.Dot(transform.forward, toActor);
                float swingSide = actorSide >= 0f ? -1f : 1f;
                float yaw = openAngle * hingeDirection * swingSide;

                _targetLocalRotation =
                    _closedLocalRotation * Quaternion.Euler(0f, yaw, 0f);
            }
            else
            {
                _targetLocalRotation = _closedLocalRotation;
            }

            if (propagate && _linkedDoor != null)
            {
                _linkedDoor.SetOpen(open, actorPosition, false);
            }
        }
    }

    public sealed class EchoValleyDoorRuntime : MonoBehaviour
    {
        private const string SceneName = "08_EchoValley";
        private const float InteractionDistance = 3f;
        private const string GeneratedRootName = "EchoValley_Generated";

        private PlayerInputReader _input;
        private Transform _playerRoot;
        private Material _woodDoorMaterial;
        private Material _metalDoorMaterial;
        private bool _installed;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void InitializeForScene()
        {
            if (SceneManager.GetActiveScene().name != SceneName ||
                GameObject.Find("EchoValley_DoorRuntime") != null)
            {
                return;
            }

            GameObject runtime = new GameObject("EchoValley_DoorRuntime");
            runtime.AddComponent<EchoValleyDoorRuntime>();
        }

        private IEnumerator Start()
        {
            while (!_installed)
            {
                Transform generatedRoot = FindGeneratedRoot();
                if (generatedRoot != null)
                {
                    InstallDoors(generatedRoot);
                    _installed = true;
                    yield break;
                }

                yield return null;
            }
        }

        private void Update()
        {
            ResolvePlayer();

            if (_input == null ||
                _playerRoot == null ||
                !_input.InteractPressed)
            {
                return;
            }

            EchoValleyDoor nearestDoor = FindNearestDoor(
                _playerRoot.position
            );

            if (nearestDoor != null)
            {
                nearestDoor.Toggle(_playerRoot.position);
            }
        }

        private void ResolvePlayer()
        {
            if (_input != null && _playerRoot != null)
            {
                return;
            }

            _input = FindFirstObjectByType<PlayerInputReader>();
            if (_input != null)
            {
                _playerRoot = _input.transform.root;
            }
        }

        private static EchoValleyDoor FindNearestDoor(Vector3 position)
        {
            EchoValleyDoor nearest = null;
            float nearestSqrDistance =
                InteractionDistance * InteractionDistance;

            IReadOnlyList<EchoValleyDoor> doors =
                EchoValleyDoor.ActiveDoors;

            for (int i = 0; i < doors.Count; i++)
            {
                EchoValleyDoor door = doors[i];
                if (door == null || !door.isActiveAndEnabled)
                {
                    continue;
                }

                float sqrDistance =
                    (door.transform.position - position).sqrMagnitude;

                if (sqrDistance <= nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = door;
                }
            }

            return nearest;
        }

        private static Transform FindGeneratedRoot()
        {
            EchoValleyMapAuthoring authoring =
                FindFirstObjectByType<EchoValleyMapAuthoring>();

            if (authoring == null)
            {
                return null;
            }

            return authoring.transform.Find(GeneratedRootName);
        }

        private void InstallDoors(Transform generatedRoot)
        {
            if (generatedRoot.Find("DoorSystem_Installed") != null)
            {
                return;
            }

            GameObject marker = new GameObject("DoorSystem_Installed");
            marker.transform.SetParent(generatedRoot, false);

            _woodDoorMaterial = CreateDoorMaterial(
                "EV_Door_Wood",
                new Color(0.29f, 0.18f, 0.095f),
                0f,
                0.13f
            );

            _metalDoorMaterial = CreateDoorMaterial(
                "EV_Door_Metal",
                new Color(0.19f, 0.27f, 0.29f),
                0.28f,
                0.2f
            );

            InstallCoreDoors(generatedRoot);
            InstallWarehouseDoors(generatedRoot);
            InstallOuterHouseDoors(generatedRoot);
        }

        private void InstallCoreDoors(Transform root)
        {
            Transform main = FindDescendant(root, "Main_3F_Apartment");
            if (main != null)
            {
                CreateDoubleDoor(
                    main,
                    "MainEntrance",
                    new Vector3(0f, 0.24f, -8.16f),
                    3.5f,
                    2.45f,
                    _woodDoorMaterial,
                    100f
                );

                Transform oldRoofDoor = FindDirectChild(main, "Door");
                if (oldRoofDoor != null)
                {
                    oldRoofDoor.gameObject.SetActive(false);
                }

                const float roofY = 3.2f * 3f + 0.18f;
                CreateSingleDoor(
                    main,
                    "RoofAccess",
                    new Vector3(5.6f, roofY + 0.12f, 1.21f),
                    1.7f,
                    2.25f,
                    true,
                    _woodDoorMaterial,
                    95f
                );
            }

            Transform twoStory = FindDescendant(root, "Two_Story_House");
            if (twoStory != null)
            {
                CreateSingleDoor(
                    twoStory,
                    "TwoStoryEntrance",
                    new Vector3(-0.85f, 0.24f, -5.87f),
                    2.45f,
                    2.35f,
                    true,
                    _woodDoorMaterial,
                    95f
                );
            }
        }

        private void InstallWarehouseDoors(Transform root)
        {
            string[] warehouseNames =
            {
                "Warehouse_01_West",
                "Warehouse_02_CenterWest",
                "Warehouse_03_CenterEast",
                "Warehouse_04_East"
            };

            for (int i = 0; i < warehouseNames.Length; i++)
            {
                Transform warehouse = FindDescendant(
                    root,
                    warehouseNames[i]
                );

                if (warehouse == null)
                {
                    continue;
                }

                CreateDoubleDoor(
                    warehouse,
                    warehouseNames[i] + "_Gate",
                    new Vector3(0f, 0.28f, -6.13f),
                    5.65f,
                    3.35f,
                    _metalDoorMaterial,
                    105f
                );
            }
        }

        private void InstallOuterHouseDoors(Transform root)
        {
            for (int i = 1; i <= 8; i++)
            {
                string houseName = "Outer_House_" + i.ToString("00");
                Transform house = FindDescendant(root, houseName);
                if (house == null)
                {
                    continue;
                }

                CreateSingleDoor(
                    house,
                    houseName + "_Entrance",
                    new Vector3(0.2f, 0.24f, -4.37f),
                    2.9f,
                    2.3f,
                    i % 2 != 0,
                    _woodDoorMaterial,
                    95f
                );
            }
        }

        private EchoValleyDoor CreateSingleDoor(
            Transform parent,
            string name,
            Vector3 centerBottom,
            float width,
            float height,
            bool hingeLeft,
            Material material,
            float openAngle
        )
        {
            float hingeX = centerBottom.x +
                (hingeLeft ? -width * 0.5f : width * 0.5f);

            GameObject pivotObject = new GameObject(name + "_Pivot");
            Transform pivot = pivotObject.transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(
                hingeX,
                centerBottom.y,
                centerBottom.z
            );
            pivot.localRotation = Quaternion.identity;

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = name + "_Panel";
            panel.transform.SetParent(pivot, false);
            panel.transform.localPosition = new Vector3(
                hingeLeft ? width * 0.5f : -width * 0.5f,
                height * 0.5f,
                0f
            );
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = new Vector3(
                width,
                height,
                0.14f
            );

            Renderer renderer = panel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            AddDoorHandle(
                pivot,
                name,
                width,
                height,
                hingeLeft,
                material
            );

            EchoValleyDoor door = pivotObject.AddComponent<EchoValleyDoor>();
            door.Configure(
                openAngle,
                260f,
                hingeLeft ? 1f : -1f
            );
            return door;
        }

        private void CreateDoubleDoor(
            Transform parent,
            string name,
            Vector3 centerBottom,
            float fullWidth,
            float height,
            Material material,
            float openAngle
        )
        {
            float leafWidth = fullWidth * 0.5f;
            float quarter = fullWidth * 0.25f;

            EchoValleyDoor left = CreateSingleDoor(
                parent,
                name + "_L",
                centerBottom + Vector3.left * quarter,
                leafWidth,
                height,
                true,
                material,
                openAngle
            );

            EchoValleyDoor right = CreateSingleDoor(
                parent,
                name + "_R",
                centerBottom + Vector3.right * quarter,
                leafWidth,
                height,
                false,
                material,
                openAngle
            );

            left.Link(right);
            right.Link(left);
        }

        private static void AddDoorHandle(
            Transform pivot,
            string name,
            float width,
            float height,
            bool hingeLeft,
            Material material
        )
        {
            GameObject handle = GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );
            handle.name = name + "_Handle";
            handle.transform.SetParent(pivot, false);
            handle.transform.localPosition = new Vector3(
                hingeLeft ? width * 0.82f : -width * 0.82f,
                height * 0.52f,
                -0.12f
            );
            handle.transform.localScale = Vector3.one * 0.11f;

            Collider collider = handle.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = handle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material CreateDoorMaterial(
            string name,
            Color color,
            float metallic,
            float smoothness
        )
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            return material;
        }

        private static Transform FindDescendant(
            Transform root,
            string objectName
        )
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(
                true
            );

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == objectName)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static Transform FindDirectChild(
            Transform parent,
            string objectName
        )
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
