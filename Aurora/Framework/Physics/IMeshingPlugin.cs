using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;

namespace Aurora.Framework.Physics
{
    public interface IMeshingPlugin
    {
        string GetName();
        IMesher GetMesher(IConfigSource config);
    }
}
