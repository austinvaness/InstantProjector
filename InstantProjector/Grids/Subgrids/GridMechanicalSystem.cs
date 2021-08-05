using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRageMath;

namespace avaness.GridSpawner.Grids.Subgrids
{
    public class GridMechanicalSystem
    {
        public bool Empty => groups.Count == 0;

        private readonly Dictionary<MechanicalConnectionType, MechanicalGroup> groups = new Dictionary<MechanicalConnectionType, MechanicalGroup>();
        private readonly MyObjectBuilder_CubeGrid grid;
        private bool hasBaseBlocks;
        private bool hasTopBlocks;

        public GridMechanicalSystem(MyObjectBuilder_CubeGrid grid)
        {
            this.grid = grid;
        }

        public void SearchBlocks(GridMechanicalSystem other)
        {
            // Search the other grid for top blocks

            //MyVisualScriptLogicProvider.AddGPS("Grid", $"{hasTopBlocks} {hasBaseBlocks} {groups.First().Key}", grid.PositionAndOrientation.Value.Position, Color.Red);

            if (!hasBaseBlocks || !other.hasTopBlocks)
                return;

            foreach(var otherGroup in other.groups)
            {
                if (otherGroup.Value.TopBlocks.Count == 0)
                    continue;

                MechanicalGroup myGroup;
                if (!groups.TryGetValue(otherGroup.Key, out myGroup))
                    continue;

                myGroup.SearchBlocks(otherGroup.Value);
            }
        }

        public void AttachBlocks()
        {
            foreach (MechanicalGroup group in groups.Values)
                group.AttachBlocks();
        }

        public void Clean()
        {
            foreach (MechanicalGroup group in groups.Values)
                group.Clean();
        }

        private MechanicalGroup GetGroup(MechanicalConnectionType type)
        {
            if (type == MechanicalConnectionType.Unknown)
                return null;

            MechanicalGroup group;
            if (groups.TryGetValue(type, out group))
                return group;

            group = new MechanicalGroup();
            groups.Add(type, group);
            return group;
        }

        public void Add(MechanicalTopBlock topBlock)
        {
            MechanicalGroup group = GetGroup(topBlock.Type);
            if (group != null)
            {
                group.TopBlocks.Add(topBlock);
                hasTopBlocks = true;
            }
        }

        public void Add(MechanicalBaseBlock baseBlock)
        {
            MechanicalGroup group = GetGroup(baseBlock.Type);
            if (group != null)
            {
                group.BaseBlocks.Add(baseBlock);
                hasBaseBlocks = true;
            }
        }

        private class MechanicalGroup
        {
            public List<MechanicalTopBlock> TopBlocks { get; } = new List<MechanicalTopBlock>();
            public List<MechanicalBaseBlock> BaseBlocks { get; } = new List<MechanicalBaseBlock>();

            public void SearchBlocks(MechanicalGroup other)
            {
                // Search the other group for top blocks

                if (other.TopBlocks.Count == 0 || BaseBlocks.Count == 0)
                    return;

                foreach (MechanicalBaseBlock baseBlock in BaseBlocks)
                {
                    foreach (MechanicalTopBlock topBlock in other.TopBlocks)
                        baseBlock.TestAlignment(topBlock);
                }
            }

            public void AttachBlocks()
            {
                foreach (MechanicalBaseBlock baseBlock in BaseBlocks)
                    baseBlock.Attach();
            }

            public void Clean()
            {
                // Top blocks that were not attached to a base block will still have an invalid entityid reference
                foreach (MechanicalTopBlock topBlock in TopBlocks)
                {
                    if (!topBlock.Attached)
                        topBlock.SetBaseBlock(0);
                }
            }
        }
    }
}
