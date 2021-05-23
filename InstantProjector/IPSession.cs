using avaness.GridSpawner.Networking;
using avaness.GridSpawner.Settings;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace avaness.GridSpawner
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class IPSession : MySessionComponentBase
    {
        public static IPSession Instance;

        public int Runtime; // Server only
        public Network Net { get; private set; }
        public Dictionary<long, Syncable> Syncable = new Dictionary<long, Syncable>();

        public MapSettings MapSettings { get; } = new MapSettings();

        private bool init = false;
        private SettingsHud hud;
        private SettingsChat chat;
        private readonly Dictionary<string, string> physicalItemNames = new Dictionary<string, string>();

        public string GetComponentName(MyDefinitionId id)
        {
            string name = id.SubtypeName;
            if (string.IsNullOrWhiteSpace(name))
                return "Null";

            string result;
            if (physicalItemNames.TryGetValue(name, out result))
                return result;

            return MakeReadable(name);
        }

        public string MakeReadable(string typename)
        {
            if (string.IsNullOrWhiteSpace(typename))
                return "Null";

            StringBuilder sb = new StringBuilder(typename.Length);

            int i;
            if (typename.StartsWith("My"))
                i = 2;
            else
                i = 0;

            int len = typename.Length;
            if (typename.EndsWith("Base"))
                len -= 4;

            bool underscore = false;
            int wordLen = 0;
            for (; i < len; i++)
            {
                char ch = typename[i];
                if (ch == '_')
                {
                    underscore = true;
                    if (sb.Length > 0)
                        sb.Append(' ');
                    wordLen = 0;
                }
                else
                {
                    if (char.IsUpper(ch))
                    {
                        if (wordLen > 1)
                        {
                            if (sb.Length > 0)
                                sb.Append(' ');
                            wordLen = 0;
                        }
                    }
                    else
                    {
                        if (underscore)
                            ch = char.ToUpperInvariant(ch);
                    }
                    sb.Append(ch);
                    wordLen++;
                    underscore = false;
                }
            }

            string s = sb.ToString();
            sb.Clear();
            return s;
        }

        public IPSession()
        {
            Instance = this;
        }

        public override void SaveData()
        {
            if (init)
            {
                 if(Constants.IsServer)
                    MapSettings.Save();
            }
        }

        public override void BeforeStart ()
        {
            Instance = this;
            Net = new Network();
        }

        private void Start()
        {
            MyObjectBuilderType obType = MyObjectBuilderType.Parse("MyObjectBuilder_Component");
            foreach (var def in MyDefinitionManager.Static.GetPhysicalItemDefinitions())
            {
                string name = def.DisplayNameText;
                if (!string.IsNullOrEmpty(name) && def.Id.TypeId == obType)
                    physicalItemNames[def.Id.SubtypeName] = name;
            }


            if (Constants.IsServer)
            {
                Net.AddFactory(new PacketBuild());
                MapSettings.Copy(MapSettings.Load());
                Net.AddFactory(new PacketSettingsRequest());
            }
            else
            {
                Net.AddFactory(new MapSettings());
                new PacketSettingsRequest().SendToServer();
            }
            if (MyAPIGateway.Session.Player != null)
                chat = new SettingsChat();
            hud = new SettingsHud();
            Net.AddFactory(new MapSettings.ValuePacket());
            Net.AddFactory(new SyncableProjectorState());
            Net.AddFactory(new SyncableProjectorSettings());
            MyAPIGateway.TerminalControls.CustomActionGetter += RemoveVanillaSpawnAction;
            MyLog.Default.WriteLineAndConsole("Instant Projector initialized.");
            init = true;
        }

        protected override void UnloadData ()
        {
            chat?.Unload();
            hud?.Unload();
            Net?.Unload();
            MyAPIGateway.TerminalControls.CustomActionGetter -= RemoveVanillaSpawnAction;
            foreach (Syncable s in Syncable.Values.ToArray())
                s.Close();
            Instance = null;
        }

        public override void UpdateAfterSimulation ()
        {
            Runtime++;
            if (MyAPIGateway.Session == null)
                return;

            if(!init)
            {
                if (Constants.IsClient && MyAPIGateway.Session.Player == null)
                    return;
                Start();
            }
        }

        private void RemoveVanillaSpawnAction(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if(block is IMyProjector && block.GameLogic.GetAs<InstantProjector>() != null)
            {
                for(int i = actions.Count - 1; i >= 0; i--)
                {
                    IMyTerminalAction a = actions[i];
                    if(a.Id == "SpawnProjection")
                    {
                        actions.RemoveAt(i);
                        return;
                    }
                }
            }
        }

    }
}
