using ProtoBuf;
using System;
using VRage.Utils;

namespace avaness.GridSpawner.Networking
{
    [ProtoInclude(1001, typeof(SyncableProjectorState))]
    [ProtoInclude(1002, typeof(SyncableProjectorSettings))]
    [ProtoContract(UseProtoMembersOnly = true)]
    public abstract class Syncable : Packet
    {
        public event Action OnValueReceived;

        [ProtoMember(1)]
        private long key;

        public Syncable ()
        { }

        public Syncable (long block)
        {
            SetKey(block);
        }

        public void SetKey(long block)
        {
            key = block + TypeId;
            IPSession.Instance.Syncable[key] = this;
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

        public virtual void RequestFromServer()
        {
            //MyLog.Default.WriteLineAndConsole("Requested syncable from server with key " + key);
            if(Constants.IsClient)
                SendToServer();
        }

        public override void Received (ulong sender)
        {
            Syncable s;
            bool tryGet = IPSession.Instance.Syncable.TryGetValue(key, out s);
            if (tryGet && IsType(s))
            {
                if (Constants.IsServer)
                    ReceivedFromClient(s, sender);
                else
                    ReceivedFromServer(s, sender);
            }
            else
            {
                //if (!tryGet)
                    //MyLog.Default.WriteLineAndConsole("Unable to find syncable value " + key);
                //else
                    //MyLog.Default.WriteLineAndConsole("Syncable types don't match. " + GetType() + " != " + s.GetType());
            }
        }

        protected virtual void ReceivedFromClient(Syncable current, ulong sender)
        {
            // Client has requested a value
            current.SendTo(sender);
        }

        protected virtual void ReceivedFromServer(Syncable current, ulong sender)
        {
            // A value has been received from the server
            //if(current is SyncableProjectorSettings)
                //MyLog.Default.WriteLine("Received from server: " + ToString());
            CopyToInvoke(current);
        }

        protected void CopyToInvoke(Syncable current)
        {
            CopyValueTo(current);
            if (current.OnValueReceived != null)
                current.OnValueReceived.Invoke();
        }

        protected abstract bool IsType (Syncable s);

        protected abstract void CopyValueTo (Syncable val);

    }
}