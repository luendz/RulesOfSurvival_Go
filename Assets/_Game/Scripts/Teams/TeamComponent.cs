using UnityEngine;

namespace ROS.Game.Teams
{
    public sealed class TeamComponent : MonoBehaviour
    {
        [SerializeField] private int teamId = -1;
        [SerializeField] private int squadSlot;
        public int TeamId => teamId;
        public int SquadSlot => squadSlot;
        public void Assign(int id, int slot) { teamId = id; squadSlot = slot; }
        public bool IsTeammate(TeamComponent other) => other != null && teamId >= 0 && other.teamId == teamId;
    }
}
