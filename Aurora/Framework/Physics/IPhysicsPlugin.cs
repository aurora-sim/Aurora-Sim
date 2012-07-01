using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework.Physics
{
    public interface IPhysicsPlugin
    {
        bool Init();
        PhysicsScene GetScene();
        string GetName();
        void Dispose();
    }
}
