using ProtoBuf;
using System;

namespace avaness.GridSpawner.Networking
{
    [ProtoInclude(1001, typeof(SyncableProjectorState))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract class Syncable : Packet
    {
        public event Action OnValueReceived;

        [ProtoMember(1)]
        private readonly long key;

        public Syncable ()
        { }

        public Syncable (byte id, long block)
        {
            key = block + id;
            IPSession.Instance.Syncable [key] = this;
        }

        protected void VerifySettable ()
        {
            if (Constants.IsClient)
                throw new InvalidOperationException("Syncable of type " + GetType().FullName + " with key " + key + " cannot be modified by a client.");
        }

        public void Close ()
        {
            IPSession.Instance?.Syncable.Remove(key);
            OnValueReceived = null;
        }

        public void RequestFromServer()
        {
            if(Constants.IsClient)
                SendToServer();
        }

        public override void Received (ulong sender)
        {
            Syncable s;
            if (IPSession.Instance.Syncable.TryGetValue(key, out s) && IsType(s))
            {
                if (Constants.IsServer)
                {
                    // Client has requested a value
                    s.SendTo(sender);
                }
                else
                {
                    // A value has been received from the server
                    CopyValueTo(s);
                    if (s.OnValueReceived != null)
                        s.OnValueReceived.Invoke();
                }
            }
        }

        protected abstract bool IsType (Syncable s);

        protected abstract void CopyValueTo (Syncable val);

    }
}