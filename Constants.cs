﻿using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;

namespace GridSpawner
{
    public static class Constants
    {
        public static bool IsServer => MyAPIGateway.Session.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;
        public static bool IsDedicated => IsServer && MyAPIGateway.Utilities.IsDedicated;
        public static bool IsPlayer => !IsDedicated;
        public static bool IsClient = !IsServer;

        public const string msgNoSpace = "There is not enough room to spawn that.";
        public const string msgMissingComp = " components are needed to build that.";
        public const string msgNoGrid = "No projector grid.";
        public const double timeoutMultiplier = 0.5;
        public static readonly Random rand = new Random();

        public static IMyPlayer GetPlayer(ulong id)
        {
            List<IMyPlayer> temp = new List<IMyPlayer>(1);
            MyAPIGateway.Players.GetPlayers(temp, (p) => p.SteamUserId == id);
            return temp.FirstOrDefault();
        }

        public static void Notify(string msg, ulong id)
        {
            long id2 = MyAPIGateway.Players.TryGetIdentityId(id);
            if(id2 != 0)
                MyVisualScriptLogicProvider.ShowNotification(msg, 2000, "White", id2);
        }

        public static long RandomLong ()
        {
            byte [] bytes = new byte [8];
            rand.NextBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static bool RandomEntityId (out long id)
        {
            for (int i = 0; i < 100; i++)
            {
                id = RandomLong();
                if (!MyAPIGateway.Entities.EntityExists(id))
                    return true;
            }
            id = 0;
            return false;
        }
    }
}
