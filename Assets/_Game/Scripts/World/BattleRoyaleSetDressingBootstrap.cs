using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.World
{
    public static class BattleRoyaleSetDressingBootstrap
    {
        public static readonly Vector3 SedanPosition =
            new Vector3(24f, 0.04f, -18f);
        public static readonly Vector3 SedanEulerAngles =
            new Vector3(0f, -32f, 0f);

        private const string BattleRoyaleScene = "07_BattleRoyaleTest";
        private const string SedanResource = "World/PF_SedanStatic";
        private const string SedanObjectName = "Sedan_Static_Decoration";

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            if (SceneManager.GetActiveScene().name != BattleRoyaleScene ||
                GameObject.Find(SedanObjectName) != null)
            {
                return;
            }

            GameObject sedanPrefab = Resources.Load<GameObject>(
                SedanResource
            );
            if (sedanPrefab == null)
            {
                Debug.LogWarning(
                    "No se encontró el Sedan decorativo. Ejecuta " +
                    "ROS Battle Royale/Build Static Sedan."
                );
                return;
            }

            GameObject sedan = Object.Instantiate(
                sedanPrefab,
                SedanPosition,
                Quaternion.Euler(SedanEulerAngles)
            );
            sedan.name = SedanObjectName;
            SetStaticRecursively(sedan.transform);

            Collider[] colliders = sedan.GetComponentsInChildren<Collider>(
                true
            );
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void SetStaticRecursively(Transform root)
        {
            root.gameObject.isStatic = true;
            for (int i = 0; i < root.childCount; i++)
            {
                SetStaticRecursively(root.GetChild(i));
            }
        }
    }
}
