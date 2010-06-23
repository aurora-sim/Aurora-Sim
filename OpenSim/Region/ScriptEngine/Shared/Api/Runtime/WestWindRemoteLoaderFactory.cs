using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenSim.Region.ScriptEngine.Interfaces;

namespace OpenSim.Region.ScriptEngine.Shared.ScriptBase
{
    /// <summary>
    /// Factory class to create objects exposing IRemoteInterface
    /// </summary>
    public class WestWindRemoteLoaderFactory : MarshalByRefObject
    {
        private const BindingFlags bfi = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        public WestWindRemoteLoaderFactory() { }

        /// <summary> Factory method to create an instance of the type whose name is specified,
        /// using the named assembly file and the constructor that best matches the specified parameters. </summary>
        /// <param name="assemblyFile"> The name of a file that contains an assembly where the type named typeName is sought. </param>
        /// <param name="typeName"> The name of the preferred type. </param>
        /// <param name="constructArgs"> An array of arguments that match in number, order, and type the parameters of the constructor to invoke, or null for default constructor. </param>
        /// <returns> The return value is the created object represented as ILiveInterface. </returns>
        public IRemoteInterface Create(string assemblyFile, string typeName, object[] constructArgs)
        {
            return (IRemoteInterface)Activator.CreateInstanceFrom(
                assemblyFile, typeName, false, bfi, null, constructArgs,
                null, null, null).Unwrap();
        }
    }	
}
