using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace avaness.GridSpawner.Networking
{
    public class Network
    {
        private const ushort mainPacketId = 34920;
        private readonly Packet [] factories = new Packet[256];

        public Network()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(mainPacketId, ReceivePacket);
        }

        /// <summary>
        /// Adds a factory that will be used to serialize incoming packets.
        /// </summary>
        /// <param name="factory">An empty Packet object that will be used to receive data.</param>
        public void AddFactory(Packet factory)
        {
            int id = factory.TypeId;
            if (factories [id] == null)
                factories [id] = factory;
        }


        /// <summary>
        /// Serializes incoming data into PacketData then redirects to the appropriate factory.
        /// </summary>
        private void ReceivePacket(ushort id, byte[] data, ulong sender, bool fromServer)
        {
            PacketData p = MyAPIGateway.Utilities.SerializeFromBinary<PacketData>(data);
            if (p != null)
            {
                Packet factory = factories [p.id];
                if (factory != null)
                    factory.Serialize(p.bytes, p.sender);
                else
                    MyLog.Default.WriteLineAndConsole("[Instant Projector] WARNING: No factory for packet with id " + p.id + "!");
            }
        }

        public void Unload()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(mainPacketId, ReceivePacket);
        }

        public void SendToServer (byte[] data, byte typeId)
        {
            if (Constants.IsClient)
                MyAPIGateway.Multiplayer.SendMessageToServer(mainPacketId, PacketData.ToBinary(data, typeId));
        }

        public void SendToOthers (byte[] data, byte typeId)
        {
            data = PacketData.ToBinary(data, typeId);

            ulong me = 0;
            if (MyAPIGateway.Session?.Player != null)
                me = MyAPIGateway.Session.Player.SteamUserId;
            List<IMyPlayer> temp = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(temp, (pl) => pl.SteamUserId != me);
            foreach (IMyPlayer pl in temp)
                MyAPIGateway.Multiplayer.SendMessageTo(mainPacketId, data, pl.SteamUserId);
        }

        public void SendTo (byte [] data, byte typeId, ulong id)
        {
            if (id != 0)
                MyAPIGateway.Multiplayer.SendMessageTo(mainPacketId, PacketData.ToBinary(data, typeId), id);
        }

        public void SendToNot (byte[] data, byte typeId, ulong id)
        {
            data = PacketData.ToBinary(data, typeId);

            ulong me = 0;
            if (MyAPIGateway.Session?.Player != null)
                me = MyAPIGateway.Session.Player.SteamUserId;
            List<IMyPlayer> temp = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(temp, (pl) => pl.SteamUserId != id && pl.SteamUserId != me);
            foreach (IMyPlayer pl in temp)
                MyAPIGateway.Multiplayer.SendMessageTo(mainPacketId, data, pl.SteamUserId);
        }

        [ProtoContract]
        private class PacketData
        {
            [ProtoMember(1)]
            public byte id;
            [ProtoMember(2)]
            public ulong sender;
            [ProtoMember(3)]
            public byte [] bytes;

            public PacketData()
            {
            }

            private PacketData(byte[] data, byte id)
            {
                this.id = id;
                bytes = data;
                if (Constants.IsDedicated)
                    sender = 0;
                else if(MyAPIGateway.Session?.Player != null)
                    sender = MyAPIGateway.Session.Player.SteamUserId;
                else
                    MyLog.Default.WriteLineAndConsole($"[Instant Projector] WARNING: Creating packet {id} with invalid sender address");
            }

            public static byte[] ToBinary(byte[] data, byte id)
            {
                PacketData p = new PacketData(data, id);
                return MyAPIGateway.Utilities.SerializeToBinary(p);
            }

            public static byte[] ToBinary(Packet p)
            {
                PacketData data = new PacketData(p.ToBinary(), p.TypeId);
                return MyAPIGateway.Utilities.SerializeToBinary(data);
            }
        }
    }
}
