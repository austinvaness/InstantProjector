using avaness.GridSpawner.Grids;
using avaness.GridSpawner.Networking;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace avaness.GridSpawner
{
    public static class ProjectorControls
    {
        private static bool controls = false;

        public static void Create()
        {
            if (controls)
                return;

            IMyTerminalControlSeparator sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyProjector>("BuildGridSep");
            sep.Enabled = IsValid;
            sep.Visible = IsValid;
            sep.SupportsMultipleBlocks = true;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(sep);

            IMyTerminalControlLabel lbl = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyProjector>("BuildGridLabel");
            lbl.Enabled = IsValid;
            lbl.Visible = IsValid;
            lbl.SupportsMultipleBlocks = true;
            lbl.Label = MyStringId.GetOrCompute("Instant Projector Controls");
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(lbl);

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
            aBuild.InvalidToolbarTypes = new[] { MyToolbarType.ButtonPanel }.ToList();
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

            IMyTerminalControlButton itemListInfo = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("ComponentListInfo");
            itemListInfo.Enabled = IsWorking;
            itemListInfo.Visible = IsValid;
            itemListInfo.SupportsMultipleBlocks = false;
            itemListInfo.Title = MyStringId.GetOrCompute("Check Inventory");
            itemListInfo.Action = OpenItemList;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(itemListInfo);


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

        private static void OpenItemList(IMyTerminalBlock block)
        {
            IMyProjector p = (IMyProjector)block;
            if (p.ProjectedGrid == null)
                return;
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            gl.GetComponents().ShowScreen(Utilities.GetInventories(block));
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

        private static void SetSpeed(IMyTerminalBlock block, float value)
        {
            InstantProjector ip = block.GameLogic.GetAs<InstantProjector>();
            ip.Speed = (float)MathHelper.Clamp(Math.Round(value, 2), 1, 1000);
            Utilities.RefreshUI(block);
            if (Constants.IsServer)
                ip.SaveStorage();
        }

        private static float GetSpeed(IMyTerminalBlock block)
        {
            return block.GameLogic.GetAs<InstantProjector>().Speed;
        }

        private static void GetSpeedText(IMyTerminalBlock block, StringBuilder sb)
        {
            InstantProjector ip = block.GameLogic.GetAs<InstantProjector>();
            MyValueFormatter.AppendWorkInBestUnit(ip.GetPower(), sb);
            sb.Append(" - ").Append(ip.Speed).Append('x');
        }

        private static StringBuilder GetTimer(IMyTerminalBlock block)
        {
            IMyProjector p = (IMyProjector)block;
            StringBuilder sb = new StringBuilder();
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl.BuildState != ProjectorState.Idle)
            {
                Utilities.AppendTime(sb, gl.Timer);
                sb.Append(" (Active)");
            }
            else
            {
                if (p.ProjectedGrid != null)
                    Utilities.AppendTime(sb, gl.GetBlueprintTimer());
            }
            return sb;
        }

        private static void GetItemList(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> items, List<MyTerminalControlListBoxItem> selected)
        {
            IMyProjector me = (IMyProjector)block;
            if (me.ProjectedGrid != null)
            {
                StringBuilder sb = new StringBuilder();
                GridComponents comps = block.GameLogic.GetAs<InstantProjector>().GetComponents();
                foreach (KeyValuePair<MyDefinitionId, int> kv in comps)
                {
                    if (IPSession.Instance != null)
                        sb.Append(IPSession.Instance.GetComponentName(kv.Key));
                    else
                        sb.Append(kv.Key.SubtypeName);
                    sb.Append(": ").Append(kv.Value);
                    MyStringId s = MyStringId.GetOrCompute(sb.ToString());
                    MyTerminalControlListBoxItem item = new MyTerminalControlListBoxItem(s, s, 0);
                    items.Add(item);
                    sb.Clear();
                }
            }
        }

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

        private static int GetMaxTimerPB(IMyTerminalBlock block)
        {
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl == null)
                return 0;
            return gl.GetBlueprintTimer();
        }

        private static int GetCurrentTimerPB(IMyTerminalBlock block)
        {
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            if (gl == null || gl.BuildState != ProjectorState.Waiting)
                return 0;
            return gl.Timer;
        }

        private static int GetStatePB(IMyTerminalBlock block)
        {
            InstantProjector gl = block.GameLogic.GetAs<InstantProjector>();
            return (int)gl.BuildState;
        }

        private static void CancelClient(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, true, true).SendToServer();
        }

        private static void CancelClientUnsafe(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, false, true).SendToServer();
        }

        private static void BuildClient(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            IMyProjector p = (IMyProjector)block;

            List<MyObjectBuilder_CubeGrid> grids = null;
            if (Utilities.SupportsSubgrids(p))
            {
                MyObjectBuilder_Projector pBuilder = (MyObjectBuilder_Projector)p.GetObjectBuilderCubeBlock(true);
                if (pBuilder != null && pBuilder.ProjectedGrids.Count > 1)
                    grids = pBuilder.ProjectedGrids;
            }

            new PacketBuild(block, true, false, grids).SendToServer();
        }

        private static void BuildClientUnsafe(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, false, false).SendToServer();
        }

        private static bool IsWorking(IMyTerminalBlock block)
        {
            return IsValid(block) && block.IsWorking;
        }

        public static bool IsValid(IMyTerminalBlock block)
        {
            return block.CubeGrid?.Physics != null && block.GameLogic.GetAs<InstantProjector>() != null;
        }

    }
}
