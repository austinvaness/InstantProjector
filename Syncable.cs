using ProtoBuf;
using System;
using VRage;
using VRage.GameServices;
using VRage.ModAPI;

namespace GridSpawner
{
    public abstract class Syncable : IDisposable
    {
        public enum Context : byte
        {
            All, Server, Client
        }

        public Context CanModify = Context.Server;

        public byte Id { get; }

        protected readonly long key;

        public Syncable(byte id, long block)
        {
            Id = id;
            key = id + block;
            InstantProjectorSession.Instance.syncValues [key] = this;
        }

        ~Syncable()
        {
            Dispose();
        }

        protected bool CanSetValue ()
        {
            if (CanModify == Context.All)
                return true;
            if (CanModify == Context.Server)
                return Constants.IsServer;
            return Constants.IsClient;
        }

        public void Dispose ()
        {
            InstantProjectorSession.Instance?.syncValues.Remove(key);
            OnValueReceived = null;
        }

        public abstract void Set (object value);

        public event Action<byte> OnValueReceived;

        public void InvokeReceived()
        {
            if (OnValueReceived != null)
                OnValueReceived.Invoke(Id);
        }
    }

    public class Sync<T> : Syncable
    {
        private T val;
        public T Value
        {
            get
            {
                return val;
            }
            set
            {
                if (!CanSetValue())
                    throw new InvalidOperationException("Syncable value " + key + " cannot be modified in the current context.");
                val = value;
                Send();
            }
        }

        public Sync (byte id, IMyEntity e) : base(id, e.EntityId)
        {

        }

        public Sync (byte id, IMyEntity e, T value) : base(id, e.EntityId)
        {
            val = value;
        }

        private void Send ()
        {
            SyncPacket p = new SyncPacket(key, val);
            if (Constants.IsServer)
                p.SendToOthers();
            else
                p.SendToServer();
        }

        public override void Set (object value)
        {
            if (value is T)
                val = (T)value;
        }


        [ProtoContract]
        public class SyncPacket : Packet
        {
            [ProtoMember(1)]
            public long key;
            [ProtoMember(2)]
            public T value;

            public SyncPacket ()
            {

            }

            public SyncPacket (long key, T value)
            {
                this.key = key;
                this.value = value;
            }

            public override void Received ()
            {
                if (Constants.IsServer)
                    SendToNot(Sender);

                Syncable syncable;
                if (InstantProjectorSession.Instance.syncValues.TryGetValue(key, out syncable))
                {
                    syncable.Set(value);
                    syncable.InvokeReceived();
                }
            }
        }
    }
}
