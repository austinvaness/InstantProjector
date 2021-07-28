using Sandbox.Game;
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

        public void AttachBlocks(GridMechanicalSystem other)
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

                myGroup.AttachBlocks(otherGroup.Value);
            }
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

            public void AttachBlocks(MechanicalGroup other)
            {
                // Search the other group for top blocks

                if (other.TopBlocks.Count == 0 || BaseBlocks.Count == 0)
                    return;

                for (int baseIndex = BaseBlocks.Count - 1; baseIndex >= 0; baseIndex--)
                {
                    double min = double.PositiveInfinity;
                    int minIndex = -1;
                    MechanicalBaseBlock baseBlock = BaseBlocks[baseIndex];
                    for (int topIndex = 0; topIndex < other.TopBlocks.Count; topIndex++)
                    {
                        MechanicalTopBlock topBlock = other.TopBlocks[topIndex];
                        double dist;
                        if (baseBlock.TryGetAlignment(topBlock, out dist) && dist < min)
                        {
                            min = dist;
                            minIndex = topIndex;
                            break;
                        }
                    }
                    
                    if(minIndex >= 0)
                    {
                        MechanicalTopBlock minTop = other.TopBlocks[minIndex];
                        baseBlock.SetTopBlock(minTop.EntityId);
                        minTop.SetBaseBlock(baseBlock.EntityId);
                        BaseBlocks.RemoveAtFast(baseIndex);
                        other.TopBlocks.RemoveAtFast(minIndex);
                    }
                }
            }

            public void Clean()
            {
                foreach (MechanicalTopBlock topBlock in TopBlocks)
                    topBlock.SetBaseBlock(0);
                foreach (MechanicalBaseBlock baseBlock in BaseBlocks)
                    baseBlock.SetTopBlock(0);

            }
        }
    }
}
