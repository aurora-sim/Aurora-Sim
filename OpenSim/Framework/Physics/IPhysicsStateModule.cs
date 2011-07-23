using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSim.Framework
{
    public interface IPhysicsStateModule
    {
        void SavePhysicsState ();
        void ResetToLastSavedState ();
    }
}
