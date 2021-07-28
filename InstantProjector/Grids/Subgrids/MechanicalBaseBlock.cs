using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace avaness.GridSpawner.Grids.Subgrids
{
    public class MechanicalBaseBlock
    {
        private readonly MyObjectBuilder_MechanicalConnectionBlock block;
        private MatrixD blockPos;
        private Vector3D bottom;

        public long EntityId => block.EntityId;
        public MechanicalConnectionType Type { get; }

        public MechanicalBaseBlock(MyObjectBuilder_MechanicalConnectionBlock block, MyObjectBuilder_CubeGrid grid, MyMechanicalConnectionBlockBaseDefinition def)
        {
            this.block = block;
            if (block is MyObjectBuilder_PistonBase)
                Type = MechanicalConnectionType.Piston;
            else if (block is MyObjectBuilder_MotorSuspension)
                Type = MechanicalConnectionType.Wheel;
            else if (block.SubtypeName.Contains("Hinge"))
                Type = MechanicalConnectionType.Hinge;
            else if (block is MyObjectBuilder_MotorAdvancedStator)
                Type = MechanicalConnectionType.AdvancedRotor;
            else if (block is MyObjectBuilder_MotorStator)
                Type = MechanicalConnectionType.Rotor;
            else
            {
                Type = MechanicalConnectionType.Unknown;
                return;
            }

            Vector3D blockSize;
            Utilities.GetBlockPosition(block, grid, def, out blockPos, out blockSize);
            bottom = blockPos.Translation + (blockPos.Down * blockSize.Y * 0.5);
        }

        public bool TryGetAlignment(MechanicalTopBlock topBlock, out double alignment)
        {
            alignment = 0;

            MatrixD topPos = topBlock.Position;
            if (!blockPos.Up.Equals(topPos.Up, 0.001))
                return false;

            MatrixD temp = blockPos;
            temp.Translation = bottom;
            Vector3D diff = topBlock.Position.Translation - bottom;
            double project = Utilities.ScalerProjection(diff, blockPos.Up);
            if(project > 0)
            {
                IPSession.Instance.matrix.Add(temp);
                MyVisualScriptLogicProvider.AddGPS(block.CustomName ?? "Block", "", bottom, Color.Purple);
                IPSession.Instance.matrix.Add(topPos);
                alignment = project;
                return true;
            }

            return false;
        }


        // GetTopGridMatrix
        /*private MatrixD GetMotorPos(MyObjectBuilder_MotorBase block, MyMotorStatorDefinition def)
        {
            MatrixD blockMatrix;
            MatrixD gridMatrix;
            Vector3 dummyPosition;
            return MatrixD.CreateWorld(Vector3D.Transform(dummyPosition, gridMatrix), blockMatrix.Forward, blockMatrix.Up);
        }

        private MatrixD GetSuspensionPos(MyObjectBuilder_MotorSuspension block, MyMotorSuspensionDefinition def)
        {
            MatrixD blockMatrix;
            MatrixD gridMatrix;
            Vector3 dummyPosition;
            Vector3 forward = base.PositionComp.LocalMatrixRef.Forward;
            return MatrixD.CreateWorld(Vector3D.Transform(dummyPosition + forward * block.Height, gridMatrix), blockMatrix.Forward, blockMatrix.Up);
        }

        private MatrixD GetPistonPos(MyObjectBuilder_PistonBase block, MyPistonBaseDefinition def)
        {
            MatrixD blockMatrix;
            Vector3 m_constraintBasePos;
            float distance = MathHelper.Clamp(block.CurrentPosition, def.Minimum, def.Maximum);
            return MatrixD.CreateWorld(Vector3D.Transform(m_constraintBasePos, Subpart3.WorldMatrix), blockMatrix.Forward, blockMatrix.Up);
        }*/

        public void SetTopBlock(long id)
        {
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
    }
}
