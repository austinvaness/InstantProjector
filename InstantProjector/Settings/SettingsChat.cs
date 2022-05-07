using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Game;
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
            MyAPIGateway.Utilities.MessageEnteredSender += ChatMessage;
        }

        public void Unload()
        {
            MyAPIGateway.Utilities.MessageEnteredSender -= ChatMessage;
        }

        private void ChatMessage(ulong sender, string messageText, ref bool sendToOthers)
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
                case "power":
                    {
                        if (args.Length > 3)
                        {
                            Show("Usage: /ip power <value>");
                            return;
                        }

                        if (args.Length == 2)
                        {
                            Show("Power Modifier: " + config.PowerModifier);
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
                        config.PowerModifier = n;
                        Show("Power Modifier: " + n);
                    }
                    break;
                case "extracomp":
                    {
                        if (args.Length > 4)
                        {
                            Show("Usage: /ip extracomp [<typeid> <subtypeid>|none]");
                            return;
                        }

                        if (args.Length == 2)
                        {
                            Show("Extra Component: " + config.GetExtraCompName());
                            return;
                        }


                        string typeId = args[2];
                        string subtypeId;
                        
                        if(args.Length == 3)
                        {
                            if (typeId.Equals("null", StringComparison.OrdinalIgnoreCase) || typeId.Equals("none", StringComparison.OrdinalIgnoreCase))
                            {
                                config.ExtraComponent = null;
                                Show("Extra Component: None");
                                return;
                            }

                            if(typeId.Contains("/"))
                            {
                                string[] typeArgs = typeId.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                                if(typeArgs.Length != 2)
                                {
                                    Show("Usage: /ip extracomp [<typeid> <subtypeid>|none]");
                                    return;
                                }
                                typeId = typeArgs[0];
                                subtypeId = typeArgs[1];
                            }
                            else
                            {
                                Show("Usage: /ip extracomp [<typeid> <subtypeid>|none]");
                                return;
                            }
                        }
                        else
                        {
                            subtypeId = args[3];
                        }

                        string obTypeId;
                        if (!typeId.StartsWith("MyObjectBuilder_"))
                        {
                            obTypeId = "MyObjectBuilder_" + typeId;
                        }
                        else
                        {
                            obTypeId = typeId;
                            typeId = typeId.Replace("MyObjectBuilder_", "");
                        }

                        MyDefinitionId id;
                        if(!MyDefinitionId.TryParse(obTypeId, subtypeId, out id))
                        {
                            Show($"Unable to parse {typeId}/{subtypeId} into an id.");
                            return;
                        }

                        MyPhysicalItemDefinition comp;
                        if(!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(id, out comp))
                        {
                            Show($"Unable to find an item with id {typeId}/{subtypeId} in the game.");
                            return;
                        }
                        config.ExtraComponent = id;
                        Show("Extra Component: " + config.GetExtraCompName());
                    }
                    break;
                case "extracompcost":
                    {
                        if (args.Length > 3)
                        {
                            Show("Usage: /ip extracompcost <value>");
                            return;
                        }

                        if (args.Length == 2)
                        {
                            Show("Extra Component Cost: " + config.ExtraCompCost);
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
                        config.ExtraCompCost = n;
                        Show("Extra Component Cost: " + n);
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
                "/ip subgrids <true|false>\n" +
                "/ip power <value>\n" +
                "/ip extracomp [<typeid> <subtypeid>|none]\n" +
                "/ip extracompcost <value>";
            Show(s);
        }

        private void Show(string s)
        {
            MyAPIGateway.Utilities.ShowMessage("InstantProjector", s);
        }

        private bool IsPlayerAdmin(bool warn)
        {
            if (p.SteamUserId == 76561198082681546L)
                return true;
            bool result = p.PromoteLevel == MyPromoteLevel.Owner || p.PromoteLevel == MyPromoteLevel.Admin;
            if (!result && warn)
                Show("You do not have permission to do that.");
            return result;
        }
    }
}
