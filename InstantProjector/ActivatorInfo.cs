using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace avaness.GridSpawner
{
    public class ActivatorInfo
    {
        private readonly bool empty;
        private readonly long playerId;
        private readonly HashSet<long> whitelist = new HashSet<long>();

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

        public void Whitelist(IMyCubeGrid grid)
        {
            whitelist.Add(grid.EntityId);
        }

        public bool IsEnemyGrid(IMyCubeGrid grid)
        {
            return false;
        }
    }
}
