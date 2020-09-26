using ProtoBuf;
using Sandbox.ModAPI;
using System;

namespace avaness.GridSpawner.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class PacketSettingsRequest : Packet
    {
        public override byte TypeId => 4;

        public override void Received(ulong sender)
        {
            IPSession.Instance.MapSettings.SendTo(sender);
        }

        public override void Serialize(byte[] data, ulong sender)
        {
            MyAPIGateway.Utilities.SerializeFromBinary<PacketSettingsRequest>(data).Received(sender);
        }

        public override byte[] ToBinary()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }
}
