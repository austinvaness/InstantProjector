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
                IMyEntity e = GetOverlappingEntity(obb, null, entityIds);
                if (e != null)
                    return e;
            }
            return null;
        }

        public static IMyEntity GetOverlappingEntity(IMyEntity e)
        {
            MyOrientedBoundingBoxD obb;
            GetOBB(e, out obb);
            return GetOverlappingEntity(obb, e);
        }

        private static IMyEntity GetOverlappingEntity(MyOrientedBoundingBoxD obb, IMyEntity original = null, HashSet<long> entityIds = null)
        {
            BoundingBoxD localAABB = new BoundingBoxD(-obb.HalfExtent, obb.HalfExtent);
            MatrixD localAABBOrientation = MatrixD.CreateFromQuaternion(obb.Orientation);
            localAABBOrientation.Translation = obb.Center;

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
                            if ((entityIds == null || !entityIds.Contains(e.EntityId)) && HasBlocksInside((MyCubeGrid)e, ref obb))
                                return e;
                        }
                        else if (e is MyVoxelBase)
                        {
                            if (IsCollidingWith((MyVoxelBase)e, localAABB, localAABBOrientation))
                                return e;
                        }
                        else if (e is MySafeZone)
                        {
                            if (!IsAllowed((MySafeZone)e, obb, original))
                                return e;
                        }
                        else
                        {
                            MyOrientedBoundingBoxD eObb;
                            GetOBB(e, out eObb);
                            if (eObb.Contains(ref obb) != ContainmentType.Disjoint)
                                return e;
                        }
                    }
                }
            }
            return null;
        }

        private static bool IsAllowed(MySafeZone safezone, MyOrientedBoundingBoxD obb, IMyEntity original = null)
        {
            if (!safezone.Enabled)
                return true;

            if (safezone.AccessTypeGrids == Sandbox.Common.ObjectBuilders.MySafeZoneAccess.Whitelist)
                return false;

            if(safezone.Shape == Sandbox.Common.ObjectBuilders.MySafeZoneShape.Box)
            {
                var zoneOBB = new MyOrientedBoundingBoxD(safezone.PositionComp.LocalAABB, safezone.PositionComp.WorldMatrixRef);
                if (obb.Contains(ref zoneOBB) == ContainmentType.Disjoint)
                    return true;
            }
            else
            {
                var zoneSphere = new BoundingSphereD(safezone.PositionComp.GetPosition(), safezone.Radius);
                if (obb.Contains(ref zoneSphere) == ContainmentType.Disjoint)
                    return true;
            }

            // 512 = VRage.Game.ObjectBuilders.Components.MySafeZoneAction.BuildingProjections
            if (original == null)
                return safezone.IsActionAllowed(obb.GetAABB(), CastProhibit(safezone.AllowedActions, 512));
            return safezone.IsActionAllowed((MyEntity)original, CastProhibit(safezone.AllowedActions, 512));
        }

        // Hack for MySafeZoneAction because it is not whitelisted
        // Source: https://discord.com/channels/125011928711036928/126460115204308993/829013796337090561
        private static T CastProhibit<T>(T ptr, object val) => (T)val;

        private static bool IsCollidingWith(MyVoxelBase voxel, BoundingBoxD box, MatrixD orientation)
        {
            if (voxel.RootVoxel != null)
                voxel = voxel.RootVoxel;

            if (voxel.IsAnyAabbCornerInside(ref orientation, box))
                return true;

            Vector3D extents = box.HalfExtents;
            double min = Math.Min(extents.X, Math.Min(extents.Y, extents.Z));
            return voxel.DoOverlapSphereTest((float)min, orientation.Translation);
        }


        // Context: Server
        // https://github.com/rexxar-tc/ShipyardMod/blob/master/ShipyardMod/Utility/MathUtility.cs#L66
        private static void GetOBB(IMyEntity e, out MyOrientedBoundingBoxD obb)
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(e.WorldMatrix);
            Vector3D exts = e.PositionComp.LocalAABB.HalfExtents;
            obb = new MyOrientedBoundingBoxD(e.PositionComp.WorldAABB.Center, exts, quat);
        }

        private static bool HasBlocksInside(MyCubeGrid grid, ref MyOrientedBoundingBoxD obb)
        {
            Vector3I center = grid.WorldToGridInteger(obb.Center);
            double radius = obb.HalfExtent.Length();
            radius *= grid.GridSizeR;
            Vector3I gridMin = grid.Min;
            Vector3I gridMax = grid.Max;
            double radiusSq = radius * radius;
            int radiusCeil = (int)Math.Ceiling(radius);

            BoundingSphereD cubeSphere = new BoundingSphereD(new Vector3D(), grid.GridSizeHalf * Math.Sqrt(3));

            Vector3I max = Vector3I.Min(Vector3I.One * radiusCeil, gridMax - center);
            Vector3I min = Vector3I.Max(Vector3I.One * -radiusCeil, gridMin - center);
            int x, y, z;
            for (x = min.X; x <= max.X; ++x)
            {
                for (y = min.Y; y <= max.Y; ++y)
                {
                    for (z = min.Z; z <= max.Z; ++z)
                    {
                        if (x * x + y * y + z * z >= radiusSq)
                            continue;

                        Vector3I offset = new Vector3I(x, y, z);

                        Vector3I cubePos = center + offset;
                        MyCube cube;
                        if (!grid.TryGetCube(cubePos, out cube))
                            continue;

                        IMySlimBlock slim = cube.CubeBlock;
                        if (slim.IsDestroyed)
                            continue;

                        cubeSphere.Center = grid.GridIntegerToWorld(cubePos);
                        if (obb.Contains(ref cubeSphere) != ContainmentType.Disjoint)
                            return true;
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
