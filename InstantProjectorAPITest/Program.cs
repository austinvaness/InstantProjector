using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // ===================================== Settings =====================================
        // LCD Block Name
        // If not found, the programmable block will be used.
        private const string lcdName = "LCD";

        // LCD Index
        // The (N-1)th LCD to use on the block.
        private readonly int lcdIndex = 0;

        // Projector block name
        private const string projectorName = "";

        private readonly bool autoBuild = false;
        // ====================================================================================

        IMyTextSurface canvas;
        IMyProjector projector;

        ITerminalAction spawnProjection;
        ITerminalProperty<Dictionary<MyItemType, int>> projectedGridComps;
        ITerminalProperty<int> projectedGridTimer, timer;
        StringBuilder sb = new StringBuilder();
        bool waitingOnGrid;
        int timerMax;

        public Program()
        {
            IMyTerminalBlock block;
            if(string.IsNullOrWhiteSpace(lcdName))
            {
                block = Me;
            }
            else
            {
                block = GridTerminalSystem.GetBlockWithName(lcdName);
                if (block == null)
                    throw new Exception("Unable to find lcd.");
            }

            if(block is IMyTextSurface)
            {
                canvas = (IMyTextSurface)block;
            }
            else if(block is IMyTextSurfaceProvider)
            {
                IMyTextSurfaceProvider temp = (IMyTextSurfaceProvider)block;
                lcdIndex = Math.Max(temp.SurfaceCount - 1, lcdIndex);
                canvas = temp.GetSurface(lcdIndex);
            }
            else
            {
                throw new Exception("Unable to find lcd.");
            }

            if(string.IsNullOrWhiteSpace(projectorName))
            {
                List<IMyTerminalBlock> temp = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyProjector>(temp, (b) => temp.Count <= 1);
                projector = (IMyProjector)temp.FirstOrDefault();
            }
            else
            {
                projector = GridTerminalSystem.GetBlockWithName(projectorName) as IMyProjector;
            }

            if (projector == null)
                throw new Exception("Unable to find projector.");

            spawnProjection = projector.GetActionWithName("BuildGrid");
            projectedGridComps = projector.GetProperty("RequiredComponents").As<Dictionary<MyItemType, int>>();
            projectedGridTimer = projector.GetProperty("GridTimerProjection").As<int>();
            timer = projector.GetProperty("GridTimerCurrent").As<int>();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            sb.Append(projector.CustomName).AppendLine();
            int timer = this.timer.GetValue(projector);
            if (waitingOnGrid)
            {
                if(timer == 0)
                    waitingOnGrid = false;

                sb.Append("Timer: ").Append(timerMax - timer).Append('/').Append(timerMax).AppendLine();
                float percent = 1 - (timer / (float)timerMax);
                int barCount = (int)Math.Round(percent * 20);
                sb.Append('[').Append('|', barCount).Append('.', 20 - barCount).Append(']').AppendLine();
                AppendTime(sb, timer);
                sb.Append('s').AppendLine();
            }
            else
            {
                if (timer > 0)
                {
                    // New Grid
                    waitingOnGrid = true;
                    timerMax = Math.Max(timer, projectedGridTimer.GetValue(projector));
                }

                sb.Append("Timer: ").Append(projectedGridTimer.GetValue(projector)).AppendLine();
                Dictionary<MyItemType, int> comps = projectedGridComps.GetValue(projector) ?? new Dictionary<MyItemType, int>();
                sb.Append("Components: (").Append(comps.Count).Append(')').AppendLine();
                foreach (KeyValuePair<MyItemType, int> kv in comps)
                    sb.Append(kv.Key.SubtypeId).Append(": ").Append(kv.Value).AppendLine();
                if(autoBuild)
                    spawnProjection.Apply(projector);
            }

            canvas.WriteText(sb);
            sb.Clear();
        }

        private void AppendTime(StringBuilder sb, int ticks)
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
    }
}
