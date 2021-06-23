using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace avaness.GridSpawner
{
    public class ActivatorInfo
    {
        private readonly bool empty, isBot;
        private readonly long playerId;

        public ActivatorInfo()
        {
            empty = true;
        }

        public ActivatorInfo(long playerId)
        {
            this.playerId = playerId;

            if (playerId == 0)
            {
                empty = true;
            }
            else
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, (p) => p.IdentityId == playerId);
                if (players.Count > 0 && players[0].IsBot)
                    isBot = true;
            }
        }

        public bool IsEnemyGrid(IMyCubeGrid grid)
        {
            if (isBot)
                return false;

            if (empty)
                return true;

            if (grid.BigOwners == null || grid.BigOwners.Count == 0)
                return false;

            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            if (faction == null)
                return !grid.BigOwners.Contains(playerId);

            return grid.BigOwners.Exists((p) => faction.IsEnemy(p));
        }
    }
}
