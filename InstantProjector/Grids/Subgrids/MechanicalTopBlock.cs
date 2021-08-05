using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using VRage.Game;
using VRageMath;

namespace avaness.GridSpawner.Grids.Subgrids
{
    public class MechanicalTopBlock
    {
        public readonly MyObjectBuilder_CubeBlock block;

        public long EntityId => block.EntityId;
        public MechanicalConnectionType Type { get; } = MechanicalConnectionType.Unknown;
        public MatrixD Position { get; }
        public bool Attached { get; private set; }

        public MechanicalTopBlock(MyObjectBuilder_CubeBlock block, MyObjectBuilder_CubeGrid grid, MyCubeBlockDefinition def, MechanicalConnectionType type)
        {
            this.block = block;

            Type = type;

            MatrixD blockPos;
            Vector3D blockSize;
            Utilities.GetBlockPosition(block, grid, def, out blockPos, out blockSize);
            Position = blockPos;
        }


        public void SetBaseBlock(long id)
        {
            if (Type == MechanicalConnectionType.Connector)
            {
                var block = (MyObjectBuilder_ShipConnector)this.block;
                block.ConnectedEntityId = id;
            }
            else
            {
                MyObjectBuilder_AttachableTopBlockBase topBlock = block as MyObjectBuilder_AttachableTopBlockBase;
                if (topBlock != null)
                    topBlock.ParentEntityId = id;
            }
            Attached = id != 0;
        }

        public static bool TryGetConnectionType(MyObjectBuilder_AttachableTopBlockBase block, out MechanicalConnectionType type)
        {
            type = MechanicalConnectionType.Unknown;

            if (block is MyObjectBuilder_PistonTop)
                type = MechanicalConnectionType.Piston;
            else if (block.SubtypeName.Contains("Hinge"))
                type = MechanicalConnectionType.Hinge;
            else if (block is MyObjectBuilder_MotorRotor)
                type = MechanicalConnectionType.Rotor;
            else
                return false;
            return true;
        }

    }
}
