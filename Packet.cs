using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace GridSpawner
{
    [ProtoInclude(1001, typeof(Sync<>.SyncPacket))]
    [ProtoInclude(1000, typeof(PacketBuild))]
    [ProtoContract]
    public abstract class Packet
    {
        private const ushort mainPacketId = 34920;
        [ProtoMember(1)]
        public ulong Sender;

        public Packet()
        {
            if (Constants.IsDedicated)
                Sender = 0;
            else
                Sender = MyAPIGateway.Session.Player.SteamUserId;
        }

        public static void RegisterReceive()
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(mainPacketId, ReceivePacket);
        }

        public static void ReceivePacket(byte[] data)
        {
            Packet p = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(data);
            if (p != null)
                p.Received();
        }

        public static void Unload()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(mainPacketId, ReceivePacket);
        }

        private byte[] ToBinary()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }

        public void SendToServer()
        {
            if (Constants.IsServer)
                Received();
            else
                MyAPIGateway.Multiplayer.SendMessageToServer(mainPacketId, ToBinary());
        }

        public void SendToOthers()
        {
            MyAPIGateway.Multiplayer.SendMessageToOthers(mainPacketId, ToBinary());
        }

        public void SendTo(ulong id)
        {
            MyAPIGateway.Multiplayer.SendMessageTo(mainPacketId, ToBinary(), id);
        }

        public void SendToNot(ulong id)
        {
            ulong me = 0;
            if (MyAPIGateway.Session.Player != null)
                me = MyAPIGateway.Session.Player.SteamUserId;
            List<IMyPlayer> temp = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(temp, (p) => p.SteamUserId != id && p.SteamUserId != me);
            byte [] data = ToBinary();
            foreach (IMyPlayer p in temp)
                MyAPIGateway.Multiplayer.SendMessageTo(mainPacketId, data, p.SteamUserId);
        }

        public abstract void Received ();
    }
}
