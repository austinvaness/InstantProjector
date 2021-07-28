using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRageMath;

namespace avaness.GridSpawner.Grids.Subgrids
{
    public class MechanicalTopBlock
    {
        public readonly MyObjectBuilder_CubeBlock block;

        public long EntityId => block.EntityId;
        public MechanicalConnectionType Type { get; }
        public MatrixD Position { get; }

        public MechanicalTopBlock(MyObjectBuilder_CubeBlock block, MyObjectBuilder_CubeGrid grid, MyCubeBlockDefinition def)
        {
            this.block = block;
            if (block is MyObjectBuilder_PistonTop)
                Type = MechanicalConnectionType.Piston;
            else if (block is MyObjectBuilder_Wheel && block.SubtypeName.Contains("RealWheel"))
                Type = MechanicalConnectionType.Wheel;
            else if (block.SubtypeName.Contains("Hinge"))
                Type = MechanicalConnectionType.Hinge;
            else if (block is MyObjectBuilder_MotorAdvancedRotor)
                Type = MechanicalConnectionType.AdvancedRotor;
            else if (block is MyObjectBuilder_MotorRotor)
                Type = MechanicalConnectionType.Rotor;
            else
            {
                Type = MechanicalConnectionType.Unknown;
                return;
            }

            MatrixD blockPos;
            Vector3D blockSize;
            Utilities.GetBlockPosition(block, grid, def, out blockPos, out blockSize);
            Position = blockPos;
        }

        public void SetBaseBlock(long id)
        {
            MyObjectBuilder_AttachableTopBlockBase topBlock = block as MyObjectBuilder_AttachableTopBlockBase;
            if(topBlock != null)
                topBlock.ParentEntityId = id;
        }
    }
}
