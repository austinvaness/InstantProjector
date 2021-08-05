using System;
using System.Collections.Generic;

namespace avaness.GridSpawner.Grids.Subgrids
{
    public class MechanicalSystem
    {
        private readonly List<GridMechanicalSystem> grids = new List<GridMechanicalSystem>();

        public void Add(GridMechanicalSystem grid)
        {
            if(grid != null)
                grids.Add(grid);
        }

        public void Attach()
        {
            foreach (GridMechanicalSystem grid in grids)
            {
                foreach (GridMechanicalSystem grid2 in grids)
                {
                    if(!ReferenceEquals(grid, grid2))
                        grid.SearchBlocks(grid2);
                }
            }

            foreach (GridMechanicalSystem grid in grids)
                grid.AttachBlocks();

            foreach (GridMechanicalSystem grid in grids)
                grid.Clean();
        }
    }
}
