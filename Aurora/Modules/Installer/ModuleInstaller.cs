using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse.StructuredData;
using RunTimeCompiler;

namespace Aurora.Modules.Installer
{
    public class ModuleInstaller : IService
    {
        #region IService Members

        public IConfigSource m_config;
        public IRegistryCore m_registry;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_config = config;
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            MainConsole.Instance.Commands.AddCommand("compile module", 
                "compile module <gui>", 
                "Compiles and adds a given addon-module to Aurora, adding the gui parameter opens a file picker in Windows", consoleCommand);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        private void consoleCommand(string[] commands)
        {
            if (commands[2] == "gui")
            {
                bool finished = false;
                OpenFileDialog dialog = new OpenFileDialog
                                            {
                                                Filter =
                                                    "Build Files (*.am)|*.am|Xml Files (*.xml)|*.xml|Dll Files (*.dll)|*.dll"
                                            };
                System.Threading.Thread t = new System.Threading.Thread(delegate()
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        finished = true;
                    }
                });
                t.SetApartmentState(System.Threading.ApartmentState.STA);
                t.Start();
                while (!finished)
                    System.Threading.Thread.Sleep(10);
                CompileModule(dialog.FileName);
            }
            else
                CompileModule(commands[2]);
        }

        public void CompileModule(string fileName)
        {
            if (Path.GetExtension(fileName) == ".am")
                ReadAMBuildFile(fileName);
            else if (Path.GetExtension(fileName) == ".dll")
                CopyAndInstallDllFile(fileName, Path.GetFileNameWithoutExtension(fileName) + ".dll", null);//Install .dll files
            else
            {
                string tmpFile = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".tmp.xml");
                ReadFileAndCreatePrebuildFile(tmpFile, fileName);
                BuildCSProj(tmpFile);
                CreateAndCompileCSProj(tmpFile, fileName, null);
            }
        }

        private void ReadAMBuildFile(string fileName)
        {
            OSDMap map = (OSDMap)OSDParser.DeserializeJson(File.ReadAllText(fileName));
            string prebuildFile = Path.Combine(Path.GetDirectoryName(fileName), map["PrebuildFile"]);
            string tmpFile = Path.Combine(Path.GetDirectoryName(fileName), map["TmpFile"]);

            ReadFileAndCreatePrebuildFile(tmpFile, prebuildFile);
            BuildCSProj(tmpFile);
            CreateAndCompileCSProj(tmpFile, prebuildFile, map);
            ConfigureModule(Path.GetDirectoryName(fileName), map);
        }

        private void ConfigureModule(string installationPath, OSDMap map)
        {
            bool standaloneSwitch = map["StandaloneSwitch"];
            bool ConsoleConfiguration = map["ConsoleConfiguration"];
            bool useConfigDirectory = true;
            if(standaloneSwitch)
                useConfigDirectory = MainConsole.Instance.Prompt("Are you running this module in standalone or on Aurora.Server?", "Standalone", new List<string>(new[] { "Standalone", "Aurora.Server" })) == "Standalone";
            string configDir = useConfigDirectory ? map["ConfigDirectory"] : map["ServerConfigDirectory"];
            string configurationFinished = map["ConfigurationFinished"];
            string configPath = Path.Combine(Environment.CurrentDirectory, configDir);
            OSDArray config = (OSDArray)map["Configs"];
            foreach (OSD c in config)
            {
                try
                {
                    File.Copy(Path.Combine(installationPath, c.AsString()), Path.Combine(configPath, c.AsString()));
                }
                catch
                {
                }
            }
            if (ConsoleConfiguration)
            {
                OSDMap ConsoleConfig = (OSDMap)map["ConsoleConfig"];
                foreach (KeyValuePair<string, OSD> kvp in ConsoleConfig)
                {
                    string resp = MainConsole.Instance.Prompt(kvp.Key);
                    OSDMap configMap = (OSDMap)kvp.Value;
                    string file = configMap["File"];
                    string Section = configMap["Section"];
                    string ConfigOption = configMap["ConfigOption"];
                    Nini.Ini.IniDocument doc = new Nini.Ini.IniDocument(Path.Combine(configPath, file));
                    doc.Sections[Section].Set(ConfigOption, resp);
                    doc.Save(Path.Combine(configPath, file));
                }
            }
            MainConsole.Instance.Warn(configurationFinished);
        }

        private static void BuildCSProj(string tmpFile)
        {
            Process p = new Process
                            {
                                StartInfo =
                                    new ProcessStartInfo(Path.Combine(Environment.CurrentDirectory, "Prebuild.exe"),
                                                         "/target vs2008 /targetframework v3_5 /file " + tmpFile)
                            };
            p.Start();
            p.WaitForExit();
        }

        private void CreateAndCompileCSProj(string tmpFile, string fileName, OSDMap options)
        {
            File.Delete(tmpFile);
            string projFile = FindProjFile(Path.GetDirectoryName(fileName));
            MainConsole.Instance.Warn("Installing " + Path.GetFileNameWithoutExtension(projFile));
            BasicProject project = ProjectReader.Instance.ReadProject(projFile);
            CsprojCompiler compiler = new CsprojCompiler();
            compiler.Compile(project);
            string dllFile = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(projFile) + ".dll");
            string copiedDllFile = Path.GetFileNameWithoutExtension(projFile) + ".dll";
            if (project.BuildOutput == "Project built successfully!")
            {
                if(options != null)
                    MainConsole.Instance.Warn(options["CompileFinished"]);
                CopyAndInstallDllFile(dllFile, copiedDllFile, options);
            }
            else
                MainConsole.Instance.Warn("Failed to compile the module, exiting! (" + project.BuildOutput + ")");
            
            File.Delete(Path.Combine(Path.GetDirectoryName(tmpFile), "Aurora.sln"));
            File.Delete(Path.Combine(Path.GetDirectoryName(tmpFile), projFile));
            File.Delete(Path.Combine(Path.GetDirectoryName(tmpFile), projFile + ".user"));
            File.Delete(Path.Combine(Path.GetDirectoryName(tmpFile), copiedDllFile));
        }

        private void CopyAndInstallDllFile(string dllFile, string copiedDllFile, OSDMap options)
        {
            try
            {
                File.Copy(dllFile, copiedDllFile);
                if (options != null)
                    MainConsole.Instance.Warn(options["CopyFinished"]);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("Failed to copy the module! (" + ex + ")");
                if (MainConsole.Instance.Prompt("Continue?", "yes", new List<string>(new[] { "yes", "no" })) == "no")
                    return;
            }
            string basePath = Path.Combine(Environment.CurrentDirectory, copiedDllFile);
            LoadModulesFromDllFile(basePath);
            MainConsole.Instance.Warn("Installed the module successfully!");
        }

        private void ReadFileAndCreatePrebuildFile(string tmpFile, string fileName)
        {
            string file = File.ReadAllText(fileName);
            file = file.Replace("<?xml version=\"1.0\" ?>", "<?xml version=\"1.0\" ?>" + Environment.NewLine +
                "<Prebuild version=\"1.7\" xmlns=\"http://dnpb.sourceforge.net/schemas/prebuild-1.7.xsd\">" + Environment.NewLine +
                "  <Solution activeConfig=\"Debug\" name=\"Aurora\" path=\"\" version=\"0.5.0-$Rev$\">" + Environment.NewLine +
                "<Configuration name=\"Debug\" platform=\"x86\">" + Environment.NewLine +
                  @"<Options>
                    <CompilerDefines>TRACE;DEBUG</CompilerDefines>
                    <OptimizeCode>false</OptimizeCode>
                    <CheckUnderflowOverflow>false</CheckUnderflowOverflow>
                    <AllowUnsafe>true</AllowUnsafe>
                    <WarningLevel>4</WarningLevel>
                    <WarningsAsErrors>false</WarningsAsErrors>
                    <OutputPath>bin</OutputPath>
                    <DebugInformation>true</DebugInformation>
                    <IncrementalBuild>true</IncrementalBuild>
                    <NoStdLib>false</NoStdLib>
                  </Options>
                </Configuration>" + Environment.NewLine +
                "<Configuration name=\"Debug\" platform=\"AnyCPU\">" + Environment.NewLine +
      "<Options>" + Environment.NewLine +
        "<target name=\"net-1.1\" description=\"Sets framework to .NET 1.1\">" + Environment.NewLine +
            "<property name=\"nant.settings.currentframework\" value=\"net-1.1\" />" + Environment.NewLine +
        @"</target>
        <CompilerDefines>TRACE;DEBUG</CompilerDefines>
        <OptimizeCode>false</OptimizeCode>
        <CheckUnderflowOverflow>false</CheckUnderflowOverflow>
        <AllowUnsafe>true</AllowUnsafe>
        <WarningLevel>4</WarningLevel>
        <WarningsAsErrors>false</WarningsAsErrors>
        <OutputPath>bin</OutputPath>
        <DebugInformation>true</DebugInformation>
        <IncrementalBuild>true</IncrementalBuild>
        <NoStdLib>false</NoStdLib>
      </Options>
    </Configuration>" + Environment.NewLine +
    "<Configuration name=\"Debug\" platform=\"x64\">" + Environment.NewLine +
      @"<Options>
        <CompilerDefines>TRACE;DEBUG</CompilerDefines>
        <OptimizeCode>false</OptimizeCode>
        <CheckUnderflowOverflow>false</CheckUnderflowOverflow>
        <AllowUnsafe>true</AllowUnsafe>
        <WarningLevel>4</WarningLevel>
        <WarningsAsErrors>false</WarningsAsErrors>
        <OutputPath>bin</OutputPath>
        <DebugInformation>true</DebugInformation>
        <IncrementalBuild>true</IncrementalBuild>
        <NoStdLib>false</NoStdLib>
      </Options>
    </Configuration>" + Environment.NewLine +
    "<Configuration name=\"Release\" platform=\"x86\">" + Environment.NewLine +
      @"<Options>
        <CompilerDefines>TRACE;DEBUG</CompilerDefines>
        <OptimizeCode>true</OptimizeCode>
        <CheckUnderflowOverflow>false</CheckUnderflowOverflow>
        <AllowUnsafe>true</AllowUnsafe>
        <WarningLevel>4</WarningLevel>
        <WarningsAsErrors>false</WarningsAsErrors>
        <SuppressWarnings/>
        <OutputPath>bin</OutputPath>
        <DebugInformation>false</DebugInformation>
        <IncrementalBuild>true</IncrementalBuild>
        <NoStdLib>false</NoStdLib>
      </Options>
    </Configuration>" + Environment.NewLine +
    "<Configuration name=\"Release\" platform=\"AnyCPU\">" + Environment.NewLine +
      @"<Options>
        <CompilerDefines>TRACE;DEBUG</CompilerDefines>
        <OptimizeCode>true</OptimizeCode>
        <CheckUnderflowOverflow>false</CheckUnderflowOverflow>
        <AllowUnsafe>true</AllowUnsafe>
        <WarningLevel>4</WarningLevel>
        <WarningsAsErrors>false</WarningsAsErrors>
        <SuppressWarnings/>
        <OutputPath>bin</OutputPath>
        <DebugInformation>false</DebugInformation>
        <IncrementalBuild>true</IncrementalBuild>
        <NoStdLib>false</NoStdLib>
      </Options>
    </Configuration>" + Environment.NewLine +
    "<Configuration name=\"Release\" platform=\"x64\">" + Environment.NewLine +
      @"<Options>
        <CompilerDefines>TRACE;DEBUG</CompilerDefines>
        <OptimizeCode>true</OptimizeCode>
        <CheckUnderflowOverflow>false</CheckUnderflowOverflow>
        <AllowUnsafe>true</AllowUnsafe>
        <WarningLevel>4</WarningLevel>
        <WarningsAsErrors>false</WarningsAsErrors>
        <SuppressWarnings/>
        <OutputPath>bin</OutputPath>
        <DebugInformation>false</DebugInformation>
        <IncrementalBuild>true</IncrementalBuild>
        <NoStdLib>false</NoStdLib>
      </Options>
    </Configuration>");
            file = file + "</Solution>" + Environment.NewLine + "</Prebuild>";

            file = FixPath(file);
            file = file.Replace("../../../bin/", "../bin");
            file = file.Replace("../../..", "../bin");
            File.WriteAllText(tmpFile, file);
        }

        private void LoadModulesFromDllFile(string copiedDllFile)
        {
            List<IService> services = AuroraModuleLoader.LoadPlugins<IService>(copiedDllFile);
            List<IApplicationPlugin> appPlugins = AuroraModuleLoader.LoadPlugins<IApplicationPlugin>(copiedDllFile);
            List<INonSharedRegionModule> nsregionModule = AuroraModuleLoader.LoadPlugins<INonSharedRegionModule>(copiedDllFile);
            foreach (IService service in services)
            {
                service.Initialize(m_config, m_registry);
                service.Start(m_config, m_registry);
                service.FinishedStartup();
            }
            foreach (IApplicationPlugin plugin in appPlugins)
            {
                plugin.PreStartup(m_registry.RequestModuleInterface<ISimulationBase>());
                plugin.Initialize(m_registry.RequestModuleInterface<ISimulationBase>());
                plugin.PostInitialise();
                plugin.Start();
                plugin.PostStart();
            }
            IRegionModulesController rmc = m_registry.RequestModuleInterface<IRegionModulesController>();
            ISceneManager manager = m_registry.RequestModuleInterface<ISceneManager>();
            if (manager != null)
            {
                foreach (INonSharedRegionModule nsrm in nsregionModule)
                {
                    nsrm.Initialise(m_config);
                    nsrm.AddRegion(manager.Scene);
                    nsrm.RegionLoaded(manager.Scene);
                    rmc.AllModules.Add(nsrm);
                }
            }
        }

        private string FindProjFile(string p)
        {
            string[] files = Directory.GetFiles(p, "*.csproj");
            return files[0];
        }

        private string FixPath(string file)
        {
            string f = "";
            foreach (string line in file.Split('\n'))
            {
                string l = line;
                if (line.StartsWith("<Project frameworkVersion="))
                {
                    string[] lines = line.Split(new[] { "path=\"" }, StringSplitOptions.RemoveEmptyEntries);
                    string li = "";
                    int i = 0;
                    foreach(string ll in lines[1].Split('"'))
                    {
                        if (i > 0)
                            li += ll + "\"";
                        i++;
                    }
                    l = lines[0] + "path=\"./\" " + li.Remove(li.Length-1);
                }
                f += l;
            }
            return f;
        }
    }
}
