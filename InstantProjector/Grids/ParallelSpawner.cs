using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace avaness.GridSpawner.Grids
{
    public class ParallelSpawner
    {
        private readonly int gridCount;
        private readonly List<MyObjectBuilder_CubeGrid> grids;
        private Action<HashSet<IMyCubeGrid>> onSuccess;
        private readonly HashSet<IMyCubeGrid> spawned;

        public ParallelSpawner(List<MyObjectBuilder_CubeGrid> grids, Action<HashSet<IMyCubeGrid>> onReady)
        {
            this.grids = grids;
            gridCount = grids.Count;
            onSuccess = onReady;
            spawned = new HashSet<IMyCubeGrid>();
        }

        public bool Start()
        {
            foreach (var o in grids)
            {
                if (MyAPIGateway.Entities.CreateFromObjectBuilderParallel(o, false, Increment) == null)
                    return false;
            }
            return true;
        }

        public void Increment(IMyEntity entity)
        {
            var grid = (IMyCubeGrid)entity;
            spawned.Add(grid);

            if (spawned.Count < gridCount)
                return;

            onSuccess.Invoke(spawned);
        }

        public static bool Add(IEnumerable<IMyCubeGrid> grids)
        {
            foreach (IMyCubeGrid grid in grids)
            {
                MyAPIGateway.Entities.AddEntity(grid);
                if (!grid.InScene || !MyAPIGateway.Entities.Exist(grid))
                    return false;
            }
            return true;
        }

        public static void Close(IEnumerable<IMyCubeGrid> grids)
        {
            foreach (IMyCubeGrid grid in grids)
            {
                MyAPIGateway.Entities.MarkForClose(grid);
            }
        }
    }
}
