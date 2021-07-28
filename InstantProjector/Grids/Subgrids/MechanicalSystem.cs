using System;
using System.Collections.Generic;

namespace avaness.GridSpawner.Grids.Subgrids
{
    public class MechanicalSystem
    {
        private readonly List<GridMechanicalSystem> grids = new List<GridMechanicalSystem>();

        public void Add(GridMechanicalSystem grid)
        {
            grids.Add(grid);
        }

        public void Attach()
        {
            foreach (GridMechanicalSystem grid in grids)
            {
                foreach (GridMechanicalSystem grid2 in grids)
                {
                    if(!ReferenceEquals(grid, grid2))
                        grid.AttachBlocks(grid2);
                }
            }
        }

        public void Clean()
        {
            foreach (GridMechanicalSystem grid in grids)
                grid.Clean();
        }
    }
}
