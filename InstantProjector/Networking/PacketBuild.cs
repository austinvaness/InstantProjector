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
        public byte data;
        
        public PacketBuild()
        {

        }

        public PacketBuild (IMyTerminalBlock projector, bool trustSender, bool cancel)
        {
            entityId = projector.EntityId;
            data = 0;
            if (trustSender)
                data |= 1;
            if (cancel)
                data |= 2;
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
                {
                    bool trustSender = (data & 1) == 1;
                    bool cancel = (data & 2) == 2;
                    if (cancel)
                        gl.CancelServer(sender, trustSender);
                    else
                        gl.BuildServer(sender, trustSender);
                }
            }
        }

        public override byte [] ToBinary ()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }
}
