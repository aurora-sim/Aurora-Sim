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
        void InitApi(IScriptApi data);

        ISponsor Sponsor { get; }
        void UpdateLease(TimeSpan time);
        long GetStateEventFlags(string state);
        void ExecuteEvent(string state, string FunctionName, object[] args);
        Dictionary<string, Object> GetVars();
        void SetVars(Dictionary<string, Object> vars);
        Dictionary<string, Object> GetStoreVars();
        void SetStoreVars(Dictionary<string, Object> vars);
        void ResetVars();
        /// <summary>
        /// Find the initial variables so that we can reset the state later if needed
        /// </summary>
        void UpdateInitialValues();
        
        void Close();
        string Name { get; }
    }
}
