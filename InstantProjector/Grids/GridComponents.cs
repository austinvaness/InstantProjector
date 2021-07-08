using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;

namespace avaness.GridSpawner.Grids
{
    public class GridComponents : IEnumerable<KeyValuePair<MyDefinitionId, int>>
    {
        public int BlockCount { get; private set; } = 0;

        private bool warnSubgrids = false;
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
                Include(new BlockComponents(slim));
                if (slim.BlockDefinition is MyMechanicalConnectionBlockBaseDefinition)
                    warnSubgrids = true;
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
                        if(BlockComponents.IsComplete(block))
                        {
                            int num;
                            MyDefinitionId id = block.GetId();
                            if (ids.TryGetValue(id, out num))
                                ids[id] = num + 1;
                            else
                                ids[id] = 1;
                        }
                        else
                        {
                            Include(new BlockComponents(block));
                        }
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

        public void Include(BlockComponents components)
        {
            if (!components.Valid)
                return;

            foreach(MyCubeBlockDefinition.Component c in components.GetComponents())
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
            if (def == null)
                return;

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

        public bool HasComponents(IEnumerable<IMyInventory> inventories, out int neededCount, out MyDefinitionId neededId)
        {
            foreach (KeyValuePair<MyDefinitionId, int> c in comps)
            {
                MyFixedPoint needed = CountComponents(inventories, c.Key, c.Value);
                if (needed > 0)
                {
                    neededId = c.Key;
                    neededCount = (int)needed;
                    return false;
                }
            }
            neededCount = 0;
            neededId = new MyDefinitionId();
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
                    Utilities.Notify(Utilities.GetCompsString((int)needed, c.Key), activator);
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

        private List<ScreenItem> CountAllComponents(IEnumerable<IMyInventory> inventories, out bool complete)
        {
            List<ScreenItem> items = new List<ScreenItem>();

            complete = true;
            IPSession ipSession = IPSession.Instance;
            foreach(KeyValuePair<MyDefinitionId, int> c in comps)
            {
                MyDefinitionId id = c.Key;
                int required = c.Value;
                int need = (int)CountComponents(inventories, id, required);
                if (need > 0)
                    complete = false;
                items.Add(new ScreenItem(ipSession.GetComponentName(id), required, required - need));
            }

            return items;
        }

        public IEnumerator<KeyValuePair<MyDefinitionId, int>> GetEnumerator()
        {
            return comps.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return comps.GetEnumerator();
        }

        public void ShowScreen(IEnumerable<IMyInventory> inventories)
        {
            StringBuilder sb = new StringBuilder();

            if(warnSubgrids)
            {
                sb.AppendLine("This list does not include components from any blocks attached via rotor, hinge, piston, or suspension.");
                sb.AppendLine();
            }

            bool complete;
            foreach(ScreenItem item in CountAllComponents(inventories, out complete))
            {
                sb.Append(item.Name).Append(": ").Append(item.Count).Append('/').Append(item.Required).AppendLine();
            }

            if(complete)
            {
                sb.AppendLine();
                sb.AppendLine("All components available!");
            }

            MyAPIGateway.Utilities.ShowMissionScreen("Projected Grid Components", "", "", sb.ToString(), null, "Close");
        }

        private class ScreenItem
        {
            public ScreenItem(string name, int required, int count)
            {
                Name = name;
                Required = required;
                Count = count;
            }

            public string Name { get; }
            public int Required { get; }
            public int Count { get; }
        }
    }
}
