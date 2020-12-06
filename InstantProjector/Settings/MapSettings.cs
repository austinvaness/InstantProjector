using avaness.GridSpawner.Networking;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Xml.Serialization;
using VRage.Game;
using VRage.ObjectBuilders;

namespace avaness.GridSpawner.Settings
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public partial class MapSettings : Packet
    {
        public override byte TypeId => 2;

        [XmlIgnore]
        public bool SyncData = false;

        public MapSettings()
        {
        }

        public void Unload()
        {
            OnBlockBuildTimeChanged = null;
            OnComponentCostModifierChanged = null;
            OnMinBlocksChanged = null;
            OnMaxBlocksChanged = null;
            OnSubgridsChanged = null;
            OnPowerModifierChanged = null;
            OnExtraComponentChanged = null;
            OnExtraCompCostChanged = null;
        }

        [XmlElement]
        [ProtoMember(1)]
        public float BlockBuildTime
        {
            get
            {
                return blockBuildTime;
            }
            set
            {
                if(value != blockBuildTime)
                {
                    blockBuildTime = value;
                    Sync(new ValuePacket(PacketEnum.BlockBuildTime, value));
                    if (OnBlockBuildTimeChanged != null)
                        OnBlockBuildTimeChanged.Invoke(value);
                }
            }
        }
        private float blockBuildTime = 0.5f;
        public event Action<float> OnBlockBuildTimeChanged;

        [XmlElement]
        [ProtoMember(2)]
        public float ComponentCostModifier
        {
            get
            {
                return componentCostModifier;
            }
            set
            {
                if(value != componentCostModifier)
                {
                    componentCostModifier = value;
                    Sync(new ValuePacket(PacketEnum.ComponentCostModifier, value));
                    if (OnComponentCostModifierChanged != null)
                        OnComponentCostModifierChanged.Invoke(value);
                }
            }
        }
        private float componentCostModifier = 1;
        public event Action<float> OnComponentCostModifierChanged;

        [XmlElement]
        [ProtoMember(3)]
        public int MinBlocks
        {
            get
            {
                return minBlocks;
            }
            set
            {
                if(value != minBlocks)
                {
                    minBlocks = value;
                    Sync(new ValuePacket(PacketEnum.MinBlocks, value));
                    if (OnMinBlocksChanged != null)
                        OnMinBlocksChanged.Invoke(value);
                }
            }
        }
        private int minBlocks = 1;
        public event Action<int> OnMinBlocksChanged;

        [XmlElement]
        [ProtoMember(4)]
        public int MaxBlocks
        {
            get
            {
                return maxBlocks;
            }
            set
            {
                if(value != maxBlocks)
                {
                    maxBlocks = value;
                    Sync(new ValuePacket(PacketEnum.MaxBlocks, value));
                    if (OnMaxBlocksChanged != null)
                        OnMaxBlocksChanged.Invoke(value);
                }
            }
        }
        private int maxBlocks = int.MaxValue;
        public event Action<int> OnMaxBlocksChanged;

        [XmlElement]
        [ProtoMember(5)]
        public bool Subgrids
        {
            get
            {
                return subgrids;
            }
            set
            {
                if(value != subgrids)
                {
                    subgrids = value;
                    Sync(new ValuePacket(PacketEnum.Subgrids, value));
                    if (OnSubgridsChanged != null)
                        OnSubgridsChanged.Invoke(value);
                }
            }
        }
        private bool subgrids = true;
        public event Action<bool> OnSubgridsChanged;

        [XmlElement]
        [ProtoMember(6)]
        public float PowerModifier
        {
            get
            {
                return powerModifier;
            }
            set
            {
                if(value != powerModifier)
                {
                    powerModifier = value;
                    Sync(new ValuePacket(PacketEnum.PowerModifier, value));
                    if (OnPowerModifierChanged != null)
                        OnPowerModifierChanged.Invoke(value);
                }
            }
        }
        private float powerModifier = 6;
        public event Action<float> OnPowerModifierChanged;


        [XmlElement]
        [ProtoMember(7)]
        public float ExtraCompCost
        {
            get
            {
                return extraCompCost;
            }
            set
            {
                if (value != extraCompCost)
                {
                    extraCompCost = value;
                    Sync(new ValuePacket(PacketEnum.ExtraCompCostModifier, value));
                    if (OnExtraCompCostChanged != null)
                        OnExtraCompCostChanged.Invoke(value);
                }
            }
        }
        private float extraCompCost = 1;
        public event Action<float> OnExtraCompCostChanged;

        [XmlElement]
        [ProtoMember(8)]
        public SerializableDefinitionId? ExtraComponent
        {
            get
            {
                return extraComponent;
            }
            set
            {
                if(value.HasValue != extraComponent.HasValue || (value.HasValue && extraComponent.HasValue && (MyDefinitionId)value.Value != extraComponent.Value))
                {
                    extraComponent = value;
                    Sync(new ValuePacket(PacketEnum.ExtraComponent, value));
                    if (OnExtraComponentChanged != null)
                        OnExtraComponentChanged.Invoke(value);
                }
            }
        }
        private SerializableDefinitionId? extraComponent = null;
        public event Action<SerializableDefinitionId?> OnExtraComponentChanged;

        public string GetExtraCompName()
        {
            if (extraComponent.HasValue)
                return extraComponent.Value.ToString().Replace("MyObjectBuilder_", "");
            return "None";
        }

        public static MapSettings Load()
        {
            try
            {
                if (Constants.IsServer && MyAPIGateway.Utilities.FileExistsInWorldStorage(Constants.mapFile, typeof(MapSettings)))
                {
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(Constants.mapFile, typeof(MapSettings));
                    string xmlText = reader.ReadToEnd();
                    reader.Close();
                    MapSettings config = MyAPIGateway.Utilities.SerializeFromXML<MapSettings>(xmlText);
                    if (config == null)
                        throw new NullReferenceException("Failed to serialize from xml.");
                    config.SyncData = true;
                    return config;
                }
            }
            catch
            { }

            MapSettings result = new MapSettings();
            result.Save();
            result.SyncData = true;
            return result;
        }

        public void Save()
        {
            if (Constants.IsServer)
            {
                var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(Constants.mapFile, typeof(MapSettings));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(this));
                writer.Flush();
                writer.Close();
            }
        }


        public void Copy(MapSettings config)
        {
            blockBuildTime = config.blockBuildTime;
            if (OnBlockBuildTimeChanged != null)
                OnBlockBuildTimeChanged.Invoke(blockBuildTime);

            componentCostModifier = config.componentCostModifier;
            if (OnComponentCostModifierChanged != null)
                OnComponentCostModifierChanged.Invoke(componentCostModifier);

            minBlocks = config.minBlocks;
            if (OnMinBlocksChanged != null)
                OnMinBlocksChanged.Invoke(minBlocks);

            maxBlocks = config.maxBlocks;
            if (OnMaxBlocksChanged != null)
                OnMaxBlocksChanged.Invoke(maxBlocks);

            subgrids = config.subgrids;
            if (OnSubgridsChanged != null)
                OnSubgridsChanged.Invoke(subgrids);

            powerModifier = config.powerModifier;
            if (OnPowerModifierChanged != null)
                OnPowerModifierChanged.Invoke(powerModifier);

            extraComponent = config.extraComponent;
            if (OnExtraComponentChanged != null)
                OnExtraComponentChanged.Invoke(extraComponent);

            extraCompCost = config.extraCompCost;
            if (OnExtraCompCostChanged != null)
                OnExtraCompCostChanged.Invoke(extraCompCost);

            SyncData = true;
        }



        private void Sync(ValuePacket p)
        {
            if (!SyncData)
                return;
            if (Constants.IsServer)
                p.SendToOthers();
            else
                p.SendToServer();
        }

        public override byte[] ToBinary()
        {
            return MyAPIGateway.Utilities.SerializeToBinary(this);
        }

        public override void Serialize(byte[] data, ulong sender)
        {
            MyAPIGateway.Utilities.SerializeFromBinary<MapSettings>(data).Received(sender);
        }

        public override void Received(ulong sender)
        {
            IPSession.Instance.MapSettings.Copy(this);
        }

    }
}
