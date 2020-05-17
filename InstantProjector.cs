using BulletXNA.BulletCollision;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.VisualScripting;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace GridSpawner
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false, "SmallProjector", "LargeProjector")]
    public class InstantProjector : MyGameLogicComponent
    {
        public static bool controls = false;

        public enum State
        {
            Idle, Building, Waiting
        }

        private IMyProjector me;

        private Sync<float> _timeout;
        private float Timeout
        {
            get
            {
                return _timeout.Value;
            }
            set
            {
                _timeout.Value = value;
            }
        }


        private Sync<State> _state;
        private State BuildState
        {
            get
            {
                return _state.Value;
            }
            set
            {
                _state.Value = value;
            }
        }

        // Context: All
        public override void Init (MyObjectBuilder_EntityBase objectBuilder)
        {
            me = Entity as IMyProjector;
            _timeout = new Sync<float>(1, me, 0);
            _state = new Sync<State>(2, me, State.Idle);
            _state.OnValueReceived += Sync_RefreshCustomInfo;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        private void Sync_RefreshCustomInfo (byte id)
        {
            me.RefreshCustomInfo();
        }

        // Context: All
        public override void Close ()
        {
            if(me != null)
                me.AppendingCustomInfo -= CustomInfo;
            if(_state != null)
                _state.OnValueReceived -= Sync_RefreshCustomInfo;

        }

        // Context: All
        private void CustomInfo (IMyTerminalBlock block, StringBuilder sb)
        {
            if (BuildState == State.Building)
            {
                sb.Append("Building ship...").AppendLine();
            }
            else if (BuildState == State.Waiting)
            {
                sb.Append("Waiting for ");
                AppendTime(sb, (int)Math.Round(Timeout));
                sb.AppendLine();
            }
        }

        // Context: All
        public override void UpdateOnceBeforeFrame ()
        {
            if (!controls)
            {
                IMyTerminalControlSeparator sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyProjector>("BuildGridSep");
                sep.Enabled = IsValid;
                sep.Visible = IsValid;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(sep);

                IMyTerminalControlButton btnBuild = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("BuildGrid");
                btnBuild.Enabled = CanBuildProjection;
                btnBuild.Visible = IsValid;
                btnBuild.SupportsMultipleBlocks = true;
                btnBuild.Title = MyStringId.GetOrCompute("Build Grid");
                btnBuild.Action = BuildClient;
                btnBuild.Tooltip = MyStringId.GetOrCompute("Builds the projected grid instantly.\nThere will be a cooldown after building.");
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(btnBuild);

                IMyTerminalControlTextbox txtTimeout = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyProjector>("GridTimeout");
                txtTimeout.Enabled = (b) => false;
                txtTimeout.Visible = IsValid;
                txtTimeout.Getter = GetTimeout;
                txtTimeout.Setter = (b, s) => { };
                txtTimeout.SupportsMultipleBlocks = false;
                txtTimeout.Title = MyStringId.GetOrCompute("Build Cooldown");
                txtTimeout.Tooltip = MyStringId.GetOrCompute("The amount of time you must wait after building a grid to be able to build another.");
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(txtTimeout);

                IMyTerminalAction aBuild = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BuildGridAction");
                aBuild.Enabled = IsValid;
                aBuild.Action = BuildClient;
                aBuild.ValidForGroups = true;
                aBuild.Name = new StringBuilder("Create Grid");
                aBuild.Writer = (b, s) => s.Append("Create Grid");
                MyAPIGateway.TerminalControls.AddAction<IMyProjector>(aBuild);

                IMyTerminalControlListbox itemList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyProjector>("ComponentList");
                itemList.Enabled = CanBuildProjection;
                itemList.Visible = IsValid;
                itemList.ListContent = GetItemList;
                itemList.Multiselect = false;
                itemList.SupportsMultipleBlocks = false;
                itemList.Title = MyStringId.GetOrCompute("Components");
                itemList.VisibleRowsCount = 10;
                itemList.ItemSelected = (b, l) => { };
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(itemList);
                MyLog.Default.WriteLine("Initialized Instant Projector.");
                controls = true;
            }

            me.AppendingCustomInfo += CustomInfo;
        }

        // Context: All
        private StringBuilder GetTimeout (IMyTerminalBlock block)
        {
            StringBuilder sb = new StringBuilder();
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl.BuildState == State.Waiting)
            {
                AppendTime(sb, (int)Math.Round(gl.Timeout));
            }
            else
            {
                IMyCubeGrid grid = gl.me.ProjectedGrid;
                if (grid != null)
                    AppendTime(sb, gl.GetTimeout());
            }
            return sb;
        }

        // Context: All
        private int GetTimeout()
        {
            IMyCubeGrid grid = me.ProjectedGrid;
            if (grid == null)
                return 0;
            return (int)Math.Round(((MyCubeGrid)grid).GetBlocks().Count * Constants.timeoutMultiplier);
        }

        // Context: All
        private void AppendTime(StringBuilder sb, int totalSeconds)
        {
            int seconds = totalSeconds % 60;
            int totalMinutes = totalSeconds / 60;
            int minutes = totalMinutes % 60;
            int hours = totalMinutes / 60;

            bool h = hours > 0;
            if (h)
                sb.Append(hours).Append(':');

            bool m = totalMinutes > 0;
            if (m)
            {
                if (h && minutes < 10)
                    sb.Append('0');
                sb.Append(minutes).Append(':');
            }

            if (m && seconds < 10)
                sb.Append('0');
            sb.Append(seconds);
        }

        // Context: Server
        public override void UpdateAfterSimulation ()
        {
            float newTimeout = Timeout;
            newTimeout -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            if (newTimeout <= 0)
            {
                newTimeout = 0;
                if(BuildState == State.Waiting)
                    BuildState = State.Idle;
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            }
            me.RefreshCustomInfo();
            Timeout = newTimeout;
        }

        // Context: All
        private void GetItemList (IMyTerminalBlock block, List<MyTerminalControlListBoxItem> items, List<MyTerminalControlListBoxItem> selected)
        {
            IMyProjector me = (IMyProjector)block;
            if(me.ProjectedGrid != null)
            {
                StringBuilder sb = new StringBuilder();
                Dictionary<MyDefinitionId, int> comps = GetComponents(me.ProjectedGrid);
                foreach(KeyValuePair<MyDefinitionId, int> kv in comps)
                {
                    sb.Append(kv.Key.SubtypeName).Append(": ").Append(kv.Value);
                    MyStringId s = MyStringId.GetOrCompute(sb.ToString());
                    MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(s, s, 0);
                    items.Add(item);
                    sb.Clear();
                }
            }
        }

        // Context: All
        private void BuildClient (IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null)
                return;

            if (CanBuildProjection(block))
                new PacketBuild(block).SendToServer();

        }

        // Context: Server
        public void BuildServer(ulong activator)
        {
            InstantSpawn(!MyAPIGateway.Session.CreativeMode, activator);
        }

        // Context: Server
        private void InstantSpawn(bool useComponents, ulong activator)
        {
            MyObjectBuilder_CubeGrid builder;
            Dictionary<MyDefinitionId, int> components;
            if(TryGetGrid(me, activator, out builder, out components))
            {
                if(!useComponents || ConsumeComponents(activator, GetInventories(), components))
                {
                    float timeout = GetTimeout();
                    BuildState = State.Building;
                    MyAPIGateway.Entities.CreateFromObjectBuilderParallel(builder, false, (e) => AddEntity(e, activator, timeout));
                    me.RefreshCustomInfo();
                }
            }
        }

        // Context: Server
        private void AddEntity (IMyEntity e, ulong activator, float timeout)
        {
            if(HasClearArea(e, activator))
                MyAPIGateway.Entities.AddEntity(e, true);
            if(timeout > 0)
            {
                BuildState = State.Waiting;
                Timeout = timeout;
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            else
            {
                BuildState = State.Idle;
            }
            me.RefreshCustomInfo();
        }

        // Context: Server
        private IEnumerable<IMyInventory> GetInventories()
        {
            List<IMyCubeGrid> grids = MyAPIGateway.GridGroups.GetGroup(me.CubeGrid, GridLinkTypeEnum.Logical);
            foreach (IMyCubeGrid g in grids)
            {
                MyCubeGrid grid = (MyCubeGrid)g;
                foreach (var block in grid.GetFatBlocks())
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        yield return ((IMyCubeBlock)block).GetInventory(i);
                    }
                }
            }
        }

        // Context: Server
        private bool ConsumeComponents(ulong activator, IEnumerable<IMyInventory> inventories, IDictionary<MyDefinitionId, int> components)
        {
            List<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>> toRemove = new List<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in components)
            {
                MyFixedPoint needed = CountComponents(inventories, c.Key, c.Value, toRemove);
                if(needed > 0)
                {
                    Constants.Notify(needed + " " + c.Key.SubtypeName + Constants.msgMissingComp, activator);
                    return false;
                }
            }

            foreach (MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint> item in toRemove)
                item.Item1.RemoveItemAmount(item.Item2, item.Item3);

            return true;
        }

        // Context: Server
        private MyFixedPoint CountComponents(IEnumerable<IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>> items)
        {
            MyFixedPoint targetAmount = amount;
            foreach (IMyInventory inv in inventories)
            {
                IMyInventoryItem invItem = inv.FindItem(id);
                if (invItem != null)
                {
                    if (invItem.Amount >= targetAmount)
                    {
                        items.Add(new MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>(inv, invItem, targetAmount));
                        targetAmount = 0;
                        break;
                    }
                    else
                    {
                        items.Add(new MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>(inv, invItem, invItem.Amount));
                        targetAmount -= invItem.Amount;
                    }
                }
            }
            return targetAmount;
        }

        // Context: All
        private static void GetComponents (MyCubeBlockDefinition def, IDictionary<MyDefinitionId, int> components)
        {
            if(def?.Components != null)
            {
                foreach (MyCubeBlockDefinition.Component c in def.Components)
                {
                    MyDefinitionId id = c.Definition.Id;
                    int num;
                    if (components.TryGetValue(id, out num))
                        components [id] = num + c.Count;
                    else
                        components.Add(id, c.Count);
                }
            }
        }

        // Context: All
        private static Dictionary<MyDefinitionId, int> GetComponents (IMyCubeGrid projection)
        {
            Dictionary<MyDefinitionId, int> comps = new Dictionary<MyDefinitionId, int>();
            List<IMySlimBlock> temp = new List<IMySlimBlock>(0);
            projection.GetBlocks(temp, (slim) =>
            {
                GetComponents((MyCubeBlockDefinition)slim.BlockDefinition, comps);
                return false;
            });
            return comps;
        }

        // Context: Server
        private bool TryGetGrid(IMyProjector p, ulong activator, out MyObjectBuilder_CubeGrid builder, out Dictionary<MyDefinitionId, int> components)
        {
            builder = null;
            components = new Dictionary<MyDefinitionId, int>();

            if (p.ProjectedGrid == null)
            {
                Constants.Notify(Constants.msgNoGrid, activator);
                return false;
            }

            if (!HasClearArea(p.ProjectedGrid, activator))
                return false;

            MyObjectBuilder_Projector pBuilder = (MyObjectBuilder_Projector)p.GetObjectBuilderCubeBlock(true);
            if (pBuilder.ProjectedGrids == null || pBuilder.ProjectedGrids.Count == 0)
            {
                Constants.Notify(Constants.msgNoGrid, activator);
                return false;
            }

            int maxBlocks = int.MinValue;
            MyObjectBuilder_CubeGrid maxGrid = null;
            foreach(MyObjectBuilder_CubeGrid grid in pBuilder.ProjectedGrids)
            {
                if(grid.CubeBlocks.Count > maxBlocks)
                {
                    maxBlocks = grid.CubeBlocks.Count;
                    maxGrid = grid;
                }
            }


            MyObjectBuilder_CubeGrid maxGrid2 = (MyObjectBuilder_CubeGrid)p.ProjectedGrid.GetObjectBuilder(true);
            maxGrid.PositionAndOrientation = maxGrid2.PositionAndOrientation;

            if (!PrepGrid(activator, p, maxGrid, components))
                return false;

            builder = maxGrid;
            return true;
        }

        // Context: Server
        private bool PrepGrid(ulong activator, IMyProjector p, MyObjectBuilder_CubeGrid builder, Dictionary<MyDefinitionId, int> components)
        {
            if (!builder.PositionAndOrientation.HasValue)
            {
                Constants.Notify("Error! No projector grid orientation.", activator);
                return false;
            }

            MyAPIGateway.Entities.RemapObjectBuilder(builder);

            builder.IsStatic = false;
            builder.CreatePhysics = true;
            builder.Immune = false;
            builder.DestructibleBlocks = true;
            foreach (MyObjectBuilder_CubeBlock cubeBuilder in builder.CubeBlocks)
            {
                if (MyDefinitionManagerBase.Static != null)
                {
                    if (cubeBuilder.EntityId == 0)
                    {
                        if (!Constants.RandomEntityId(out cubeBuilder.EntityId))
                            return false;
                    }

                    cubeBuilder.SetupForProjector();
                    MyCubeBlockDefinition def = MyDefinitionManager.Static.GetCubeBlockDefinition(cubeBuilder);
                    GetComponents(def, components);
                    cubeBuilder.Owner = p.OwnerId;
                    cubeBuilder.BuiltBy = p.OwnerId;
                    cubeBuilder.ShareMode = ((MyCubeBlock)p).IDModule.ShareMode;
                    if (cubeBuilder is MyObjectBuilder_FunctionalBlock)
                        ((MyObjectBuilder_FunctionalBlock)cubeBuilder).Enabled = true;
                    if (cubeBuilder is MyObjectBuilder_BatteryBlock)
                    {
                        MyBatteryBlockDefinition batDef = (MyBatteryBlockDefinition)def;
                        ((MyObjectBuilder_BatteryBlock)cubeBuilder).CurrentStoredPower = batDef.InitialStoredPowerRatio * batDef.MaxStoredPower;
                    }
                }
            }

            return true;
        }

        // Context: Server
        private bool HasClearArea(IMyEntity e, ulong activator)
        {
            List<MyEntity> entities = new List<MyEntity>();
            MyOrientedBoundingBoxD eObb = GetOBB(e);
            MyGamePruningStructure.GetAllEntitiesInOBB(ref eObb, entities);
            if (entities.Count > 0)
            {
                foreach (MyEntity entity in entities)
                {
                    IMyEntity e2 = entity;
                    if (e2.EntityId != e.EntityId && e2.Physics != null)
                    {
                        if (e2 is IMyCubeGrid)
                        {
                            if (e is IMyCubeGrid && ((IMyCubeGrid)e2).IsSameConstructAs((IMyCubeGrid)e))
                                continue;
                            if(HasBlocksInsideOBB((MyCubeGrid)e2, ref eObb))
                            {
                                Constants.Notify(Constants.msgNoSpace, activator);
                                return false;
                            }
                        }
                        else if (e2 is MyVoxelBase)
                        {
                            MyTuple<float, float> result = ((MyVoxelBase)e2).GetVoxelContentInBoundingBox_Fast(e.LocalAABB, e.WorldMatrix);
                            if (!float.IsNaN(result.Item2) && !float.IsInfinity(result.Item2) && result.Item2 != 0)
                            {
                                Constants.Notify(Constants.msgNoSpace, activator);
                                return false;
                            }
                        }
                        else
                        {
                            if (GetOBB(e2).Contains(ref eObb) != ContainmentType.Disjoint)
                            {
                                Constants.Notify(Constants.msgNoSpace, activator);
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        // Context: Server
        public static bool HasBlocksInsideOBB (MyCubeGrid grid, ref MyOrientedBoundingBoxD box)
        {
            double radius = box.HalfExtent.Length();
            radius *= grid.GridSizeR;
            Vector3I center = grid.WorldToGridInteger(box.Center);
            Vector3I gridMin = grid.Min;
            Vector3I gridMax = grid.Max;
            double radiusSq = radius * radius;
            int radiusCeil = (int)Math.Ceiling(radius);
            int x, y, z;
            Vector3I max2 = Vector3I.Min(new Vector3I (radiusCeil), gridMax - center);
            Vector3I min2 = Vector3I.Max(new Vector3I (-radiusCeil), gridMin - center);
            for (x = min2.X; x <= max2.X; ++x)
            {
                for (y = min2.Y; y <= max2.Y; ++y)
                {
                    for (z = min2.Z; z <= max2.Z; ++z)
                    {
                        if (x * x + y * y + z * z < radiusSq)
                        {
                            Vector3I cubePos = center + new Vector3I(x, y, z);
                            MyCube cube;
                            if (grid.TryGetCube(cubePos, out cube))
                            {
                                IMySlimBlock slim = (IMySlimBlock)cube.CubeBlock;
                                if (slim.Position == cubePos)
                                {
                                    Vector3D v = grid.GridIntegerToWorld(cubePos);
                                    if (box.Contains(ref v))
                                        return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        // Context: Server
        // https://github.com/rexxar-tc/ShipyardMod/blob/master/ShipyardMod/Utility/MathUtility.cs#L66
        private MyOrientedBoundingBoxD GetOBB (IMyEntity e)
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(e.WorldMatrix);
            Vector3D exts = e.PositionComp.LocalAABB.HalfExtents;
            return new MyOrientedBoundingBoxD(e.PositionComp.WorldAABB.Center, exts, quat);
        }

        // Context: All
        private bool CanBuildProjection (IMyTerminalBlock block)
        {
            if (block.CubeGrid?.Physics == null || !block.IsWorking)
                return false;
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl == null)
                return false;
            return gl.me.ProjectedGrid != null && gl.BuildState == State.Idle;
        }
        
        // Context: All
        private bool IsValid (IMyTerminalBlock block)
        {
            return block.CubeGrid?.Physics != null && block.GameLogic.GetAs<InstantProjector>() != null;
        }

    }
}
