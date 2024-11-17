using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Train
{
    internal interface ITrainEngine
    {

        void SetAccelerationLevel( int value);
        void SetBrakeLevel(float value);
        void SetIndependentBrakeLevel(float value);
        void SetSandLevel(float value);
        void SetReverser(int value);
        void SetSleep(bool state);
    }
}
