using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Lifetime;
using OpenSim.Framework;

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
        EnumeratorInfo ExecuteEvent(string state, string FunctionName, object[] args, EnumeratorInfo Start, out Exception ex);
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

        /// <summary>
        /// Gives a ref to the scene the script is in and its parent object
        /// </summary>
        /// <param name="iScene"></param>
        /// <param name="iSceneChildEntity"></param>
        /// <param name="useStateSaves"></param>
        void SetSceneRefs (IScene iScene, ISceneChildEntity iSceneChildEntity, bool useStateSaves);

        /// <summary>
        /// Whether this script needs a state save performed
        /// </summary>
        bool NeedsStateSaved { get; set; }
    }
}
