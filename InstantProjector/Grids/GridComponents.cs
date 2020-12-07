using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;

namespace avaness.GridSpawner.Grids
{
    public class GridComponents : IEnumerable<KeyValuePair<MyDefinitionId, int>>
    {
        public int BlockCount { get; private set; } = 0;

        private Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();

        public GridComponents()
        {
        }

        public GridComponents(IMyCubeGrid grid)
        {
            Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();
            List<IMySlimBlock> temp = new List<IMySlimBlock>(0);
            grid.GetBlocks(temp, (slim) =>
            {
                Include((MyCubeBlockDefinition)slim.BlockDefinition);
                return false;
            });
        }

        public GridComponents(IMyProjector p)
        {
            if(p.ProjectedGrid != null)
            {
                Dictionary<MyDefinitionId, int> ids = new Dictionary<MyDefinitionId, int>();
                MyObjectBuilder_Projector ob = (MyObjectBuilder_Projector)p.GetObjectBuilderCubeBlock(true);
                foreach(MyObjectBuilder_CubeGrid grid in ob.ProjectedGrids)
                {
                    foreach(MyObjectBuilder_CubeBlock block in grid.CubeBlocks)
                    {
                        int num;
                        MyDefinitionId id = block.GetId();
                        if (ids.TryGetValue(id, out num))
                            ids[id] = num + 1;
                        else
                            ids[id] = 1;

                    }
                }

                foreach(KeyValuePair<MyDefinitionId, int> kv in ids)
                {
                    MyCubeBlockDefinition def = MyDefinitionManager.Static.GetCubeBlockDefinition(kv.Key);
                    if (def != null)
                        IncludeCount(def, kv.Value);
                }
            }
        }

        public void Include(MyCubeBlockDefinition def)
        {
            foreach(MyCubeBlockDefinition.Component c in def.Components)
            {
                MyDefinitionId id = c.Definition.Id;
                int num;
                if (comps.TryGetValue(id, out num))
                    comps[id] = num + c.Count;
                else
                    comps.Add(id, c.Count);
            }
            BlockCount++;
        }

        public void IncludeCount(MyCubeBlockDefinition def, int count)
        {
            foreach(MyCubeBlockDefinition.Component c in def.Components)
            {
                int cCount = c.Count * count;
                MyDefinitionId id = c.Definition.Id;
                int num;
                if (comps.TryGetValue(id, out num))
                    comps[id] = num + cCount;
                else
                    comps.Add(id, cCount);
            }
            BlockCount += count;
        }


        public void ApplySettings(Settings.MapSettings config)
        {
            float modifier = config.ComponentCostModifier;
            Dictionary<MyDefinitionId, int> newDict;
            if(modifier != 1)
            {
                newDict = new Dictionary<MyDefinitionId, int>(comps.Count);
                foreach (KeyValuePair<MyDefinitionId, int> kv in comps)
                {
                    int newCost = (int)Math.Round(kv.Value * modifier);
                    if (newCost > 0)
                        newDict.Add(kv.Key, newCost);

                }
            }
            else
            {
                newDict = comps;
            }

            MyDefinitionId? extraComp = config.ExtraComponent;
            if (extraComp.HasValue)
            {
                MyDefinitionId id = extraComp.Value;
                int count;
                if (!newDict.TryGetValue(id, out count))
                    count = 0;

                int numExtraComps = (int)Math.Round(BlockCount * config.ExtraCompCost);
                if (numExtraComps <= 0)
                    numExtraComps = 1;

                newDict[id] = count + numExtraComps;
            }

            comps = newDict;
        }

        public bool HasComponents(IEnumerable<IMyInventory> inventories, out int neededCount, out string neededName)
        {
            foreach (KeyValuePair<MyDefinitionId, int> c in comps)
            {
                MyFixedPoint needed = CountComponents(inventories, c.Key, c.Value);
                if (needed > 0)
                {
                    neededName = c.Key.SubtypeName;
                    neededCount = (int)needed;
                    return false;
                }
            }
            neededCount = 0;
            neededName = null;
            return true;
        }

        // Context: Server
        public bool ConsumeComponents(ulong activator, IEnumerable<IMyInventory> inventories)
        {
            List<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>> toRemove = new List<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in comps)
            {
                MyFixedPoint needed = CountComponents(inventories, c.Key, c.Value, toRemove);
                if (needed > 0)
                {
                    Constants.Notify(InstantProjector.GetCompsString((int)needed, c.Key.SubtypeName), activator);
                    return false;
                }
            }

            foreach (MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint> item in toRemove)
                item.Item1.RemoveItemAmount(item.Item2, item.Item3);

            return true;
        }

        // Context: Server
        private MyFixedPoint CountComponents(IEnumerable<IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>> items = null)
        {
            MyFixedPoint targetAmount = amount;
            foreach (IMyInventory inv in inventories)
            {
                IMyInventoryItem invItem = inv.FindItem(id);
                if (invItem != null)
                {
                    if (invItem.Amount >= targetAmount)
                    {
                        if(items != null)
                            items.Add(new MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                        targetAmount = 0;
                        break;
                    }
                    else
                    {
                        if (items != null)
                            items.Add(new MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                        targetAmount -= invItem.Amount;
                    }
                }
            }
            return targetAmount;
        }

        public IEnumerator<KeyValuePair<MyDefinitionId, int>> GetEnumerator()
        {
            return comps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return comps.GetEnumerator();
        }
    }
}
