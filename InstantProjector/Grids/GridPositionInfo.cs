using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRageMath;

namespace avaness.GridSpawner.Grids
{
    [ProtoContract]
    public class GridPositionInfo
    {
        [ProtoMember(1, IsPacked = true)]
        public double[] positions;

        public GridPositionInfo()
        { }

        public GridPositionInfo(List<MyObjectBuilder_CubeGrid> grids)
        {
            if(grids.Count > 1)
            {
                List<double> positions = new List<double>(grids.Count * 3);
                foreach(MyObjectBuilder_CubeGrid grid in grids)
                {
                    SerializableVector3D position = grid.PositionAndOrientation.Value.Position;
                    positions.Add(position.X);
                    positions.Add(position.Y);
                    positions.Add(position.Z);
                }
                this.positions = positions.ToArray();
            }

            byte[] data = MyAPIGateway.Utilities.SerializeToBinary(this);
            MyVisualScriptLogicProvider.SendChatMessage("Length: " + data.Length, "InstantProjector");
        }

        public void Apply(List<MyObjectBuilder_CubeGrid> grids)
        {
            if (positions == null || (grids.Count * 3) != positions.Length)
                return;

            int i = 0;
            foreach(MyObjectBuilder_CubeGrid grid in grids)
            {
                MyPositionAndOrientation current = grid.PositionAndOrientation.Value;
                double x = positions[i];
                i++;
                double y = positions[i];
                i++;
                double z = positions[i];
                i++;
                current.Position = new SerializableVector3D(x, y, z);
                grid.PositionAndOrientation = current;
            }
        }
    }
}
