using Sandbox.Definitions;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;

namespace avaness.GridSpawner.Grids
{
    public class BlockComponents
    {
        private readonly MyCubeBlockDefinition def;
        private readonly float integrityPercent, buildPercent;

        public bool Valid { get; }
        public bool Complete { get; }

        public BlockComponents(IMySlimBlock slim)
        {
            def = (MyCubeBlockDefinition)slim.BlockDefinition;
            Valid = def != null;
            if(Valid)
            {
                integrityPercent = slim.Integrity / slim.MaxIntegrity;
                buildPercent = slim.BuildLevelRatio;
            }
            Complete = integrityPercent == 1 && buildPercent == 1;
        }

        public BlockComponents(MyObjectBuilder_CubeBlock ob)
        {
            def = MyDefinitionManager.Static.GetCubeBlockDefinition(ob.GetId());
            Valid = def != null;
            if (Valid)
            {
                integrityPercent = ob.IntegrityPercent;
                buildPercent = ob.BuildPercent;
            }
            Complete = IsComplete(ob);
        }

        public static bool IsComplete(MyObjectBuilder_CubeBlock ob)
        {
            return ob.IntegrityPercent == 1 && ob.BuildPercent == 1;
        }

        public IEnumerable<MyCubeBlockDefinition.Component> GetComponents()
        {
            if (Complete)
                return def.Components;

            MyComponentStack stack = new MyComponentStack(def, integrityPercent, buildPercent);
            if (stack.IsFullIntegrity)
                return def.Components;

            return GetComponents(stack);
        }

        private IEnumerable<MyCubeBlockDefinition.Component> GetComponents(MyComponentStack stack)
        {
            for(int i = 0; i < stack.GroupCount; i++)
            {
                MyComponentStack.GroupInfo info = stack.GetGroupInfo(i);
                if (info.MountedCount <= 0)
                    break;
                yield return new MyCubeBlockDefinition.Component()
                {
                    Count = info.MountedCount,
                    Definition = info.Component,
                };
            }
        }
    }
}
