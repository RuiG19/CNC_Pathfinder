using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CNC_Pathfinder.src.Utilities
{
    class Memory_Utilities
    {

        public static Action FreeMemory = () =>
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        };


    }
}
