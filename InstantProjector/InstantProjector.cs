using avaness.GridSpawner.Grids;
using avaness.GridSpawner.Networking;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace avaness.GridSpawner
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false, "SmallProjector", "LargeProjector", "LargeBlockConsole")]
    public class InstantProjector : MyGameLogicComponent
    {
        private static bool controls = false;

        public enum State
        {
            Idle, Waiting, Building
        }

        private IMyProjector me;
        private SyncableProjectorState _state;
        private SyncableProjectorSettings _settings;
        private MyModStorageComponentBase storage;
        private ProjectedGrid pending;
        private MyResourceSinkComponent sink;
        private float minPower, buildPower;

        // Client component cache for UI
        private long cachedCompId;
        private GridComponents cachedComps;

        private int Timer
        {
            get
            {
                return _state.Timer;
            }
            set
            {
                _state.Timer = value;
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
                sink?.Update();
                RefreshUI();
            }
        }

        private float Speed
        {
            get
            {
                return _settings.Speed;
            }
            set
            {
                _settings.Speed = value;
            }
        }

        public bool LooseArea
        {
            get
            {
                return _settings.LooseArea;
            }
            set
            {
                _settings.LooseArea = value;
            }
        }


        // Context: All
        public override void Init (MyObjectBuilder_EntityBase objectBuilder)
        {
            me = Entity as IMyProjector;
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            if(Constants.IsServer)
            {
                if (me.Storage == null)
                    me.Storage = new MyModStorageComponent();
                storage = me.Storage;
            }
        }

        // Context: All
        public override void Close ()
        {
            if (me != null)
            {
                me.IsWorkingChanged -= Me_IsWorkingChanged;
                me.AppendingCustomInfo -= CustomInfo;
                if (_state != null)
                {
                    _state.OnValueReceived -= ReceivedNewState;
                    _state.Close();
                }
                if(_settings != null)
                {
                    _settings.OnValueReceived -= SaveStorage;
                    _settings.OnValueReceived -= RefreshUI;
                    _settings.Close();
                }

                Settings.MapSettings config = IPSession.Instance.MapSettings;
                config.OnSubgridsChanged -= ClearCachedComps;
                config.OnComponentCostModifierChanged -= ClearCachedComps;
                config.OnExtraComponentChanged -= ClearCachedComps;
                config.OnExtraCompCostChanged -= ClearCachedComps;
            }
        }

        // Context: Client
        private void ReceivedNewState ()
        {
            if (_state.BuildState == State.Waiting || _state.BuildState == State.Building)
                buildPower = GetPower();

            if(_state.BuildState == State.Waiting)
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
            else
            {
                Timer = 0;
                NeedsUpdate = MyEntityUpdateEnum.NONE;
                me.RefreshCustomInfo();
            }
            sink.Update();
            RefreshUI();
        }

        // Context: All
        private void CustomInfo (IMyTerminalBlock block, StringBuilder sb)
        {
            sb.Append("Current Input: ");
            MyValueFormatter.AppendWorkInBestUnit(sink.RequiredInputByType(MyResourceDistributorComponent.ElectricityId), sb);
            sb.AppendLine();

            if (BuildState == State.Building)
            {
                sb.Append("Building ship...").AppendLine();
            }
            else if (BuildState == State.Waiting)
            {
                sb.Append("Ship will be built in ");
                AppendTime(sb, Timer);
                sb.AppendLine();
            }
        }

        // Context: All
        public override void UpdateOnceBeforeFrame ()
        {
            if (me.CubeGrid?.Physics == null)
                return;

            _state = new SyncableProjectorState(me, State.Idle, 0);

            if (Constants.IsServer)
            {
                LoadStorage();
                _settings.OnValueReceived += SaveStorage;
                BuildState = State.Idle;
                me.IsWorkingChanged += Me_IsWorkingChanged;
            }
            else
            {
                _settings = new SyncableProjectorSettings(me, 0, true);
                _state.RequestFromServer();
                _settings.RequestFromServer();
                _state.OnValueReceived += ReceivedNewState;
            }

            MyProjectorDefinition def = (MyProjectorDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(me.BlockDefinition);
            minPower = def.RequiredPowerInput;
            sink = me.Components.Get<MyResourceSinkComponent>();
            MyDefinitionId powerDef = MyResourceDistributorComponent.ElectricityId;
            sink.SetRequiredInputFuncByType(powerDef, GetCurrentPower);
            sink.Update();
            _settings.OnValueReceived += RefreshUI;

            me.AppendingCustomInfo += CustomInfo;
            me.RefreshCustomInfo();

            Settings.MapSettings config = IPSession.Instance.MapSettings;
            config.OnSubgridsChanged += ClearCachedComps;
            config.OnComponentCostModifierChanged += ClearCachedComps;
            config.OnExtraComponentChanged += ClearCachedComps;
            config.OnExtraCompCostChanged += ClearCachedComps;

            if (!controls)
            {
                // Terminal controls

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
                btnBuild.Tooltip = MyStringId.GetOrCompute("Builds the projection instantly.\nThere will be a cooldown after building.");
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(btnBuild);

                IMyTerminalControlButton btnCancel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("CancelBuildGrid");
                btnCancel.Enabled = IsWorking;
                btnCancel.Visible = IsValid;
                btnCancel.SupportsMultipleBlocks = true;
                btnCancel.Title = MyStringId.GetOrCompute("Cancel");
                btnCancel.Action = CancelClient;
                btnCancel.Tooltip = MyStringId.GetOrCompute("Cancels the build process.");
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(btnCancel);

                IMyTerminalControlCheckbox chkShift = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyProjector>("MoveProjectionArea");
                chkShift.Enabled = IsWorking;
                chkShift.Visible = IsValid;
                chkShift.SupportsMultipleBlocks = true;
                chkShift.Title = MyStringId.GetOrCompute("Loose Projection Area");
                chkShift.OnText = MyStringId.GetOrCompute("On");
                chkShift.OffText = MyStringId.GetOrCompute("Off");
                chkShift.Tooltip = MyStringId.GetOrCompute("Allow the projection to spawn in a different area if the original area is occupied.");
                chkShift.Setter = SetLooseArea;
                chkShift.Getter = GetLooseArea;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(chkShift);

                IMyTerminalControlSlider sliderSpeed = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyProjector>("BuildSpeed");
                sliderSpeed.Enabled = IsWorking;
                sliderSpeed.Visible = IsValid;
                sliderSpeed.SupportsMultipleBlocks = true;
                sliderSpeed.Title = MyStringId.GetOrCompute("Speed");
                sliderSpeed.Tooltip = MyStringId.GetOrCompute("Increasing the speed will use more energy.");
                sliderSpeed.SetLogLimits(Constants.minSpeed, Constants.maxSpeed);
                sliderSpeed.Writer = GetSpeedText;
                sliderSpeed.Getter = GetSpeed;
                sliderSpeed.Setter = SetSpeed;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(sliderSpeed);

                IMyTerminalControlTextbox txtTimeout = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyProjector>("GridTimer");
                txtTimeout.Enabled = (b) => false;
                txtTimeout.Visible = IsValid;
                txtTimeout.Getter = GetTimer;
                txtTimeout.Setter = (b, s) => { };
                txtTimeout.SupportsMultipleBlocks = false;
                txtTimeout.Title = MyStringId.GetOrCompute("Build Timer");
                txtTimeout.Tooltip = MyStringId.GetOrCompute("The amount of time you must wait after building a grid to be able to build another.");
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(txtTimeout);

                // Terminal actions
                // Button panels are special and trigger on the server instead of the client, making everything more complicated.

                IMyTerminalAction aCancel = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("CancelBuildAction");
                aCancel.Enabled = IsValid;
                aCancel.Action = CancelClient; // For all except button panels
                aCancel.ValidForGroups = true;
                aCancel.Name = new StringBuilder("Cancel Spawn Grid");
                aCancel.Writer = (b, s) => s.Append("Cancel");
                aCancel.InvalidToolbarTypes = new[] { MyToolbarType.ButtonPanel }.ToList();
                MyAPIGateway.TerminalControls.AddAction<IMyProjector>(aCancel);
                IMyTerminalAction aCancel2 = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("CancelBuildGrid");
                aCancel2.Enabled = IsValid;
                aCancel2.Action = CancelClientUnsafe; // For only button panels
                aCancel2.ValidForGroups = true;
                aCancel2.Name = new StringBuilder("Cancel Spawn Grid");
                aCancel2.Writer = (b, s) => s.Append("Cancel");
                aCancel2.InvalidToolbarTypes = new List<MyToolbarType> { MyToolbarType.BuildCockpit, MyToolbarType.Character, MyToolbarType.LargeCockpit,
                    MyToolbarType.None, MyToolbarType.Seat, MyToolbarType.Ship, MyToolbarType.SmallCockpit, MyToolbarType.Spectator};
                MyAPIGateway.TerminalControls.AddAction<IMyProjector>(aCancel2);

                IMyTerminalAction aBuild = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BuildGridAction");
                aBuild.Enabled = IsValid;
                aBuild.Action = BuildClient; // For all except button panels
                aBuild.ValidForGroups = true;
                aBuild.Name = new StringBuilder("Spawn Grid");
                aBuild.Writer = (b, s) => s.Append("Spawn");
                aBuild.InvalidToolbarTypes = new [] { MyToolbarType.ButtonPanel }.ToList();
                MyAPIGateway.TerminalControls.AddAction<IMyProjector>(aBuild);
                IMyTerminalAction aBuild2 = MyAPIGateway.TerminalControls.CreateAction<IMyProjector>("BuildGrid");
                aBuild2.Enabled = IsValid;
                aBuild2.Action = BuildClientUnsafe; // For only button panels
                aBuild2.ValidForGroups = true;
                aBuild2.Name = new StringBuilder("Spawn Grid");
                aBuild2.Writer = (b, s) => s.Append("Spawn");
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


                // Programmable Block stuff

                IMyTerminalControlProperty<Dictionary<MyItemType, int>> itemListProp 
                    = MyAPIGateway.TerminalControls.CreateProperty<Dictionary<MyItemType, int>, IMyProjector>("RequiredComponents");
                itemListProp.Enabled = IsWorking;
                itemListProp.Visible = IsValid;
                itemListProp.SupportsMultipleBlocks = false;
                itemListProp.Setter = (b, l) => { };
                itemListProp.Getter = GetItemListPB;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(itemListProp);
                
                IMyTerminalControlProperty<int> gridTimeoutProp
                    = MyAPIGateway.TerminalControls.CreateProperty<int, IMyProjector>("GridTimerProjection");
                gridTimeoutProp.Enabled = IsWorking;
                gridTimeoutProp.Visible = IsValid;
                gridTimeoutProp.SupportsMultipleBlocks = false;
                gridTimeoutProp.Setter = (b, l) => { };
                gridTimeoutProp.Getter = GetMaxTimerPB;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(gridTimeoutProp);

                IMyTerminalControlProperty<int> gridTimeoutActive
                    = MyAPIGateway.TerminalControls.CreateProperty<int, IMyProjector>("GridTimerCurrent");
                gridTimeoutActive.Enabled = IsWorking;
                gridTimeoutActive.Visible = IsValid;
                gridTimeoutActive.SupportsMultipleBlocks = false;
                gridTimeoutActive.Setter = (b, l) => { };
                gridTimeoutActive.Getter = GetCurrentTimerPB;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(gridTimeoutActive);

                IMyTerminalControlProperty<int> buildState
                    = MyAPIGateway.TerminalControls.CreateProperty<int, IMyProjector>("BuildState");
                buildState.Enabled = IsWorking;
                buildState.Visible = IsValid;
                buildState.SupportsMultipleBlocks = false;
                buildState.Setter = (b, l) => { };
                buildState.Getter = GetStatePB;
                MyAPIGateway.TerminalControls.AddControl<IMyProjector>(gridTimeoutActive);

                MyLog.Default.WriteLineAndConsole("Initialized Instant Projector.");
                controls = true;
            }

        }

        private void ClearCachedComps(SerializableDefinitionId? id)
        {
            cachedComps = null;
        }

        private void ClearCachedComps(float cost)
        {
            cachedComps = null;
        }

        private void ClearCachedComps(bool subgrids)
        {
            cachedComps = null;
        }

        private static bool GetLooseArea(IMyTerminalBlock block)
        {
            return block.GameLogic.GetAs<InstantProjector>().LooseArea;
        }

        private static void SetLooseArea(IMyTerminalBlock block, bool value)
        {
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            gl.LooseArea = value;
            if (Constants.IsServer)
                gl.SaveStorage();
        }

        // Context: All
        private void RefreshUI()
        {
            this.me.RefreshCustomInfo();

            MyCubeBlock me = (MyCubeBlock)this.me;
            if (me.IDModule == null || Constants.IsDedicated || MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
                return;

            MyOwnershipShareModeEnum shareMode = me.IDModule.ShareMode;
            long ownerId = me.IDModule.Owner;
            
            me.ChangeOwner(ownerId, shareMode != MyOwnershipShareModeEnum.All ? MyOwnershipShareModeEnum.All : MyOwnershipShareModeEnum.Faction);
            me.ChangeOwner(ownerId, shareMode);

        }

        // Context: All
        private float GetCurrentPower()
        {
            if (BuildState == State.Idle)
                return minPower;
            return buildPower;
        }

        private float GetPower()
        {
            return minPower + IPSession.Instance.MapSettings.PowerModifier * (Speed - 1);
        }

        // Context: Server
        private void LoadStorage()
        {
            string base64;
            if(storage.TryGetValue(Constants.Storage, out base64))
            {
                byte[] data = Convert.FromBase64String(base64);
                _settings = MyAPIGateway.Utilities.SerializeFromBinary<SyncableProjectorSettings>(data);
                if (_settings != null)
                {
                    _settings.SetKey(me.EntityId);
                    _settings.Verify();
                    return;
                }
            }

            _settings = new SyncableProjectorSettings(me, 1, true);
            SaveStorage();
        }
        
        // Context: Server
        private void SaveStorage()
        {
            string base64 = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(_settings));
            storage[Constants.Storage] = base64;
        }

        // Context: Terminal
        private static void SetSpeed(IMyTerminalBlock block, float value)
        {
            InstantProjector ip = block.GameLogic.GetAs<InstantProjector>();
            ip.Speed = (float)MathHelper.Clamp(Math.Round(value, 2), 1, 1000);
            ip.RefreshUI();
            if(Constants.IsServer)
                ip.SaveStorage();
        }

        // Context: Terminal
        private static float GetSpeed(IMyTerminalBlock block)
        {
            return block.GameLogic.GetAs<InstantProjector>().Speed;
        }

        // Context: Terminal
        private static void GetSpeedText(IMyTerminalBlock block, StringBuilder sb)
        {
            InstantProjector ip = block.GameLogic.GetAs<InstantProjector>();
            MyValueFormatter.AppendWorkInBestUnit(ip.GetPower(), sb);
            sb.Append(" - ").Append(ip.Speed).Append('x');
        }

        // Context: All
        public override void UpdateBeforeSimulation ()
        {
            int newTimer = --Timer;
            if (newTimer <= 0)
            {
                Timer = 0;
                if (Constants.IsServer)
                {
                    if (pending.Spawn(() => BuildState = State.Idle))
                        BuildState = State.Building;
                    else
                        BuildState = State.Idle;
                }
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            }
            else if (newTimer % 60 == 0)
            {
                if (Constants.IsServer)
                {
                    if (newTimer < 1200) // < 20 seconds, every second
                        pending.Notify(Constants.msgTime + (newTimer / 60) + " seconds.", 1);
                    else if (newTimer % 600 == 0 && newTimer < 3600) // < 60 seconds, every 10 seconds
                        pending.Notify(Constants.msgTime + (newTimer / 60) + " seconds.", 2);
                    else if (newTimer % 3600 == 0) // >= 60 seconds, every minute
                    {
                        int minutes = newTimer / 3600;
                        string msg = Constants.msgTime + minutes + " minute";
                        if (minutes > 1)
                            msg += "s.";
                        else
                            msg += ".";
                        pending.Notify(msg, 10);

                        pending.UpdateBounds();
                        IMyEntity e = pending.GetOverlappingEntity();
                        if (e != null)
                            Constants.Notify(GetOverlapString(false, e), pending.Activator, 10);

                        int needed;
                        string name;
                        if (!pending.HasComponents(out needed, out name))
                            Constants.Notify(GetCompsString(needed, name), pending.Activator, 10);
                    }
                }
                RefreshUI();
            }

        }

        // Context: Terminal
        private static StringBuilder GetTimer (IMyTerminalBlock block)
        {
            StringBuilder sb = new StringBuilder();
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl.BuildState != State.Idle)
            {
                AppendTime(sb, gl.Timer);
                sb.Append(" (Active)");
            }
            else
            {
                IMyCubeGrid grid = gl.me.ProjectedGrid;
                if (grid != null)
                    AppendTime(sb, gl.GetBlueprintTimer());
            }
            return sb;
        }

        private int GetBlueprintTimer()
        {
            if (me.ProjectedGrid == null)
                return 0;

            GridComponents comps = GetComponents();
            return GetBlueprintTimer(comps.BlockCount);
        }

        // Context: All
        private int GetBlueprintTimer (int blockCount)
        {
            float speed = 1 / Speed;
            return (int)Math.Max(Math.Round(blockCount * IPSession.Instance.MapSettings.BlockBuildTime * 60 * speed), 60);
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
            if (!m)
                sb.Append('s');
        }

        // Context: Terminal
        private static void GetItemList (IMyTerminalBlock block, List<MyTerminalControlListBoxItem> items, List<MyTerminalControlListBoxItem> selected)
        {
            IMyProjector me = (IMyProjector)block;
            if (me.ProjectedGrid != null)
            {
                StringBuilder sb = new StringBuilder();
                GridComponents comps = block.GameLogic.GetAs<InstantProjector>().GetComponents();
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
            {
                GridComponents comps = block.GameLogic.GetAs<InstantProjector>().GetComponents();
                return comps.ToDictionary(kv => new MyItemType(kv.Key.TypeId, kv.Key.SubtypeId), kv => kv.Value);
            }
            return new Dictionary<MyItemType, int>();
        }

        // Context: All
        private GridComponents GetComponents()
        {
            if(cachedComps == null || cachedCompId != me.ProjectedGrid.EntityId)
            {
                cachedCompId = me.ProjectedGrid.EntityId;
                if (SupportsSubgrids(me))
                    cachedComps = new GridComponents(me);
                else
                    cachedComps = new GridComponents(me.ProjectedGrid);

                cachedComps.ApplySettings(IPSession.Instance.MapSettings);
            }
            return cachedComps;
        }

        // Context: Terminal
        private static int GetMaxTimerPB(IMyTerminalBlock block)
        {
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl == null)
                return 0;
            return gl.GetBlueprintTimer();
        }

        // Context: Terminal
        private static int GetCurrentTimerPB(IMyTerminalBlock block)
        {
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl == null || gl.BuildState != State.Waiting)
                return 0;
            return gl.Timer;
        }

        // Context: Terminal
        private static int GetStatePB(IMyTerminalBlock block)
        {
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            return (int)gl.BuildState;
        }

        // Context: Terminal
        private void CancelClient(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, true, true).SendToServer();
        }

        // Context: Terminal
        private static void CancelClientUnsafe(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, false, true).SendToServer();
        }

        // Context: Server
        public void CancelServer(ulong activator, bool trustSender)
        {
            if (!trustSender && MyAPIGateway.Session.Player?.SteamUserId == activator)
                activator = 0;
            Cancel(activator);
        }

        // Context: Server
        public void Cancel(ulong activator = 0)
        {
            if (BuildState == State.Waiting)
            {
                if(pending.Activator != activator)
                    pending.Notify(Constants.msgCanceled);
                Constants.Notify(Constants.msgCanceled, activator);
                Timer = 0;
                BuildState = State.Idle;
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            }
        }

        // Context: Terminal
        private static void BuildClient (IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, true, false).SendToServer();
        }

        // Context: Terminal
        private static void BuildClientUnsafe (IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, false, false).SendToServer();
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
            else if (BuildState == State.Idle)
                InstantSpawn(activator);
        }

        // Context: Server
        private void InstantSpawn (ulong activator)
        {
            ProjectedGrid grid;
            if (ProjectedGrid.TryCreate(activator, me, _settings.LooseArea, out grid))
            {
                Timer = GetBlueprintTimer(grid.BlockCount);
                pending = grid;
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
                buildPower = GetPower();
                BuildState = State.Waiting;
                StringBuilder sb = new StringBuilder(Constants.msgTime);
                AppendTime(sb, Timer);
                Constants.Notify(sb.ToString(), grid.Activator);
            }
            
        }

        // Context: Server
        private void Me_IsWorkingChanged(IMyCubeBlock block)
        {
            if (!block.IsWorking)
                Cancel();
        }

        // Context: Terminal
        private static bool IsWorking (IMyTerminalBlock block)
        {
            return IsValid(block) && block.IsWorking;
        }

        // Context: Terminal
        public static bool IsValid (IMyTerminalBlock block)
        {
            return block.CubeGrid?.Physics != null && block.GameLogic.GetAs<InstantProjector>() != null;
        }

        public static bool SupportsSubgrids(IMyProjector p)
        {
            return IPSession.Instance.MapSettings.Subgrids && p.BlockDefinition.SubtypeId == "LargeBlockConsole";
        }

        public static string GetOverlapString(bool error, IMyEntity e)
        {
            string type = IPSession.Instance.GetOrComputeReadable(e.GetType().Name);

            StringBuilder sb = new StringBuilder();
            if (string.IsNullOrWhiteSpace(type))
                sb.Append("An entity ");
            else
                sb.Append(type).Append(' ');

            string name = null;
            IMyCubeGrid grid = e as IMyCubeGrid;
            if (grid != null)
            {
                name = grid.CustomName;
            }
            else
            {
                IMyCharacter ch = e as IMyCharacter;
                if (ch != null && ch.IsPlayer)
                {
                    IMyPlayer p = MyAPIGateway.Players.GetPlayerControllingEntity(ch);
                    if (p != null)
                        name = p.DisplayName;
                }
            }

            if(name != null)
                sb.Append("named '").Append(name).Append("' ");

            if (error)
                sb.Append("is preventing the projection from spawning.");
            else
                sb.Append("may interfere with spawning the projection.");

            return sb.ToString();
        }

        public static string GetCompsString(int neededCount, string neededName)
        {
            string name = IPSession.Instance.GetOrComputeReadable(neededName);
            return neededCount + " " + neededName + Constants.msgMissingComp;
        }
    }
}
