using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System;
using VRage.Game;
using VRageMath;

namespace avaness.GridSpawner.Grids.Subgrids
{
    public class MechanicalBaseBlock
    {
        private readonly MyObjectBuilder_CubeBlock block;
        private MatrixD blockPos;
        private MechanicalTopBlock bestGuess;
        private double bestGuessDist;
        private double bestGuessLinear;

        public MechanicalConnectionType Type { get; } = MechanicalConnectionType.Unknown;
        public bool Attached { get; private set; }

        public MechanicalBaseBlock(MyObjectBuilder_CubeBlock block, MyObjectBuilder_CubeGrid grid, MyCubeBlockDefinition def, MechanicalConnectionType type)
        {
            this.block = block;
            
            Type = type;

            Vector3D blockSize;
            Utilities.GetBlockPosition(block, grid, def, out blockPos, out blockSize);
            if (Type == MechanicalConnectionType.Connector)
            {
                Vector3D up = blockPos.Up;
                blockPos.Up = blockPos.Forward;
                blockPos.Forward = up;

                double sizeY = blockSize.Y;
                blockSize.Y = blockSize.Z;
                blockSize.Z = sizeY;
            }
            blockPos.Translation += blockPos.Down * blockSize.Y * 0.5; // Move position to bottom of block 
        }

        public void TestAlignment(MechanicalTopBlock topBlock)
        {
            string me = block.GetType().ToString();
            MatrixD topPos = topBlock.Position;

            // Is top facing correct direction?
            if (Type != MechanicalConnectionType.Connector && !blockPos.Up.Equals(topPos.Up, 0.1))
                return;

            // Is top in front of the base block?
            Vector3D diff = topBlock.Position.Translation - blockPos.Translation;
            double project = Utilities.ScalerProjection(diff, blockPos.Up);
            if (project <= 0)
                return;

            // Calculate linear distance
            double linear;
            if(Type == MechanicalConnectionType.Wheel)
            {
                linear = Math.Abs(Utilities.ScalerProjection(diff, blockPos.Left));
            }
            else
            {
                Vector3D rejection = diff - (project * blockPos.Up);
                linear = rejection.LengthSquared();
            }

            // Mark as best guess if it is closer and the linear distance is approximately equal or less
            double dist = diff.LengthSquared();
            if(bestGuess == null || (dist < bestGuessDist && linear - bestGuessLinear < 0.1))
            {
                bestGuess = topBlock;
                bestGuessDist = dist;
                bestGuessLinear = linear;
            }
        }

        public void Attach()
        {
            if (bestGuess == null || bestGuess.Attached)
            {
                SetTopBlock(0);
            }
            else
            {
                bestGuess.SetBaseBlock(block.EntityId);
                SetTopBlock(bestGuess.EntityId);
            }
        }

        private void SetTopBlock(long id)
        {
            if (Type == MechanicalConnectionType.Connector)
            {
                var block = (MyObjectBuilder_ShipConnector)this.block;
                block.ConnectedEntityId = id;
            }
            else
            {
                var block = (MyObjectBuilder_MechanicalConnectionBlock)this.block;
                block.TopBlockId = id;
                MyObjectBuilder_MotorBase motor = block as MyObjectBuilder_MotorBase;
                if (motor != null)
                {
                    if (id == 0)
                        motor.RotorEntityId = null;
                    else
                        motor.RotorEntityId = id;
                }
            }

            Attached = id != 0;
        }

        public static bool TryGetConnectionType(MyObjectBuilder_MechanicalConnectionBlock block, out MechanicalConnectionType type)
        {
            type = MechanicalConnectionType.Unknown;

            if (block is MyObjectBuilder_PistonBase)
                type = MechanicalConnectionType.Piston;
            else if (block is MyObjectBuilder_MotorSuspension)
                type = MechanicalConnectionType.Wheel;
            else if (block.SubtypeName.Contains("Hinge"))
                type = MechanicalConnectionType.Hinge;
            else if (block is MyObjectBuilder_MotorStator)
                type = MechanicalConnectionType.Rotor;
            else
                return false;
            return true;
        }
    }
}
