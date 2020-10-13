using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;
using VRage.Utils;

namespace avaness.GridSpawner.Networking
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class SyncableProjectorSettings : Syncable
    {
        public override byte TypeId => 5;

        [ProtoMember(1)]
        private float _speed;
        public float Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                if (value != _speed)
                {
                    _speed = value;
                    if (Constants.IsServer)
                        SendToOthers();
                    else
                        SendToServer();
                }
            }
        }

        [ProtoMember(2)]
        private bool _looseArea;
        public bool LooseArea
        {
            get
            {
                return _looseArea;
            }
            set
            {
                if(value != _looseArea)
                {
                    _looseArea = value;
                    if (Constants.IsServer)
                        SendToOthers();
                    else
                        SendToServer();
                }
            }
        }

        public SyncableProjectorSettings() : base()
        { }

        public SyncableProjectorSettings(IMyEntity e, float speed, bool looseArea) : base(e.EntityId)
        {
            _speed = speed;
            _looseArea = looseArea;
        }

        public override void Serialize(byte[] data, ulong sender)
        {
            MyAPIGateway.Utilities.SerializeFromBinary<SyncableProjectorSettings>(data).Received(sender);
        }

        public override byte[] ToBinary()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);

        }

        protected override void CopyValueTo(Syncable val)
        {
            SyncableProjectorSettings s = (SyncableProjectorSettings)val;
            s._speed = _speed;
            s._looseArea = _looseArea;
        }

        protected override bool IsType(Syncable s)
        {
            return s is SyncableProjectorSettings;
        }

        protected override void ReceivedFromClient(Syncable current, ulong sender)
        {
            if (_speed <= 0)
            {
                current.SendTo(sender);
            }
            else
            {
                CopyToInvoke(current);
                SendToOthers();
            }
        }

        public override void RequestFromServer()
        {
            _speed = 0;
            base.RequestFromServer();
        }

        public override string ToString()
        {
            return $"Speed: {_speed} Loose Area: {_looseArea}";
        }

        public void Verify()
        {
            if (_speed < Constants.minSpeed)
                _speed = Constants.minSpeed;
            else if (_speed > Constants.maxSpeed)
                _speed = Constants.maxSpeed;
        }
    }
}
