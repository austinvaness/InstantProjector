using avaness.GridSpawner.Networking;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using VRage.ObjectBuilders;

namespace avaness.GridSpawner.Settings
{
    public partial class MapSettings
    {
        [ProtoContract]
        public class ValuePacket : Packet
        {
            public override byte TypeId => 3;

            [ProtoMember(1)]
            private readonly byte type;
            [ProtoMember(2)]
            private readonly byte[] value;

            public ValuePacket()
            {

            }

            public ValuePacket(PacketEnum type, SerializableDefinitionId? value)
            {
                this.type = (byte)type;
                if(!value.HasValue)
                    this.value = new byte[0];
                else
                    this.value = MyAPIGateway.Utilities.SerializeToBinary(value.Value);
            }

            public ValuePacket(PacketEnum type, int value)
            {
                this.type = (byte)type;
                this.value = BitConverter.GetBytes(value);
            }

            public ValuePacket(PacketEnum type, float value)
            {
                this.type = (byte)type;
                this.value = BitConverter.GetBytes(value);
            }

            public ValuePacket(PacketEnum type, bool value)
            {
                this.type = (byte)type;
                if (value)
                    this.value = new byte[1] { 1 };
                else
                    this.value = new byte[1] { 0 };
            }

            public ValuePacket(PacketEnum type, byte value)
            {
                this.type = (byte)type;
                this.value = new byte[1] { value };
            }

            public override void Received(ulong sender)
            {
                MapSettings config = IPSession.Instance.MapSettings;
                switch ((PacketEnum)type)
                {
                    case PacketEnum.BlockBuildTime:
                        {
                            float num = BitConverter.ToSingle(value, 0);
                            config.blockBuildTime = num;
                            if (config.OnBlockBuildTimeChanged != null)
                                config.OnBlockBuildTimeChanged.Invoke(num);
                        }
                        break;
                    case PacketEnum.ComponentCostModifier:
                        {
                            float num = BitConverter.ToSingle(value, 0);
                            config.componentCostModifier = num;
                            if (config.OnComponentCostModifierChanged != null)
                                config.OnComponentCostModifierChanged.Invoke(num);
                        }
                        break;
                    case PacketEnum.MinBlocks:
                        {
                            int num = BitConverter.ToInt32(value, 0);
                            config.minBlocks = num;
                            if (config.OnMinBlocksChanged != null)
                                config.OnMinBlocksChanged.Invoke(num);
                        }
                        break;
                    case PacketEnum.MaxBlocks:
                        {
                            int num = BitConverter.ToInt32(value, 0);
                            config.maxBlocks = num;
                            if (config.OnMaxBlocksChanged != null)
                                config.OnMaxBlocksChanged.Invoke(num);
                        }
                        break;
                    case PacketEnum.Subgrids:
                        {
                            bool b = value[0] == 1;
                            config.subgrids = b;
                            if (config.OnSubgridsChanged != null)
                                config.OnSubgridsChanged.Invoke(b);
                        }
                        break;
                    case PacketEnum.PowerModifier:
                        {
                            float num = BitConverter.ToSingle(value, 0);
                            config.powerModifier = num;
                            if (config.OnPowerModifierChanged != null)
                                config.OnPowerModifierChanged.Invoke(num);
                        }
                        break;
                    case PacketEnum.ExtraCompCostModifier:
                        {
                            float num = BitConverter.ToSingle(value, 0);
                            config.extraCompCost = num;
                            if (config.OnExtraCompCostChanged != null)
                                config.OnExtraCompCostChanged.Invoke(num);
                        }
                        break;
                    case PacketEnum.ExtraComponent:
                        {
                            SerializableDefinitionId? id = null;
                            if (value.Length > 0)
                                id = MyAPIGateway.Utilities.SerializeFromBinary<SerializableDefinitionId>(value);

                            config.extraComponent = id;
                            if (config.OnExtraComponentChanged != null)
                                config.OnExtraComponentChanged.Invoke(id);
                        }
                        break;
                }
            }

            public override void Serialize(byte[] data, ulong sender)
            {
                MyAPIGateway.Utilities.SerializeFromBinary<ValuePacket>(data).Received(sender);

            }

            public override byte[] ToBinary()
            {
                return MyAPIGateway.Utilities.SerializeToBinary(this);
            }

            public override string ToString()
            {
                return ((PacketEnum)type).ToString();
            }
        }

    }
}
