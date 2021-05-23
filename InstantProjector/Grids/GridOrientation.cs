using System;
using System.Collections;
using System.Collections.Generic;
using VRage.ModAPI;
using VRageMath;

namespace avaness.GridSpawner.Grids
{
    public class GridOrientation : IEnumerable<MatrixD>
    {
        private IMyEntity reference;
        private MatrixD referenceNI;
        private readonly List<MatrixD> localMatricies = new List<MatrixD>();

        public GridOrientation(IMyEntity e)
        {
            reference = e;
            PrepInclude();
        }

        public void PrepInclude()
        {
            referenceNI = MatrixD.Normalize(MatrixD.Invert(reference.WorldMatrix));
        }

        public void Include(MatrixD world)
        {
            localMatricies.Add(Utilities.WorldToLocalNI(world, referenceNI));
        }

        public IEnumerator<MatrixD> WorldMatricies()
        {
            MatrixD refMatrix = reference.WorldMatrix;
            foreach (MatrixD m in localMatricies)
                yield return Utilities.LocalToWorld(m, refMatrix);
        }

        public IEnumerator<MatrixD> GetEnumerator()
        {
            return WorldMatricies();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return WorldMatricies();
        }

        public void Translate(Vector3D local)
        {
            for(int i = 0; i < localMatricies.Count; i++)
            {
                MatrixD m = localMatricies[i];
                m.Translation += local;
                localMatricies[i] = m;
            }
        }
    }
}
