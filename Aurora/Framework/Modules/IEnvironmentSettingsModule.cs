using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public interface IEnvironmentSettingsModule
    {
        WindlightDayCycle GetCurrentDayCycle();
        void TriggerWindlightUpdate(int interpolate);

        void SetDayCycle(WindlightDayCycle cycle);
    }
}
