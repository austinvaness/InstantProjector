using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace GridSpawner
{
    public class CountdownManager
    {
        public void Update()
        {

        }
    }

    public class Countdown
    {
        float value;

        public void SubtractTick ()
        {
            value -= MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
        }

    }
}
