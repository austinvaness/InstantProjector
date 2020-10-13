using ProtoBuf;
using Sandbox.ModAPI;
using System;
using VRage.ModAPI;

namespace avaness.GridSpawner.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class SyncableProjectorState : Syncable
    {
        public override byte TypeId { get; } = 1;

        [ProtoMember(1)]
        private InstantProjector.State _state;
        public InstantProjector.State BuildState
        {
            get
            {
                return _state;
            }
            set
            {
                VerifySettable();
                _state = value;
                SendToOthers();
            }
        }

        [ProtoMember(2)]
        public int Timer;


        public SyncableProjectorState () : base()
        { }

        public SyncableProjectorState (IMyEntity e, InstantProjector.State state, int timeout) : base(e.EntityId)
        {
            _state = state;
            Timer = timeout;
        }

        public override byte [] ToBinary ()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }

        public override void Serialize (byte [] data, ulong sender)
        {
            MyAPIGateway.Utilities.SerializeFromBinary<SyncableProjectorState>(data).Received(sender);
        }

        protected override bool IsType (Syncable s)
        {
            return s is SyncableProjectorState;
        }

        protected override void CopyValueTo (Syncable s)
        {
            SyncableProjectorState ps = (SyncableProjectorState)s;
            ps._state = _state;
            ps.Timer = Timer;
        }

        public override string ToString ()
        {
            return "{ State:" + _state + ", Timeout:" + Timer + " }";
        }
    }
}