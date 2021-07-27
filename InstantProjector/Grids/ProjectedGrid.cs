﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace avaness.GridSpawner.Grids
{
    public class ProjectedGrid
    {
        public ulong Activator { get; }
        public int BlockCount { get; }

        private int startTime;
        private readonly List<MyObjectBuilder_CubeGrid> grids;
        private readonly GridComponents comps;
        private readonly GridBounds bounds;
        private readonly IMyProjector p;
        private readonly GridOrientation finalOrientation;
        private readonly bool shiftBuildArea;
        private readonly ActivatorInfo owner;

        private ParallelSpawner spawner;
        private Action onDone;

        private ProjectedGrid(ulong activator, IMyProjector p, List<MyObjectBuilder_CubeGrid> grids, GridBounds bounds, GridComponents comps, GridOrientation orientation, bool shiftBuildArea, int blockCount, ActivatorInfo owner)
        {
            Activator = activator;
            BlockCount = blockCount;
            this.p = p;
            this.grids = grids;
            this.bounds = bounds;
            this.comps = comps;
            finalOrientation = orientation;
            this.shiftBuildArea = shiftBuildArea;
            this.owner = owner;
        }

        public static bool TryCreate(ulong activator, IMyProjector p, bool shiftBuildArea, GridPositionInfo positionFix, out ProjectedGrid projectedGrid)
        {
            projectedGrid = null;

            // Ensure the projector is valid and has a projection
            if (p.CubeGrid?.Physics == null)
            {
                Utilities.Notify(Constants.msgError + "bad_physics", activator);
                return false;
            }

            if (p.ProjectedGrid == null)
            {
                Utilities.Notify(Constants.msgNoGrid, activator);
                return false;
            }

            MyObjectBuilder_Projector pBuilder = (MyObjectBuilder_Projector)p.GetObjectBuilderCubeBlock(true);
            if (pBuilder.ProjectedGrids == null || pBuilder.ProjectedGrids.Count == 0)
            {
                Utilities.Notify(Constants.msgNoGrid, activator);
                return false;
            }

            if (GetScale(p) != 1)
            {
                Utilities.Notify(Constants.msgScale, activator);
                return false;
            }

            // Prepare list of grids
            List<MyObjectBuilder_CubeGrid> grids = pBuilder.ProjectedGrids;
            positionFix?.Apply(grids);
            int largestIndex = FindLargest(grids);

            MyObjectBuilder_CubeGrid largestGrid = grids[largestIndex];
            if (Utilities.SupportsSubgrids(p))
            {
                if(largestIndex != 0)
                {
                    MyObjectBuilder_CubeGrid temp = grids[0];
                    grids[0] = largestGrid;
                    grids[largestIndex] = temp;
                }
            }
            else
            {
                grids.Clear();
                grids.Add(largestGrid);
            }

            MatrixD largestMatrixInvert = MatrixD.Invert(largestGrid.PositionAndOrientation.Value.GetMatrix());
            MatrixD targetMatrix = p.ProjectedGrid.WorldMatrix;

            float scale = GetScale(p);

            GridOrientation orientation = new GridOrientation(p);

            GridComponents comps = null;
            if (!MyAPIGateway.Session.CreativeMode)
                comps = new GridComponents();

            int totalBlocks = 0;

            MyIDModule owner = ((MyCubeBlock)p).IDModule;
            if (activator != 0)
            {
                long temp = MyAPIGateway.Players.TryGetIdentityId(activator);
                if(temp != 0)
                {
                    if (owner.ShareMode == MyOwnershipShareModeEnum.All)
                        owner = new MyIDModule(temp, MyOwnershipShareModeEnum.Faction);
                    else
                        owner = new MyIDModule(temp, owner.ShareMode);
                }
            }

            Random rand = new Random();
            foreach (MyObjectBuilder_CubeGrid grid in grids)
            {
                totalBlocks += grid.CubeBlocks.Count;
                if (totalBlocks > IPSession.Instance.MapSettings.MaxBlocks)
                {
                    Utilities.Notify(Constants.msgGridLarge, activator);
                    return false;
                }

                PrepBlocks(rand, owner, grid, comps);
                if(grid.CubeBlocks.Count == 0)
                {
                    Utilities.Notify(Constants.msgGridSmall, activator);
                    return false;
                }

                grid.IsStatic = false;
                grid.CreatePhysics = true;
                grid.Immune = false;
                grid.DestructibleBlocks = true;

                MatrixD current = grid.PositionAndOrientation.Value.GetMatrix();
                MatrixD newWorldMatrix = (current * largestMatrixInvert) * targetMatrix;
                grid.PositionAndOrientation = new MyPositionAndOrientation(ref newWorldMatrix);
                orientation.Include(newWorldMatrix);
            }

            if (totalBlocks < IPSession.Instance.MapSettings.MinBlocks)
            {
                Utilities.Notify(Constants.msgGridSmall, activator);
                return false;
            }


            if (comps == null)
            {
                comps = new GridComponents();
            }
            else
            {
                comps.ApplySettings(IPSession.Instance.MapSettings);
                int needed;
                MyDefinitionId neededId;
                if (!comps.HasComponents(Utilities.GetInventories(p), out needed, out neededId))
                {
                    Utilities.Notify(Utilities.GetCompsString(needed, neededId), activator);
                    return false;
                }
            }


            GridBounds bounds = new GridBounds(p, grids);
            ActivatorInfo ownerInfo = new ActivatorInfo(owner.Owner);
            ownerInfo.Whitelist(p.CubeGrid); // In cases where the projector is a shared projector station, it needs to be whitelisted.
            IMyEntity e = bounds.GetOverlappingEntity(ownerInfo);
            if (e != null && (!shiftBuildArea || !bounds.HasClearArea()))
            {
                Utilities.Notify(Utilities.GetOverlapString(true, e), activator);
                return false;
            }

            projectedGrid = new ProjectedGrid(activator, p, grids, bounds, comps, orientation, shiftBuildArea, totalBlocks, ownerInfo);
            return true;
        }

        private static void PrepBlocks(Random rand, MyIDModule owner, MyObjectBuilder_CubeGrid grid, GridComponents comps)
        {
            for (int i = grid.CubeBlocks.Count - 1; i >= 0; i--)
            {
                MyObjectBuilder_CubeBlock cubeBuilder = grid.CubeBlocks[i];
                if (cubeBuilder.EntityId == 0)
                {
                    if (!Utilities.RandomEntityId(rand, out cubeBuilder.EntityId))
                    {
                        grid.CubeBlocks.RemoveAtFast(i);
                        continue;
                    }
                }

                cubeBuilder.SetupForProjector();
                MyCubeBlockDefinition def = MyDefinitionManager.Static.GetCubeBlockDefinition(cubeBuilder);
                if(def == null)
                {
                    grid.CubeBlocks.RemoveAtFast(i);
                    continue;
                }

                if(comps != null)
                    comps.Include(new BlockComponents(cubeBuilder));

                cubeBuilder.Owner = owner.Owner;
                cubeBuilder.BuiltBy = owner.Owner;
                cubeBuilder.ShareMode = owner.ShareMode;

                // Since the cross grid entity ids are invalid, remove references to them.
                if (cubeBuilder is MyObjectBuilder_AttachableTopBlockBase)
                    ((MyObjectBuilder_AttachableTopBlockBase)cubeBuilder).ParentEntityId = 0;
                if (cubeBuilder is MyObjectBuilder_MechanicalConnectionBlock)
                    ((MyObjectBuilder_MechanicalConnectionBlock)cubeBuilder).TopBlockId = null;
                if(cubeBuilder is MyObjectBuilder_MotorBase)
                    ((MyObjectBuilder_MotorBase)cubeBuilder).RotorEntityId = null;


                if (cubeBuilder is MyObjectBuilder_FunctionalBlock)
                    ((MyObjectBuilder_FunctionalBlock)cubeBuilder).Enabled = true;
                if (cubeBuilder is MyObjectBuilder_BatteryBlock)
                {
                    MyBatteryBlockDefinition batDef = (MyBatteryBlockDefinition)def;
                    ((MyObjectBuilder_BatteryBlock)cubeBuilder).CurrentStoredPower = batDef.InitialStoredPowerRatio * batDef.MaxStoredPower;
                }
            }

        }

        private static int FindLargest(List<MyObjectBuilder_CubeGrid> grids)
        {
            int maxBlockCount = int.MinValue;
            int largest = -1;
            for(int i = 0; i < grids.Count; i++)
            {
                MyObjectBuilder_CubeGrid grid = grids[i];
                if (grid.CubeBlocks.Count > maxBlockCount)
                {
                    maxBlockCount = grid.CubeBlocks.Count;
                    largest = i;
                }
            }
            return largest;
        }

        private static float GetScale(IMyProjector p)
        {
            if (p.BlockDefinition.SubtypeId != "LargeBlockConsole")
                return 1;
            return p.GetValueFloat("Scale");
        }


        public bool Spawn(Action onDone)
        {
            bounds.Update();
            IMyEntity e = bounds.GetOverlappingEntity(owner);
            if (e != null)
            {
                if (shiftBuildArea)
                {
                    Utilities.Notify(Constants.msgDifferentSpace, Activator);
                    if (!bounds.TryFindClearArea(finalOrientation))
                    {
                        Utilities.Notify(Utilities.GetOverlapString(true, e), Activator);
                        return false;
                    }
                }
                else
                {
                    Utilities.Notify(Utilities.GetOverlapString(true, e), Activator);
                    return false;
                }
            }

            // Realign projection to projector
            int i = 0;
            foreach(MatrixD world in finalOrientation)
            {
                grids[i].PositionAndOrientation = new MyPositionAndOrientation(world);

                i++;
            }

            startTime = IPSession.Instance.Runtime;
            spawner = new ParallelSpawner(grids, OnSpawned);
            if(!spawner.Start())
                return false;
            this.onDone = onDone;
            return true;
        }

        private void OnSpawned(HashSet<IMyCubeGrid> grids)
        {
            onDone.Invoke();

            Vector3D velocity = p.CubeGrid.Physics.LinearVelocity;

            Vector3D diff = Vector3D.Zero;
            bool first = true;
            HashSet<long> gridIds = new HashSet<long>();
            foreach (IMyCubeGrid grid in grids)
            {
                grid.Physics.LinearVelocity = velocity;
                if (first)
                {
                    diff = AccelerateTime(grid, velocity);
                }
                else
                {
                    MatrixD temp = grid.WorldMatrix;
                    temp.Translation += diff;
                    grid.WorldMatrix = temp;
                }

                gridIds.Add(grid.EntityId);
                IMyEntity e = GridBounds.GetOverlappingEntity(grid, owner);
                if (e != null)
                {
                    Utilities.Notify(Utilities.GetOverlapString(true, e), Activator);
                    ParallelSpawner.Close(grids);
                    return;
                }

                if(grids.Count > 1)
                {
                    var cubes = ((MyCubeGrid)grid).GetFatBlocks();
                    foreach (MyCubeBlock cube in cubes)
                    {
                        IMyMechanicalConnectionBlock baseBlock = cube as IMyMechanicalConnectionBlock;
                        if(baseBlock != null)
                            baseBlock.Attach();
                    }
                }
            }

            if (MyAPIGateway.Session.CreativeMode || comps.ConsumeComponents(Activator, Utilities.GetInventories(p)))
                ParallelSpawner.Add(grids);
            else
                ParallelSpawner.Close(grids);
        }

        private Vector3D AccelerateTime(IMyEntity e, Vector3D velocity)
        {
            float deltaTime = Math.Max(IPSession.Instance.Runtime - startTime, 0) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            MatrixD temp = e.WorldMatrix;
            Vector3D diff = velocity * deltaTime;
            temp.Translation += diff;
            e.WorldMatrix = temp;
            return diff;
        }


        public void Notify(string msg, int seconds = 5)
        {
            Utilities.Notify(msg, Activator, seconds);
        }

        public void UpdateBounds()
        {
            bounds.Update();
        }

        public IMyEntity GetOverlappingEntity()
        {
            return bounds.GetOverlappingEntity(owner);
        }

        public bool HasComponents(out int neededCount, out MyDefinitionId neededId)
        {
            return comps.HasComponents(Utilities.GetInventories(p), out neededCount, out neededId);
        }
    }
}


