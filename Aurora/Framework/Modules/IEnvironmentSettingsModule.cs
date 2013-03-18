namespace Aurora.Framework
{
    public interface IEnvironmentSettingsModule
    {
        WindlightDayCycle GetCurrentDayCycle();
        void TriggerWindlightUpdate(int interpolate);

        void SetDayCycle(WindlightDayCycle cycle);
    }
}
