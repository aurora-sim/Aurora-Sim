using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface INinjaPhysicsModule
    {
        void jointCreate(SceneObjectPart part);
    }
}
