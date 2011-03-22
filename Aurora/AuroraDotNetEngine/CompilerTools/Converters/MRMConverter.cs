using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Text;
using Microsoft.CSharp;
//using Microsoft.JScript;
using Microsoft.VisualBasic;
using log4net;
using OpenSim.Region.Framework.Interfaces;
using OpenMetaverse;
using Aurora.ScriptEngine.AuroraDotNetEngine.MiniModule;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools
{
    public class MRMConverter : IScriptConverter
    {
        private CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();

        public string DefaultState { get { return ""; } }

        public void Initialise(Compiler compiler)
        {
        }

        public void Convert(string Script, out string CompiledScript, out Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> PositionMap)
        {
            CompiledScript = CreateCompilerScript(Script);
            PositionMap = null;
        }

        public string Name
        {
            get { return "MRM:C#"; }
        }

        public void Dispose()
        {
        }

        private string CreateCompilerScript(string compileScript)
        {
            return ConvertMRMKeywords (compileScript);
        }

        private string ConvertMRMKeywords (string script)
        {
            script = script.Replace ("microthreaded void", "IEnumerable");
            script = script.Replace ("relax;", "yield return null;");

            return script;
        }

        public CompilerResults Compile(CompilerParameters parameters, string Script)
        {
            List<string> libraries = new List<string> ();
            string[] lines = Script.Split (new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in lines)
            {
                if (s.StartsWith ("//@DEPENDS:"))
                {
                    libraries.Add (s.Replace ("//@DEPENDS:", ""));
                }
            }

            string rootPath =
                Path.GetDirectoryName (AppDomain.CurrentDomain.BaseDirectory);
            foreach (string library in libraries)
            {
                parameters.ReferencedAssemblies.Add (Path.Combine (rootPath, library));
            }

            libraries.Add ("OpenSim.Region.OptionalModules.dll");
            libraries.Add ("OpenMetaverseTypes.dll");
            libraries.Add ("log4net.dll");


            bool complete = false;
            bool retried = false;

            CompilerResults results;
            do
            {
                lock (CScodeProvider)
                {
                    results = CScodeProvider.CompileAssemblyFromSource(
                        parameters, Script);
                }
                // Deal with an occasional segv in the compiler.
                // Rarely, if ever, occurs twice in succession.
                // Line # == 0 and no file name are indications that
                // this is a native stack trace rather than a normal
                // error log.
                if (results.Errors.Count > 0)
                {
                    if (!retried && (results.Errors[0].FileName == null || results.Errors[0].FileName == String.Empty) &&
                        results.Errors[0].Line == 0)
                    {
                        // System.Console.WriteLine("retrying failed compilation");
                        retried = true;
                    }
                    else
                    {
                        complete = true;
                    }
                }
                else
                {
                    complete = true;
                }
            } while (!complete);
            return results;
        }

        public void FinishCompile (IScriptModulePlugin plugin, ScriptData data, IScript Script)
        {
            MRMBase mmb = (MRMBase)Script;
            if (mmb == null)
                return;

            InitializeMRM (plugin, data, mmb, data.Part.LocalId, data.ItemID);
            mmb.Start ();
        }

        public void GetGlobalEnvironment (IScriptModulePlugin plugin, ScriptData data, uint localID, out IWorld world, out IHost host)
        {
            // UUID should be changed to object owner.
            UUID owner = data.World.RegionInfo.EstateSettings.EstateOwner;
            SEUser securityUser = new SEUser (owner, "Name Unassigned");
            SecurityCredential creds = new SecurityCredential (securityUser, data.World);

            world = new World (data.World, creds);
            host = new Host (new SOPObject (data.World, localID, creds), data.World, new ExtensionHandler (plugin.Extensions));
        }

        public void InitializeMRM (IScriptModulePlugin plugin, ScriptData data, MRMBase mmb, uint localID, UUID itemID)
        {
            IWorld world;
            IHost host;

            GetGlobalEnvironment (plugin, data, localID, out world, out host);

            mmb.InitMiniModule (world, host, itemID);
        }
    }
}
