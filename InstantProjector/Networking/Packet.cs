﻿using Sandbox.ModAPI;

namespace avaness.GridSpawner.Networking
{
    public abstract class Packet
    {
        // 0 = PacketBuild
        // 1 = SyncableProjectorState
        // 2 = MapSettings
        // 3 = MapSettings.ValuePacket
        // 4 = PacketSettingsRequest
        // 5 = SyncableProjectorSettings
        public abstract byte TypeId { get; }

        public abstract byte [] ToBinary ();
        public abstract void Serialize (byte [] data, ulong sender);
        public abstract void Received (ulong sender);

        public void SendToServer()
        {
            if (Constants.IsServer)
            {
                if (MyAPIGateway.Session.Player != null)
                    Received(MyAPIGateway.Session.Player.SteamUserId);
                else
                    Received(0);
            }
            else
            {
                IPSession.Instance.Net.SendToServer(ToBinary(), TypeId);
            }
        }

        public void SendToOthers()
        {
            IPSession.Instance.Net.SendToOthers(ToBinary(), TypeId);
        }

        public void SendTo(ulong id)
        {
            IPSession.Instance.Net.SendTo(ToBinary(), TypeId, id);
        }

        public void SendToNot(ulong id)
        {
            IPSession.Instance.Net.SendToNot(ToBinary(), TypeId, id);
        }
    }
}
