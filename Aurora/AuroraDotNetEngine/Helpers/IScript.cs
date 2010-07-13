using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Lifetime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public interface IScript : IDisposable
    {
        string[] GetApis();
        void InitApi(string name, IScriptApi data);

        ISponsor Sponsor { get; }
        void UpdateLease(TimeSpan time);
        int GetStateEventFlags(string state);
        Guid ExecuteEvent(string state, string FunctionName, object[] args, Guid Start, out Exception ex);
        Dictionary<string, Object> GetVars();
        void SetVars(Dictionary<string, Object> vars);
        void ResetVars();

        void Close();
        string Name { get; }
    }
}
