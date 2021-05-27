using avaness.GridSpawner.Grids;
using avaness.GridSpawner.Networking;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.ModAPI;
using VRage.Utils;

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
            btnBuild.Enabled = IsWorkingAdmin;
            btnBuild.Visible = IsAdmin;
            btnBuild.SupportsMultipleBlocks = true;
            btnBuild.Title = MyStringId.GetOrCompute("Build Grid");
            btnBuild.Action = BuildClient;
            btnBuild.Tooltip = MyStringId.GetOrCompute("Builds the projection instantly.\nThere will be a cooldown after building.");
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(btnBuild);

            IMyTerminalControlButton btnCancel = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("CancelBuildGrid");
            btnCancel.Enabled = IsWorkingAdmin;
            btnCancel.Visible = IsAdmin;
            btnCancel.SupportsMultipleBlocks = true;
            btnCancel.Title = MyStringId.GetOrCompute("Cancel");
            btnCancel.Action = CancelClient;
            btnCancel.Tooltip = MyStringId.GetOrCompute("Cancels the build process.");
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(btnCancel);

            IMyTerminalControlCheckbox chkShift = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyProjector>("MoveProjectionArea");
            chkShift.Enabled = IsWorkingAdmin;
            chkShift.Visible = IsAdmin;
            chkShift.SupportsMultipleBlocks = true;
            chkShift.Title = MyStringId.GetOrCompute("Loose Projection Area");
            chkShift.OnText = MyStringId.GetOrCompute("On");
            chkShift.OffText = MyStringId.GetOrCompute("Off");
            chkShift.Tooltip = MyStringId.GetOrCompute("Allow the projection to spawn in a different area if the original area is occupied.");
            chkShift.Setter = SetLooseArea;
            chkShift.Getter = GetLooseArea;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(chkShift);

            IMyTerminalControlTextbox txtTimeout = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyProjector>("GridTimer");
            txtTimeout.Enabled = (b) => false;
            txtTimeout.Visible = IsAdmin;
            txtTimeout.Getter = GetTimer;
            txtTimeout.Setter = (b, s) => { };
            txtTimeout.SupportsMultipleBlocks = false;
            txtTimeout.Title = MyStringId.GetOrCompute("Build Timer");
            txtTimeout.Tooltip = MyStringId.GetOrCompute("The amount of time you must wait after building a grid to be able to build another.");
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(txtTimeout);

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

        private static void CancelClient(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, true, true).SendToServer();
        }

        private static void BuildClient(IMyTerminalBlock block)
        {
            if (MyAPIGateway.Session == null || !block.IsWorking)
                return;

            new PacketBuild(block, true, false).SendToServer();
        }

        private static bool IsWorking(IMyTerminalBlock block)
        {
            return IsValid(block) && block.IsWorking;
        }

        private static bool IsWorkingAdmin(IMyTerminalBlock block)
        {
            return IsAdmin(block) && block.IsWorking;
        }

        public static bool IsValid(IMyTerminalBlock block)
        {
            return block.CubeGrid?.Physics != null && block.GameLogic.GetAs<InstantProjector>() != null;
        }

        private static bool IsAdmin(IMyTerminalBlock block)
        {
            return IsValid(block) && block.BlockDefinition.SubtypeName == "OverlordProjector";
        }
    }
}
