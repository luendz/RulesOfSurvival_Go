using ROS.Game.Input;
using ROS.Game.Interaction;
using ROS.Game.UI;
using UnityEngine;

namespace ROS.Game.Loot
{
    public static class LootRuntimeSetup
    {
        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad
        )]
        private static void Initialize()
        {
            PlayerInputReader[] players =
                Object.FindObjectsByType<PlayerInputReader>(
                    FindObjectsSortMode.None
                );

            foreach (PlayerInputReader input in players)
            {
                GameObject player = input.gameObject;

                if (player.GetComponent<PlayerInteractor>() == null)
                {
                    player.AddComponent<PlayerInteractor>();
                }

                if (player.GetComponent<PlayerLootEquipment>() == null)
                {
                    player.AddComponent<PlayerLootEquipment>();
                }

                if (player.GetComponent<LootDropController>() == null)
                {
                    player.AddComponent<LootDropController>();
                }

                if (player.GetComponent<NearbyLootPresenter>() == null)
                {
                    player.AddComponent<NearbyLootPresenter>();
                }
            }
        }
    }
}
