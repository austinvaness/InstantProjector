using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace avaness.GridSpawner
{
    public class ActivatorInfo
    {
        private readonly bool empty;
        private readonly long playerId;

        public ActivatorInfo()
        {
            empty = true;
        }

        public ActivatorInfo(long playerId)
        {
            this.playerId = playerId;
            if (playerId == 0)
                empty = true;
        }

        public bool IsEnemyGrid(IMyCubeGrid grid)
        {
            if (empty)
                return true; // Not enough information so assume the worst

            if (grid.BigOwners == null || grid.BigOwners.Count == 0)
                return false; // Grid is unowned

            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            if (faction == null)
                return !grid.BigOwners.Contains(playerId); // Player has no faction so the grid must be owned by the exact player

            return grid.BigOwners.Exists((p) => faction.IsEnemy(p)); // Check if owners of grid are enemies
        }
    }
}
