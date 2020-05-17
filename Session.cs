using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRageMath;

namespace GridSpawner
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class InstantProjectorSession : MySessionComponentBase
    {
        public static InstantProjectorSession Instance;

        public readonly Dictionary<long, Syncable> syncValues = new Dictionary<long, Syncable>();
        public readonly Dictionary<long, float> cooldowns = new Dictionary<long, float>();

        public InstantProjectorSession()
        {
            Instance = this;
        }

        private bool init = false;
        private void Start ()
        {
            Packet.RegisterReceive();
            init = true;
        }

        protected override void UnloadData ()
        {
            InstantProjector.controls = false;
            Packet.Unload();
            foreach (Syncable s in syncValues.Values.ToArray())
                s.Dispose();
            Instance = null;
        }

        public override void UpdateAfterSimulation ()
        {
            if(!init)
                Start();
            MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate));
        }
    }
}
