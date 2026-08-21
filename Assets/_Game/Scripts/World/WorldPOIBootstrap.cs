using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROS.Game.World
{
    /// <summary>
    /// Coloca el primer POI jugable (casa pequeña + casa grande) en la escena
    /// de Battle Royale usando greybox de primitivas Unity.
    /// </summary>
    public static class WorldPOIBootstrap
    {
        private const string BattleRoyaleScene = "07_BattleRoyaleTest";
        private const string SmallHouseName    = "POI_SmallHouse_01";
        private const string LargeHouseName    = "POI_LargeHouse_01";

        // Posiciones en el mapa (ajustar según escena)
        private static readonly Vector3 SmallHousePos = new Vector3( 15f, 0f, -12f);
        private static readonly Vector3 LargeHousePos = new Vector3(-22f, 0f,  18f);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            if (SceneManager.GetActiveScene().name != BattleRoyaleScene)
                return;

            // Evitar duplicados si el método se invoca más de una vez
            if (GameObject.Find(SmallHouseName) != null)
                return;

            GameObject small = GreyboxBuilding.CreateSmall(SmallHousePos, yRotation: 0f);
            small.name = SmallHouseName;

            GameObject large = GreyboxBuilding.CreateLarge(LargeHousePos, yRotation: -45f);
            large.name = LargeHouseName;
        }
    }
}
