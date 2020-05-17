using ProtoBuf;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace GridSpawner
{
    [ProtoContract]
    public class PacketBuild : Packet
    {
        [ProtoMember(1)]
        public long entityId;
        
        public PacketBuild()
        {

        }

        public PacketBuild (IMyTerminalBlock projector)
        {
            entityId = projector.EntityId;
        }

        public override void Received ()
        {
            IMyProjector p = MyAPIGateway.Entities.GetEntityById(entityId) as IMyProjector;
            if(p != null)
            {
                InstantProjector gl = p.GameLogic.GetAs<InstantProjector>();
                if (gl != null)
                    gl.BuildServer(Sender);
            }
        }
    }
}
