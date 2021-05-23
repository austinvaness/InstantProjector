using avaness.GridSpawner.Grids;
using avaness.GridSpawner.Networking;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace avaness.GridSpawner
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false, "SmallProjector", "LargeProjector", "LargeBlockConsole", "OverlordProjector")]
    public partial class InstantProjector : MyGameLogicComponent
    {

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

        public int Timer
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

        public ProjectorState BuildState
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

        public float Speed
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

        private void RefreshUI()
        {
            Utilities.RefreshUI(me);
        }

        // Context: Client
        private void ReceivedNewState ()
        {
            if (_state.BuildState == ProjectorState.Waiting || _state.BuildState == ProjectorState.Building)
                buildPower = GetPower();

            if(_state.BuildState == ProjectorState.Waiting)
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

            if (BuildState == ProjectorState.Building)
            {
                sb.Append("Building ship...").AppendLine();
            }
            else if (BuildState == ProjectorState.Waiting)
            {
                sb.Append("Ship will be built in ");
                Utilities.AppendTime(sb, Timer);
                sb.AppendLine();
            }
        }

        // Context: All
        public override void UpdateOnceBeforeFrame ()
        {
            if (me.CubeGrid?.Physics == null)
                return;

            _state = new SyncableProjectorState(me, ProjectorState.Idle, 0);

            if (Constants.IsServer)
            {
                LoadStorage();
                _settings.OnValueReceived += SaveStorage;
                BuildState = ProjectorState.Idle;
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

            ProjectorControls.Create();
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

        // Context: All
        private float GetCurrentPower()
        {
            if (BuildState == ProjectorState.Idle)
                return minPower;
            return buildPower;
        }

        public float GetPower()
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
        public void SaveStorage()
        {
            string base64 = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(_settings));
            storage[Constants.Storage] = base64;
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
                    if (pending.Spawn(() => BuildState = ProjectorState.Idle))
                        BuildState = ProjectorState.Building;
                    else
                        BuildState = ProjectorState.Idle;
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
                            Utilities.Notify(Utilities.GetOverlapString(false, e), pending.Activator, 10);

                        int needed;
                        MyDefinitionId neededId;
                        if (!pending.HasComponents(out needed, out neededId))
                            Utilities.Notify(Utilities.GetCompsString(needed, neededId), pending.Activator, 10);
                    }
                }
                RefreshUI();
            }

        }

        public int GetBlueprintTimer()
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

        // Context: All
        public GridComponents GetComponents()
        {
            if(cachedComps == null || cachedCompId != me.ProjectedGrid.EntityId)
            {
                cachedCompId = me.ProjectedGrid.EntityId;
                if (Utilities.SupportsSubgrids(me))
                    cachedComps = new GridComponents(me);
                else
                    cachedComps = new GridComponents(me.ProjectedGrid);

                cachedComps.ApplySettings(IPSession.Instance.MapSettings);
            }
            return cachedComps;
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
            if (BuildState == ProjectorState.Waiting)
            {
                if(pending.Activator != activator)
                    pending.Notify(Constants.msgCanceled);
                Utilities.Notify(Constants.msgCanceled, activator);
                Timer = 0;
                BuildState = ProjectorState.Idle;
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            }
        }

        // Context: Server
        public void BuildServer (ulong activator, bool trustSender)
        {
            if (!trustSender && MyAPIGateway.Session.Player?.SteamUserId == activator)
                activator = 0;
            if (me.ProjectedGrid == null)
                Utilities.Notify(Constants.msgNoGrid, activator);
            else if (BuildState == ProjectorState.Building)
                Utilities.Notify(Constants.msgBuilding, activator);
            else if (BuildState == ProjectorState.Waiting)
                Utilities.Notify(Constants.msgWaiting, activator);
            else if (BuildState == ProjectorState.Idle)
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
                BuildState = ProjectorState.Waiting;
                StringBuilder sb = new StringBuilder(Constants.msgTime);
                Utilities.AppendTime(sb, Timer);
                Utilities.Notify(sb.ToString(), grid.Activator);
            }
            
        }

        // Context: Server
        private void Me_IsWorkingChanged(IMyCubeBlock block)
        {
            if (!block.IsWorking)
                Cancel();
        }
    }
}
