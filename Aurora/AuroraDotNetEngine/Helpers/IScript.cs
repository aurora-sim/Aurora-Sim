using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Lifetime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    [Serializable]
    public class EnumeratorInfo
    {
        public Guid Key = Guid.Empty;
        public DateTime SleepTo = DateTime.MinValue;
    }

    public interface IScript : IDisposable
    {
        void InitApi(string name, IScriptApi data);

        ISponsor Sponsor { get; }
        void UpdateLease(TimeSpan time);
        long GetStateEventFlags(string state);
        EnumeratorInfo ExecuteEvent(string state, string FunctionName, object[] args, EnumeratorInfo Start, out Exception ex);
        Dictionary<string, Object> GetVars();
        void SetVars(Dictionary<string, Object> vars);
        Dictionary<string, Object> GetStoreVars();
        void SetStoreVars(Dictionary<string, Object> vars);
        void ResetVars();

        void Close();
        string Name { get; }
    }
}
