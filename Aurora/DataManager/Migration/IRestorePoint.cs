using Aurora.Framework;

namespace Aurora.DataManager.Migration
{
    public interface IRestorePoint
    {
        void DoRestore(IDataConnector genericData);
    }
}