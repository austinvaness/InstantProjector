using ProtoBuf;
using Sandbox.ModAPI;

namespace avaness.GridSpawner.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class PacketBuild : Packet
    {
        public override byte TypeId { get; } = 0;

        [ProtoMember(1)]
        public long entityId;
        [ProtoMember(2)]
        public byte trustSender;
        
        public PacketBuild()
        {

        }

        public PacketBuild (IMyTerminalBlock projector, bool trustSender)
        {
            entityId = projector.EntityId;
            if (trustSender)
                this.trustSender = 1;
            else
                this.trustSender = 0;
        }


        public override void Serialize (byte [] data, ulong sender)
        {
            MyAPIGateway.Utilities.SerializeFromBinary<PacketBuild>(data).Received(sender);
        }

        public override void Received (ulong sender)
        {
            IMyProjector p = MyAPIGateway.Entities.GetEntityById(entityId) as IMyProjector;
            if (p != null)
            {
                InstantProjector gl = p.GameLogic.GetAs<InstantProjector>();
                if (gl != null)
                    gl.BuildServer(sender, trustSender == 1);
            }
        }

        public override byte [] ToBinary ()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class PacketCmd : Packet
    {
        public override byte TypeId => 0;

        [ProtoMember(1)]
        public string cmd;

        public PacketCmd()
        {

        }

        public PacketCmd(string cmd)
        {
            this.cmd = cmd;
        }

        public override void Received(ulong sender)
        {

        }

        public override void Serialize(byte[] data, ulong sender)
        {
            MyAPIGateway.Utilities.SerializeFromBinary<PacketCmd>(data).Received(sender);
        }

        public override byte[] ToBinary()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }
}
