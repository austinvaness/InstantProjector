using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace avaness.GridSpawner.Grids
{
    public class GridBounds
    {
        private IMyEntity reference;
        private BoundingSphereD worldVolume;
        private Vector3D relativeCenter;
        private GridOrientation orientation;
        private List<MyOrientedBoundingBoxD> obbs;
        private HashSet<long> entityIds;

        public void Update()
        {
            MatrixD mRef = reference.WorldMatrix;

            worldVolume.Center = Vector3D.Transform(relativeCenter, mRef);

            int i = 0;
            foreach(MatrixD world in orientation)
            {
                obbs[i] = new MyOrientedBoundingBoxD(
                    world.Translation,
                    obbs[i].HalfExtent, 
                    Quaternion.CreateFromForwardUp(world.Forward, world.Up));

                i++;
            }
        }

        public GridBounds(IMyEntity e, List<MyObjectBuilder_CubeGrid> grids)
        {
            reference = e;
            Create(grids);
        }

        private void Create(List<MyObjectBuilder_CubeGrid> grids)
        {
            Vector3D centerSum = new Vector3D();

            MatrixD mRef = reference.WorldMatrix;
            MatrixD mRefNI = MatrixD.Normalize(MatrixD.Invert(mRef));

            obbs = new List<MyOrientedBoundingBoxD>(grids.Count);
            orientation = new GridOrientation(reference);
            entityIds = new HashSet<long>();
            foreach(MyObjectBuilder_CubeGrid grid in grids)
            {
                entityIds.Add(grid.EntityId);
                Vector3I min = Vector3I.MaxValue;
                Vector3I max = Vector3I.MinValue;
                foreach(MyObjectBuilder_CubeBlock cube in grid.CubeBlocks)
                {
                    min = Vector3I.Min(min, cube.Min);
                    max = Vector3I.Max(max, ComputeMax(cube));
                }

                double cubeGridSize = grid.GridSizeEnum == MyCubeSize.Large ? 2.5 : 0.5;
                Vector3D pMin = min * cubeGridSize - (cubeGridSize * 0.5);
                Vector3D pMax = max * cubeGridSize + (cubeGridSize * 0.5);

                MyPositionAndOrientation pos = grid.PositionAndOrientation.Value;
                Vector3D center = Vector3D.Transform((pMin + pMax) * 0.5, pos.GetMatrix());
                centerSum += center;
                MyOrientedBoundingBoxD box = new MyOrientedBoundingBoxD(center, (pMax - pMin) * 0.5, pos.Orientation);
                obbs.Add(box);

                orientation.Include(MatrixD.CreateWorld(center, pos.Forward, pos.Up));
            }


            centerSum /= obbs.Count;
            relativeCenter = Vector3D.TransformNormal(centerSum - mRef.Translation, MatrixD.Transpose(mRef));

            double radius2 = 0;
            foreach(MyOrientedBoundingBoxD obb in obbs)
            {
                double dist2 = Vector3D.DistanceSquared(centerSum, obb.Center) + obb.HalfExtent.LengthSquared();
                if (dist2 > radius2)
                    radius2 = dist2;
            }

            worldVolume = new BoundingSphereD(centerSum, Math.Sqrt(radius2));
        }

        private Vector3I ComputeMax(MyObjectBuilder_CubeBlock cube)
        {
            MyCubeBlockDefinition definition =  MyDefinitionManager.Static.GetCubeBlockDefinition(cube.GetId());
            Vector3I result = definition.Size - 1;
            MatrixI matrix = new MatrixI(cube.BlockOrientation);
            Vector3I.TransformNormal(ref result, ref matrix, out result);
            Vector3I.Abs(ref result, out result);
            return cube.Min + result;
        }

        public IMyEntity GetOverlappingEntity()
        {
            foreach(MyOrientedBoundingBoxD obb in obbs)
            {
                BoundingSphereD worldVol = new BoundingSphereD(obb.Center, obb.HalfExtent.Length());
                IMyEntity e = GetOverlappingEntity(obb, worldVol, entityIds);
                if (e != null)
                    return e;
            }
            return null;
        }

        public static IMyEntity GetOverlappingEntity(IMyEntity e)
        {
            MyOrientedBoundingBoxD obb;
            BoundingSphereD bs;
            GetOBB(e, out obb, out bs);
            return GetOverlappingEntity(obb, bs);
        }

        private static IMyEntity GetOverlappingEntity(MyOrientedBoundingBoxD obb, BoundingSphereD bs, HashSet<long> entityIds = null)
        {
            BoundingBoxD localAABB = new BoundingBoxD(obb.Center - obb.HalfExtent, obb.Center + obb.HalfExtent);
            MatrixD localAABBOrientation = MatrixD.CreateFromQuaternion(obb.Orientation);
            List<MyEntity> entities = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInOBB(ref obb, entities);
            if (entities.Count > 0)
            {
                foreach (MyEntity entity in entities)
                {
                    IMyEntity e = entity;
                    if (e.Physics != null && e.Physics.Enabled)
                    {
                        if (e is IMyCubeGrid)
                        {
                            if ((entityIds == null || !entityIds.Contains(e.EntityId)) && HasBlocksInsideSphere((MyCubeGrid)e, ref bs))
                                return e;
                        }
                        else if (e is MyVoxelBase)
                        {
                            MyTuple<float, float> result = ((MyVoxelBase)e).GetVoxelContentInBoundingBox_Fast(localAABB, localAABBOrientation);
                            if (!float.IsNaN(result.Item2) && !float.IsInfinity(result.Item2) && result.Item2 != 0)
                                return e;
                        }
                        else
                        {
                            MyOrientedBoundingBoxD eObb;
                            BoundingSphereD eBs;
                            GetOBB(e, out eObb, out eBs);
                            if (eObb.Contains(ref obb) != ContainmentType.Disjoint)
                                return e;
                        }
                    }
                }
            }
            return null;
        }


        // Context: Server
        // https://github.com/rexxar-tc/ShipyardMod/blob/master/ShipyardMod/Utility/MathUtility.cs#L66
        private static void GetOBB(IMyEntity e, out MyOrientedBoundingBoxD obb, out BoundingSphereD bs)
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(e.WorldMatrix);
            Vector3D exts = e.PositionComp.LocalAABB.HalfExtents;
            obb = new MyOrientedBoundingBoxD(e.PositionComp.WorldAABB.Center, exts, quat);
            bs = new BoundingSphereD(obb.Center, exts.Length());
        }

        private static bool HasBlocksInsideSphere(MyCubeGrid grid, ref BoundingSphereD sphere)
        {
            var radius = sphere.Radius;
            radius *= grid.GridSizeR;
            var center = grid.WorldToGridInteger(sphere.Center);
            var gridMin = grid.Min;
            var gridMax = grid.Max;
            double radiusSq = radius * radius;
            int radiusCeil = (int)Math.Ceiling(radius);
            int i, j, k;
            Vector3I max2 = Vector3I.Min(Vector3I.One * radiusCeil, gridMax - center);
            Vector3I min2 = Vector3I.Max(Vector3I.One * -radiusCeil, gridMin - center);
            for (i = min2.X; i <= max2.X; ++i)
            {
                for (j = min2.Y; j <= max2.Y; ++j)
                {
                    for (k = min2.Z; k <= max2.Z; ++k)
                    {
                        if (i * i + j * j + k * k < radiusSq)
                        {
                            MyCube cube;
                            var vector3I = center + new Vector3I(i, j, k);

                            if (grid.TryGetCube(vector3I, out cube))
                            {
                                var slim = (IMySlimBlock)cube.CubeBlock;
                                if (slim.Position == vector3I && !slim.IsDestroyed)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public bool HasClearArea()
        {
            return MyAPIGateway.Entities.FindFreePlace(worldVolume.Center, (float)worldVolume.Radius).HasValue;
        }

        public bool TryFindClearArea(GridOrientation orientation)
        {
            Vector3D? result = MyAPIGateway.Entities.FindFreePlace(worldVolume.Center, (float)worldVolume.Radius);
            if (!result.HasValue || Vector3D.DistanceSquared(worldVolume.Center, result.Value) > Constants.maxNewDist2)
                return false;

            MatrixD mRef = reference.WorldMatrix;
            Vector3D newPosRel = Vector3D.TransformNormal(result.Value - mRef.Translation, MatrixD.Transpose(mRef));
            orientation.Translate(newPosRel - relativeCenter);
            return true;
        }
    }
}
