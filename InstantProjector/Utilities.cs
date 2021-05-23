using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace avaness.GridSpawner
{
    public static class Utilities
    {
        public static void AppendTime(StringBuilder sb, int ticks)
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

        public static string GetCompsString(int neededCount, MyDefinitionId compId)
        {
            return neededCount + " " + IPSession.Instance.GetComponentName(compId) + Constants.msgMissingComp;
        }

        public static bool SupportsSubgrids(IMyProjector p)
        {
            return IPSession.Instance.MapSettings.Subgrids && p.BlockDefinition.SubtypeId == "LargeBlockConsole";
        }

        public static string GetOverlapString(bool error, IMyEntity e)
        {
            string type = IPSession.Instance.MakeReadable(e.GetType().Name);

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

            if (name != null)
                sb.Append("named '").Append(name).Append("' ");

            if (error)
                sb.Append("is preventing the projection from spawning.");
            else
                sb.Append("may interfere with spawning the projection.");

            return sb.ToString();
        }

        public static void RefreshUI(IMyTerminalBlock block)
        {
            //block.RefreshCustomInfo();

            MyCubeBlock cube = (MyCubeBlock)block;
            if (cube.IDModule == null || Constants.IsDedicated || MyAPIGateway.Gui.GetCurrentScreen != MyTerminalPageEnum.ControlPanel)
                return;

            MyOwnershipShareModeEnum shareMode = cube.IDModule.ShareMode;
            long ownerId = cube.IDModule.Owner;

            cube.ChangeOwner(ownerId, shareMode != MyOwnershipShareModeEnum.All ? MyOwnershipShareModeEnum.All : MyOwnershipShareModeEnum.Faction);
            cube.ChangeOwner(ownerId, shareMode);

        }


        public static void Notify(string msg, ulong steamId, int seconds = 5)
        {
            if (steamId != 0)
            {
                long id2 = MyAPIGateway.Players.TryGetIdentityId(steamId);
                if (id2 != 0)
                    MyVisualScriptLogicProvider.ShowNotification(msg, seconds * 1000, "White", id2);
            }
        }


        public static bool RandomEntityId(Random rand, out long id)
        {
            byte[] bytes = new byte[8];
            for (int i = 0; i < 100; i++)
            {
                rand.NextBytes(bytes);
                id = BitConverter.ToInt64(bytes, 0);
                if (!MyAPIGateway.Entities.EntityExists(id))
                    return true;
            }
            id = 0;
            return false;
        }

        public static MatrixD LocalToWorld(MatrixD local, MatrixD reference)
        {
            return local * reference;
        }

        public static MatrixD WorldToLocalNI(MatrixD world, MatrixD referenceInverted)
        {
            return world * referenceInverted;
        }

        public static MatrixD WorldToLocal(MatrixD world, MatrixD referenceInverted)
        {
            return world * MatrixD.Normalize(MatrixD.Invert(referenceInverted));
        }

        /*public static Quaternion LocalToWorld(Quaternion local, Quaternion reference)
        {
            return reference * local;
        }

        public static Quaternion WorldToLocal(Quaternion world, Quaternion reference)
        {
            return Quaternion.Inverse(reference) * world;
        }

        public static Quaternion WorldToLocalNI(Quaternion world, Quaternion referenceInverted)
        {
            return referenceInverted * world;
        }*/
    }
}
