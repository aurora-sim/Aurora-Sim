using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public interface IEnvironmentSettingsModule
    {
        void TriggerWindlightUpdate(int interpolate);
    }
}
