using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public interface ICSCodeGenerator : IDisposable
    {
        string Convert(string script);
        string[] GetWarnings();
        Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap { get; }
    }
}
