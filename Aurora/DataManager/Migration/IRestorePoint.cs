using Aurora.Framework;

namespace Aurora.DataManager.Migration
{
    public interface IRestorePoint
    {
        void DoRestore(DataSessionProvider sessionProvider, IGenericData genericData);
    }
}