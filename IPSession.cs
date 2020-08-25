using avaness.GridSpawner.Networking;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace avaness.GridSpawner
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class IPSession : MySessionComponentBase
    {
        public static IPSession Instance;

        public int Runtime; // Server only
        public Network Net { get; private set; }
        public Dictionary<long, Syncable> Syncable = new Dictionary<long, Syncable>();

        // Key: original grid id, Value: remaining ticks
        private readonly Dictionary<long, int> cooldowns = new Dictionary<long, int>();

        public IPSession()
        {
            Instance = this;
        }

        public override void BeforeStart ()
        {
            Instance = this;
            Net = new Network();
        }

        private bool init = false;
        private void Start ()
        {
            if (Constants.IsServer)
                Net.AddFactory(new PacketBuild());
            Net.AddFactory(new SyncableProjectorState());
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
            MyLog.Default.WriteLineAndConsole("Instant Projector initialized.");
            init = true;
        }

        protected override void UnloadData ()
        {
            Net?.Unload();
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            foreach (Syncable s in Syncable.Values)
                s.Close();
            Instance = null;
        }

        public override void UpdateAfterSimulation ()
        {
            Runtime++;
            if (MyAPIGateway.Session == null)
                return;

            if(!init)
                Start();
            if(!Constants.IsServer)
                MyAPIGateway.Utilities.InvokeOnGameThread(() => SetUpdateOrder(MyUpdateOrder.NoUpdate));
        }

        private void OnEntityRemove (IMyEntity e)
        {
            IMyCubeGrid grid = e as IMyCubeGrid;
            int ticks;
            if (grid != null && cooldowns.TryGetValue(grid.EntityId, out ticks))
                SetGridTimeout(grid, ticks);
        }

        // Returns the new grid tick value
        public int SetGridTimeout(IMyCubeGrid cubeGrid, int ticks)
        {
            if(cubeGrid.MarkedForClose)
            {
                cooldowns.Remove(cubeGrid.EntityId);
                List<IMyCubeGrid> grids = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Logical);
                foreach(IMyCubeGrid grid in grids)
                {
                    if(!grid.MarkedForClose)
                        return UpdateIfGreater(grid.EntityId, ticks);
                }
                return ticks;
            }
            else
            {
                return UpdateIfGreater(cubeGrid.EntityId, ticks);
            }
        }

        // Returns the new grid tick value
        private int UpdateIfGreater(long entityId, int ticks)
        {
            int temp;
            if (cooldowns.TryGetValue(entityId, out temp) && ticks < temp - 1)
                ticks = temp;
            cooldowns [entityId] = ticks;
            return ticks;
        }

        public int GetGridTimeout (IMyCubeGrid cubeGrid)
        {
            List<IMyCubeGrid> grids = MyAPIGateway.GridGroups.GetGroup(cubeGrid, GridLinkTypeEnum.Logical);
            foreach(IMyCubeGrid grid in grids)
            {
                int ticks;
                if(cooldowns.TryGetValue(grid.EntityId, out ticks))
                {
                    cooldowns.Remove(grid.EntityId);
                    return ticks;
                }
            }
            return 0;
        }
    }
}
