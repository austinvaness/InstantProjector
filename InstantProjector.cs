﻿using avaness.GridSpawner.Networking;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace avaness.GridSpawner
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false, "SmallProjector", "LargeProjector")]
    public class InstantProjector : MyGameLogicComponent
    {
        private static bool controls = false;

        public enum State
        {
            Idle, Building, Waiting
        }

        private IMyProjector me;

        private SyncableProjectorState _state;

        private int Timeout
        {
            get
            {
                return _state.Timeout;
            }
            set
            {
                _state.Timeout = value;
            }
        }

        private State BuildState
        {
            get
            {
                return _state.BuildState;
            }
            set
            {
                _state.BuildState = value;
                me.RefreshCustomInfo();
            }
        }

        // Context: All
        public override void Init (MyObjectBuilder_EntityBase objectBuilder)
        {
            me = Entity as IMyProjector;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        // Context: All
        public override void Close ()
        {
            if (me != null)
            {
                me.AppendingCustomInfo -= CustomInfo;
                if (_state != null)
                {
                    if (Constants.IsServer && Timeout > 0 && me.CubeGrid != null && IPSession.Instance != null)
                        IPSession.Instance.SetGridTimeout(me.CubeGrid, Timeout);
                    _state.OnValueReceived -= ReceivedNewState;
                    _state.Close();
                }
            }

        }

        // Context: Client
        private void ReceivedNewState ()
        {
            if(_state.BuildState == State.Waiting)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            else
            {
                Timeout = 0;
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            }
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
                AppendTime(sb, Timeout);
                sb.AppendLine();
            }
        }

        // Context: All
        public override void UpdateOnceBeforeFrame ()
        {
            me.AppendingCustomInfo += CustomInfo;
            _state = new SyncableProjectorState(0, me, State.Idle, 0);
            if (Constants.IsServer)
            {
                Timeout = IPSession.Instance.GetGridTimeout(me.CubeGrid);
                if (Timeout > 0)
                {
                    BuildState = State.Waiting;
                    NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                }
                else
                {
                    BuildState = State.Idle;
                }
            }
            else
            {
                _state.RequestFromServer();
                _state.OnValueReceived += ReceivedNewState;
            }

            if (!controls)
            {
                IMyTerminalControlSeparator sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyProjector>("BuildGridSep");
                sep.Enabled = IsValid;
                sep.Visible = IsValid;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(sep);


                IMyTerminalControlButton btnBuild = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("BuildGrid");
                btnBuild.Enabled = IsWorking;
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

                // Button panels are special and trigger on the server instead of the client, making everything more complicated.
                IMyTerminalAction aBuild = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BuildGridAction");
                aBuild.Enabled = IsValid;
                aBuild.Action = BuildClient; // For all except button panels
                aBuild.ValidForGroups = true;
                aBuild.Name = new StringBuilder("Create Grid");
                aBuild.Writer = (b, s) => s.Append("Create Grid");
                aBuild.InvalidToolbarTypes = new [] { MyToolbarType.ButtonPanel }.ToList();
                MyAPIGateway.TerminalControls.AddAction<IMyProjector>(aBuild);
                IMyTerminalAction aBuild2 = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BuildGridAction2");
                aBuild2.Enabled = IsValid;
                aBuild2.Action = BuildClientUnsafe; // For only button panels
                aBuild2.ValidForGroups = true;
                aBuild2.Name = new StringBuilder("Create Grid");
                aBuild2.Writer = (b, s) => s.Append("Create Grid");
                aBuild2.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.BuildCockpit, MyToolbarType.Character, MyToolbarType.LargeCockpit, 
                    MyToolbarType.None, MyToolbarType.Seat, MyToolbarType.Ship, MyToolbarType.SmallCockpit, MyToolbarType.Spectator};
                MyAPIGateway.TerminalControls.AddAction<IMyProjector>(aBuild2);

                IMyTerminalControlListbox itemList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyProjector>("ComponentList");
                itemList.Enabled = IsWorking;
                itemList.Visible = IsValid;
                itemList.ListContent = GetItemList;
                itemList.Multiselect = false;
                itemList.SupportsMultipleBlocks = false;
                itemList.Title = MyStringId.GetOrCompute("Components");
                itemList.VisibleRowsCount = 10;
                itemList.ItemSelected = (b, l) => { };
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(itemList);

                IMyTerminalControlProperty<Dictionary<MyItemType, int>> itemListProp 
                    = MyAPIGateway.TerminalControls.CreateProperty<Dictionary<MyItemType, int>, IMyProjector>("RequiredComponents");
                itemListProp.Enabled = IsWorking;
                itemListProp.Visible = IsValid;
                itemListProp.SupportsMultipleBlocks = false;
                itemListProp.Setter = (b, l) => { };
                itemListProp.Getter = GetItemListPB;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(itemListProp);

                MyLog.Default.WriteLineAndConsole("Initialized Instant Projector.");
                controls = true;
            }

        }

        // Context: All
        public override void UpdateBeforeSimulation ()
        {
            int newTimeout = --Timeout;
            if(Constants.IsServer)
                newTimeout = IPSession.Instance.SetGridTimeout(me.CubeGrid, newTimeout);
            if (newTimeout <= 0)
            {
                newTimeout = 0;
                if (Constants.IsServer && BuildState == State.Waiting)
                    BuildState = State.Idle;
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            }
            me.RefreshCustomInfo();
            Timeout = newTimeout;
        }

        // Context: Terminal
        private static StringBuilder GetTimeout (IMyTerminalBlock block)
        {
            StringBuilder sb = new StringBuilder();
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl.BuildState == State.Waiting)
            {
                AppendTime(sb, gl.Timeout);
                sb.Append(" (Active)");
            }
            else
            {
                IMyCubeGrid grid = gl.me.ProjectedGrid;
                if (grid != null)
                    AppendTime(sb, gl.GetTimeout());
            }
            return sb;
        }

        // Context: Terminal
        private int GetTimeout ()
        {
            IMyCubeGrid grid = me.ProjectedGrid;
            if (grid == null)
                return 0;
            return (int)Math.Round(((MyCubeGrid)grid).GetBlocks().Count * Constants.timeoutMultiplier * 60);
        }

        // Context: Terminal
        private static void AppendTime (StringBuilder sb, int ticks)
        {
            int totalSeconds = (int)Math.Round(ticks / 60f);
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

        // Context: Terminal
        private static void GetItemList (IMyTerminalBlock block, List<MyTerminalControlListBoxItem> items, List<MyTerminalControlListBoxItem> selected)
        {
            IMyProjector me = (IMyProjector)block;
            if (me.ProjectedGrid != null)
            {
                StringBuilder sb = new StringBuilder();
                Dictionary<MyDefinitionId, int> comps = GetComponents(me.ProjectedGrid);
                foreach (KeyValuePair<MyDefinitionId, int> kv in comps)
                {
                    sb.Append(kv.Key.SubtypeName).Append(": ").Append(kv.Value);
                    MyStringId s = MyStringId.GetOrCompute(sb.ToString());
                    MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(s, s, 0);
                    items.Add(item);
                    sb.Clear();
                }
            }
        }

        // Context: Terminal
        private static Dictionary<MyItemType, int> GetItemListPB(IMyTerminalBlock block)
        {
            IMyProjector me = (IMyProjector)block;
            if (me.ProjectedGrid != null)
                return GetComponents(me.ProjectedGrid).ToDictionary(kv => new MyItemType(kv.Key.TypeId, kv.Key.SubtypeId), kv => kv.Value);
            return new Dictionary<MyItemType, int>();
        }

        // Context: Terminal
        private static void BuildClient (IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, true).SendToServer();
        }

        // Context: Terminal
        private static void BuildClientUnsafe (IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, false).SendToServer();
        }

        // Context: Server
        public void BuildServer (ulong activator, bool trustSender)
        {
            if (!trustSender && MyAPIGateway.Session.Player?.SteamUserId == activator)
                activator = 0;
            if (me.ProjectedGrid == null)
                Constants.Notify(Constants.msgNoGrid, activator);
            else if (BuildState == State.Building)
                Constants.Notify(Constants.msgBuilding, activator);
            else if (BuildState == State.Waiting)
                Constants.Notify(Constants.msgWaiting, activator);
            else
                InstantSpawn(!MyAPIGateway.Session.CreativeMode, activator);
        }

        // Context: Server
        private void InstantSpawn (bool useComponents, ulong activator)
        {
            MyObjectBuilder_CubeGrid builder;
            Dictionary<MyDefinitionId, int> components;
            if (TryGetGrid(me, activator, out builder, out components))
            {
                int time = IPSession.Instance.Runtime;
                int timeout = GetTimeout();

                if(MyAPIGateway.Entities.CreateFromObjectBuilderParallel(builder, false, 
                    (e) => AddEntity(e, activator, timeout, useComponents, components, time)) != null)
                {
                    BuildState = State.Building;
                }
                else
                {
                    Constants.Notify(Constants.msgError + "2", activator);
                    BuildState = State.Idle;
                }
            }
        }

        // Context: Server
        private void AddEntity (IMyEntity e, ulong activator, int timeout, bool useComponents, Dictionary<MyDefinitionId, int> components, int startTime)
        {
            AccelerateTime(e, startTime);
            if (HasClearArea((IMyCubeGrid)e))
            {
                if (!useComponents || ConsumeComponents(activator, GetInventories(), components))
                {
                    if (timeout > 0)
                    {
                        Timeout = timeout;
                        BuildState = State.Waiting;
                        NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                    }
                    else
                    {
                        BuildState = State.Idle;
                    }
                    MyAPIGateway.Entities.AddEntity(e, true);
                    return;
                }
            }
            else
            {
                Constants.Notify(Constants.msgNoSpace, activator);
            }
            BuildState = State.Idle;
            MyAPIGateway.Entities.MarkForClose(e);
        }

        // Context: Server
        private void AccelerateTime(IMyEntity e, int startTime)
        {
            float deltaTime = Math.Max(IPSession.Instance.Runtime - startTime, 0) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MatrixD temp = e.WorldMatrix;
            temp.Translation += e.Physics.LinearVelocity * deltaTime;
            e.WorldMatrix = temp;
        }

        // Context: Server
        private List<IMyInventory> GetInventories ()
        {
            List<IMyCubeGrid> grids = MyAPIGateway.GridGroups.GetGroup(me.CubeGrid, GridLinkTypeEnum.Logical);
            List<IMyInventory> inventories = new List<IMyInventory>();
            long owner = me.OwnerId;
            foreach (IMyCubeGrid g in grids)
            {
                MyCubeGrid grid = (MyCubeGrid)g;
                foreach (var block in grid.GetFatBlocks())
                {
                    if(owner != 0)
                    {
                        MyRelationsBetweenPlayerAndBlock relation = block.GetUserRelationToOwner(owner);
                        if (relation == MyRelationsBetweenPlayerAndBlock.Enemies)
                            continue;
                    }

                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        IMyInventory inv = ((IMyCubeBlock)block).GetInventory(i);
                        inventories.Add(inv);
                    }
                }
            }
            return inventories;
        }

        // Context: Server
        private bool ConsumeComponents (ulong activator, IEnumerable<IMyInventory> inventories, IDictionary<MyDefinitionId, int> components)
        {
            List<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>> toRemove = new List<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>>();
            foreach (KeyValuePair<MyDefinitionId, int> c in components)
            {
                MyFixedPoint needed = CountComponents(inventories, c.Key, c.Value, toRemove);
                if (needed > 0)
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
        private MyFixedPoint CountComponents (IEnumerable<IMyInventory> inventories, MyDefinitionId id, int amount, ICollection<MyTuple<IMyInventory, IMyInventoryItem, MyFixedPoint>> items)
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
            if (def?.Components != null)
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
        private bool TryGetGrid (IMyProjector p, ulong activator, out MyObjectBuilder_CubeGrid builder, out Dictionary<MyDefinitionId, int> components)
        {
            builder = null;
            components = new Dictionary<MyDefinitionId, int>();

            if (p.CubeGrid?.Physics == null)
            {
                Constants.Notify(Constants.msgError + "1", activator);
                return false;
            }

            if (p.ProjectedGrid == null)
            {
                Constants.Notify(Constants.msgNoGrid, activator);
                return false;
            }

            Vector3D? newPos = null;
            if (!HasClearArea(p.ProjectedGrid))
            {
                newPos = FindClearArea(p.ProjectedGrid);
                if(newPos.HasValue)
                {
                    Constants.Notify(Constants.msgDifferentSpace, activator);
                }
                else if(!newPos.HasValue)
                {
                    Constants.Notify(Constants.msgNoSpace, activator);
                    return false;
                }
            }

            MyObjectBuilder_Projector pBuilder = (MyObjectBuilder_Projector)p.GetObjectBuilderCubeBlock(true);
            if (pBuilder.ProjectedGrids == null || pBuilder.ProjectedGrids.Count == 0)
            {
                Constants.Notify(Constants.msgNoGrid, activator);
                return false;
            }

            int maxBlocks = int.MinValue;
            MyObjectBuilder_CubeGrid maxGrid = null;
            foreach (MyObjectBuilder_CubeGrid grid in pBuilder.ProjectedGrids)
            {
                if (grid.CubeBlocks.Count > maxBlocks)
                {
                    maxBlocks = grid.CubeBlocks.Count;
                    maxGrid = grid;
                }
            }


            MyObjectBuilder_CubeGrid maxGrid2 = (MyObjectBuilder_CubeGrid)p.ProjectedGrid.GetObjectBuilder(true);
            if (newPos.HasValue && maxGrid2.PositionAndOrientation.HasValue)
            {
                MyPositionAndOrientation temp = maxGrid2.PositionAndOrientation.Value;
                temp.Position = newPos.Value;
                maxGrid.PositionAndOrientation = temp;
            }
            else
            {
                maxGrid.PositionAndOrientation = maxGrid2.PositionAndOrientation;
            }

            if (!PrepGrid(activator, p, maxGrid, components))
                return false;

            builder = maxGrid;
            builder.LinearVelocity = p.CubeGrid.Physics.LinearVelocity;
            return true;
        }

        // Context: Server
        private bool PrepGrid (ulong activator, IMyProjector p, MyObjectBuilder_CubeGrid builder, Dictionary<MyDefinitionId, int> components)
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
        private bool HasClearArea(IMyCubeGrid projectedGrid)
        {
            List<MyEntity> entities = new List<MyEntity>();
            MyOrientedBoundingBoxD projectedObb = GetOBB(projectedGrid);
            MyGamePruningStructure.GetAllEntitiesInOBB(ref projectedObb, entities);
            if (entities.Count > 0)
            {
                foreach (MyEntity entity in entities)
                {
                    IMyEntity e = entity;
                    if (e.EntityId != projectedGrid.EntityId && e.Physics != null && e.Physics.Enabled)
                    {
                        if (e is IMyCubeGrid)
                        {
                            if (((IMyCubeGrid)e).IsSameConstructAs(projectedGrid))
                                continue;
                            if (HasBlocksInsideOBB((MyCubeGrid)e, ref projectedObb))
                                return false;
                        }
                        else if (e is MyVoxelBase)
                        {
                            MyTuple<float, float> result = ((MyVoxelBase)e).GetVoxelContentInBoundingBox_Fast(projectedGrid.LocalAABB, projectedGrid.WorldMatrix);
                            if (!float.IsNaN(result.Item2) && !float.IsInfinity(result.Item2) && result.Item2 != 0)
                                return false;
                        }
                        else
                        {
                            if (GetOBB(e).Contains(ref projectedObb) != ContainmentType.Disjoint)
                                return false;
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
            Vector3I max2 = Vector3I.Min(new Vector3I(radiusCeil), gridMax - center);
            Vector3I min2 = Vector3I.Max(new Vector3I(-radiusCeil), gridMin - center);
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

        private Vector3D? FindClearArea(IMyEntity e)
        {
            BoundingSphereD vol = e.WorldVolume;
            Vector3D? result = MyAPIGateway.Entities.FindFreePlace(vol.Center, (float)vol.Radius);
            if (result.HasValue && Vector3D.DistanceSquared(vol.Center, result.Value) > Constants.maxNewDist2)
                result = null;
            return result;
        }

        // Context: Server
        // https://github.com/rexxar-tc/ShipyardMod/blob/master/ShipyardMod/Utility/MathUtility.cs#L66
        private MyOrientedBoundingBoxD GetOBB (IMyEntity e)
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(e.WorldMatrix);
            Vector3D exts = e.PositionComp.LocalAABB.HalfExtents;
            return new MyOrientedBoundingBoxD(e.PositionComp.WorldAABB.Center, exts, quat);
        }

        // Context: Terminal
        private static bool IsWorking (IMyTerminalBlock block)
        {
            return IsValid(block) && block.IsWorking;
        }

        // Context: Terminal
        private static bool IsValid (IMyTerminalBlock block)
        {
            return block.CubeGrid?.Physics != null && block.GameLogic.GetAs<InstantProjector>() != null;
        }

    }
}
