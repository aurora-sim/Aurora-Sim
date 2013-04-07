using Aurora.Framework.ClientInterfaces;

namespace Aurora.Framework.Modules
{
    public interface IEnvironmentSettingsModule
    {
        WindlightDayCycle GetCurrentDayCycle();
        void TriggerWindlightUpdate(int interpolate);

        void SetDayCycle(WindlightDayCycle cycle);
    }
}