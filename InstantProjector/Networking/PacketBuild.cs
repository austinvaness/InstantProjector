using avaness.GridSpawner.Grids;
using ProtoBuf;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;

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
        [ProtoMember(3)]
        public GridPositionInfo positionFix;

        public PacketBuild()
        {

        }

        public PacketBuild (IMyTerminalBlock projector, bool trustSender, bool cancel, List<MyObjectBuilder_CubeGrid> grids = null)
        {
            entityId = projector.EntityId;
            data = 0;
            if (trustSender)
                data |= 1;
            if (cancel)
                data |= 2;
            if(grids != null && trustSender)
                positionFix = new GridPositionInfo(grids);
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
                        gl.BuildServer(sender, trustSender, positionFix);
                }
            }
        }

        public override byte [] ToBinary ()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }
    }
}
