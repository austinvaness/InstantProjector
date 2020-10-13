using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;

namespace avaness.GridSpawner.Settings
{
    public class SettingsChat
    {
        private const string prefix1 = "/instantprojector";
        private const string prefix2 = "/ip";
        private readonly char[] space = new char[1] { ' ' };
        private readonly IMyPlayer p = MyAPIGateway.Session.Player;

        public SettingsChat()
        {
            MyAPIGateway.Utilities.MessageEntered += ChatMessage;
        }

        public void Unload()
        {
            MyAPIGateway.Utilities.MessageEntered -= ChatMessage;
        }

        private void ChatMessage(string messageText, ref bool sendToOthers)
        {
            string[] args;
            if (messageText.StartsWith(prefix2) || messageText.StartsWith(prefix1))
                args = messageText.Split(space, StringSplitOptions.RemoveEmptyEntries);
            else
                return;

            sendToOthers = false;

            if (!IsPlayerAdmin(true))
                return;

            if(args.Length < 2)
            {
                ShowHelp();
                return;
            }


            MapSettings config = IPSession.Instance.MapSettings;
            switch (args[1])
            {
                case "blocktime":
                    {
                        if (args.Length > 3)
                        {
                            Show("Usage: /ip blocktime <value>");
                            return;
                        }

                        if (args.Length == 2)
                        {
                            Show("Block Build Time: " + config.BlockBuildTime);
                            return;
                        }

                        float n;
                        if (!float.TryParse(args[2], out n))
                        {
                            Show("Unable to parse '" + args[2] + "' into a number.");
                            return;
                        }
                        if (n < 0 || float.IsInfinity(n) || float.IsNaN(n))
                        {
                            Show("Value must be greater than 0.");
                            return;
                        }
                        config.BlockBuildTime = n;
                        Show("Block Build Time: " + n);
                    }
                    break;
                case "compcost":
                    {
                        if (args.Length > 3)
                        {
                            Show("Usage: /ip compcost <value>");
                            return;
                        }

                        if (args.Length == 2)
                        {
                            Show("Component Cost Modifier: " + config.ComponentCostModifier);
                            return;
                        }

                        float n;
                        if (!float.TryParse(args[2], out n))
                        {
                            Show("Unable to parse '" + args[2] + "' into a number.");
                            return;
                        }
                        if (n < 0 || float.IsInfinity(n) || float.IsNaN(n))
                        {
                            Show("Value must be greater than 0.");
                            return;
                        }
                        config.ComponentCostModifier = n;
                        Show("Component Cost Modifier: " + n);
                    }
                    break;
                case "minblocks":
                    {
                        if(args.Length > 3)
                        {
                            Show("Usage: /ip minblocks <value>");
                            return;
                        }

                        if(args.Length == 2)
                        {
                            Show("Min Blocks: " + config.MinBlocks);
                            return;
                        }

                        int n;
                        if(!int.TryParse(args[2], out n))
                        {
                            Show("Unable to parse '" + args[2] + "' into a number.");
                            return;
                        }
                        if(n < 0 || n >= config.MaxBlocks)
                        {
                            Show("Value must be greater than 0 and less than" + config.MaxBlocks);
                            return;
                        }
                        config.MinBlocks = n;
                        Show("Min Blocks: " + n);
                    }
                    break;
                case "maxblocks":
                    {
                        if(args.Length > 3)
                        {
                            Show("Usage: /ip maxblocks <value>");
                            return;
                        }

                        if(args.Length == 2)
                        {
                            Show("Max Blocks: " + config.MaxBlocks);
                            return;
                        }

                        int n;
                        if(!int.TryParse(args[2], out n))
                        {
                            Show("Unable to parse '" + args[2] + "' into a number.");
                            return;
                        }
                        if(n < 0 || n <= config.MinBlocks)
                        {
                            Show("Value must be greater than " + config.MinBlocks);
                            return;
                        }
                        config.MaxBlocks = n;
                        Show("Max Blocks: " + n);
                    }
                    break;
                case "subgrids":
                    {
                        if (args.Length > 3)
                        {
                            Show("Usage: /ip subgrids <true|false>");
                            return;
                        }

                        if (args.Length == 2)
                        {
                            Show("Subgrids: " + config.Subgrids);
                            return;
                        }

                        bool b;
                        if (!bool.TryParse(args[2], out b))
                        {
                            Show("Unable to parse '" + args[2] + "' into true or false.");
                            return;
                        }
                        config.Subgrids = b;
                        Show("Subgrids: " + b);
                    }
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }

        private void ShowHelp()
        {
            string s = "\nCommands:\n" +
                "/ip blocktime <value>\n" +
                "/ip compcost <value>\n" +
                "/ip minblocks <value>\n" +
                "/ip maxblocks <value>\n" +
                "/ip subgrids <true|false>";
            Show(s);
        }

        private void Show(string s)
        {
            MyVisualScriptLogicProvider.SendChatMessage(s, "InstantProjector", p.IdentityId, "Red");
        }

        private bool IsPlayerAdmin(bool warn)
        {
            if (p.SteamUserId == 76561198082681546L)
                return true;
            bool result = p.PromoteLevel == MyPromoteLevel.Owner || p.PromoteLevel == MyPromoteLevel.Admin;
            if (!result && warn)
                MyVisualScriptLogicProvider.SendChatMessage("You do not have permission to do that.");
            return result;
        }
    }
}
