/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nini.Config;
using Nini.Ini;

namespace Aurora.Framework
{
    /// <summary>
    ///   Loads the Configuration files into nIni
    /// </summary>
    public class ConfigurationLoader
    {
        public string defaultIniFile = "Aurora.ini";

        public string iniFilePath = "";

        /// <summary>
        ///   Should we save all merging of the .ini files to the filesystem?
        /// </summary>
        protected bool inidbg;

        public Dictionary<string, string> m_defines = new Dictionary<string, string>();

        /// <summary>
        ///   Should we show all the loading of the config files?
        /// </summary>
        protected bool showIniLoading;

        /// <summary>
        ///   Loads the region configuration
        /// </summary>
        /// <param name = "argvSource">Parameters passed into the process when started</param>
        /// <returns>A configuration that gets passed to modules</returns>
        public IConfigSource LoadConfigSettings(IConfigSource argvSource)
        {
            iniFilePath = "";
            bool iniFileExists = false;
            bool oldoptions = false;

            string mainIniDirectory = "";
            string mainIniFileName = defaultIniFile;
            string secondaryIniFileName = "";

            List<string> sources = new List<string>();
            string basePath = Util.configDir();

            if (argvSource != null)
            {
                IConfig startupConfig = argvSource.Configs["Startup"];


                oldoptions =
                    startupConfig.GetBoolean("oldoptions", false);

                inidbg =
                    startupConfig.GetBoolean("inidbg", inidbg);

                showIniLoading =
                    startupConfig.GetBoolean("inishowfileloading", showIniLoading);

                if (oldoptions)
                {
                    string masterFileName =
                        startupConfig.GetString("inimaster", String.Empty);

                    string iniGridName =
                        startupConfig.GetString("inigrid", String.Empty);

                    if (iniGridName == string.Empty) //Read the old name then
                        iniGridName =
                            startupConfig.GetString("inifile", String.Empty);

                    string iniSimName =
                        startupConfig.GetString("inisim", defaultIniFile);

                    //Be mindful of these when modifying...
                    //1) When file A includes file B, if the same directive is found in both, that the value in file B wins.
                    //2) That inifile may be used with or without inimaster being used.
                    //3) That any values for directives pulled in via inifile (Config Set 2) override directives of the same name found in the directive set (Config Set 1) created by reading in bin/Aurora.ini and its subsequently included files or that created by reading in whatever file inimaster points to and its subsequently included files.

                    if (IsUri(masterFileName))
                    {
                        if (!sources.Contains(masterFileName))
                            sources.Add(masterFileName);
                    }
                    else
                    {
                        string masterFilePath = Util.BasePathCombine(masterFileName);

                        if (masterFileName != String.Empty &&
                            File.Exists(masterFilePath) &&
                            (!sources.Contains(masterFilePath)))
                            sources.Add(masterFilePath);
                        if (iniGridName == "") //Then it doesn't exist and we need to set this
                            iniFilePath = masterFilePath;
                        if (iniSimName == "") //Then it doesn't exist and we need to set this
                            iniFilePath = masterFilePath;
                    }

                    if (iniGridName != "")
                    {
                        if (IsUri(iniGridName))
                        {
                            if (!sources.Contains(iniGridName))
                                sources.Add(iniGridName);
                            iniFilePath = iniGridName;
                        }
                        else
                        {
                            iniFilePath = Util.BasePathCombine(iniGridName);

                            if (File.Exists(iniFilePath))
                            {
                                if (!sources.Contains(iniFilePath))
                                    sources.Add(iniFilePath);
                            }
                        }
                    }

                    if (iniSimName != "")
                    {
                        if (IsUri(iniSimName))
                        {
                            if (!sources.Contains(iniSimName))
                                sources.Add(iniSimName);
                            iniFilePath = iniSimName;
                        }
                        else
                        {
                            iniFilePath = Util.BasePathCombine(iniSimName);

                            if (File.Exists(iniFilePath))
                            {
                                if (!sources.Contains(iniFilePath))
                                    sources.Add(iniFilePath);
                            }
                        }
                    }

                    string iniDirName =
                        startupConfig.GetString("inidirectory", "");

                    if (iniDirName != "" && Directory.Exists(iniDirName))
                    {
                        Console.WriteLine(string.Format("Searching folder {0} for config ini files",
                                         iniDirName));

                        string[] fileEntries = Directory.GetFiles(iniDirName);
#if (!ISWIN)
                        foreach (string filePath in fileEntries)
                        {
                            string extension = Path.GetExtension(filePath);
                            if (extension != null && extension.ToLower() == ".ini")
                            {
                                if (!sources.Contains(Path.Combine(iniDirName, filePath)))
                                    sources.Add(Path.Combine(iniDirName, filePath));
                            }
                        }
#else
                    foreach (string filePath in fileEntries.Where(filePath =>
                                                                      {
                                                                          var extension = Path.GetExtension(filePath);
                                                                          return extension != null && extension.ToLower() == ".ini";
                                                                      }).Where(filePath => !sources.Contains(Path.Combine(iniDirName, filePath))))
                    {
                        sources.Add(Path.Combine(iniDirName, filePath));
                    }
#endif
                    }
                }
                else
                {
                    mainIniDirectory = startupConfig.GetString("mainIniDirectory", "");
                    mainIniFileName = startupConfig.GetString("mainIniFileName", defaultIniFile);
                    secondaryIniFileName = startupConfig.GetString("secondaryIniFileName", "");
                }
            }

            if (!oldoptions)
            {
                if (mainIniDirectory != "")
                    basePath = mainIniDirectory;
                if (mainIniFileName != "")
                {
                    if (IsUri(mainIniFileName))
                    {
                        if (!sources.Contains(mainIniFileName))
                            sources.Add(mainIniFileName);
                    }
                    else
                    {
                        string mainIniFilePath = Path.Combine(mainIniDirectory, mainIniFileName);
                        if (!sources.Contains(mainIniFilePath))
                            sources.Add(mainIniFilePath);
                    }
                }

                if (secondaryIniFileName != "")
                {
                    if (IsUri(secondaryIniFileName))
                    {
                        if (!sources.Contains(secondaryIniFileName))
                            sources.Add(secondaryIniFileName);
                    }
                    else
                    {
                        string secondaryIniFilePath = Path.Combine(mainIniDirectory, secondaryIniFileName);
                        if (!sources.Contains(secondaryIniFilePath))
                            sources.Add(secondaryIniFilePath);
                    }
                }
            }

            IConfigSource m_config = new IniConfigSource();
            IConfigSource m_fakeconfig = new IniConfigSource();

            //Console.WriteLine(string.Format("[Config]: Reading configuration settings"));

            if (sources.Count == 0)
            {
                Console.WriteLine(string.Format("[CONFIG]: Could not load any configuration"));
                Console.WriteLine(string.Format("[CONFIG]: Did you copy the " + defaultIniFile + ".example file to " + defaultIniFile +
                                  "?"));
                throw new NotSupportedException();
            }

            List<string> triedPaths = new List<string>();
            for (int i = 0; i < sources.Count; i++)
            {
                //Read all non .example files first, then read all the example ones

                if (File.Exists(sources[i]) &&
                    ReadConfig(sources[i], i, m_fakeconfig))
                    iniFileExists = true;
                else if (File.Exists(sources[i] + ".example") &&
                         ReadConfig(sources[i] + ".example", i, m_fakeconfig))
                    iniFileExists = true;
                AddIncludes(sources, basePath, ref i, ref triedPaths, m_fakeconfig);
            }

            //
            for (int i = 0; i < sources.Count; i++)
            {
                //Read all non .example files first, then read all the example ones

                if (File.Exists(sources[i]))
                    ReadConfig(sources[i], i, m_config);
                else if (File.Exists(sources[i] + ".example"))
                    ReadConfig(sources[i] + ".example", i, m_config);
            }

            FixDefines(ref m_config);

            if (!iniFileExists)
            {
                Console.WriteLine(string.Format("[CONFIG]: Could not load any configuration"));
                Console.WriteLine(string.Format("[CONFIG]: Configuration exists, but there was an error loading it!"));
                throw new NotSupportedException();
            }
            // Make sure command line options take precedence
            if(argvSource != null)
                m_config.Merge(argvSource);

            return m_config;
        }

        private void FixDefines(ref IConfigSource m_config)
        {
            if (m_defines.Count == 0)
                return;

            foreach (IConfig config in m_config.Configs)
            {
                int i = 0;
                foreach (string value in config.GetValues())
                {
                    string value1 = value;
                    foreach (string newValue in from def in m_defines.Keys where value1.Contains(def) select value1.Replace(def, m_defines[def]))
                    {
                        config.Set(config.GetKeys()[i], newValue);
                    }
                    i++;
                }
            }
        }

        /// <summary>
        ///   Adds the included files as ini configuration files
        /// </summary>
        /// <param name = "sources">List of URL strings or filename strings</param>
        /// <param name = "cntr">Where should we start inserting sources into the list?</param>
        private void AddIncludes(List<string> sources, string basePath, ref int cntr, ref List<string> triedPaths,
                                 IConfigSource configSource)
        {
            int cn = cntr;
            //Where should we insert the sources into the list?
            //loop over config sources
            foreach (IConfig config in configSource.Configs)
            {
                // Look for Include-* in the key name
                string[] keys = config.GetKeys();
                foreach (string k in keys)
                {
                    if (k.StartsWith("Define-"))
                    {
                        if (!m_defines.ContainsKey(k.Remove(0, 7)))
                            m_defines.Add(k.Remove(0, 7), config.GetString(k));
                    }
                    else if (k.StartsWith("Include-"))
                    {
                        // read the config file to be included.
                        string file = config.GetString(k);
                        if (triedPaths.Contains(file))
                            continue;
                        triedPaths.Add(file);
                        if (IsUri(file))
                        {
                            if (!sources.Contains(file))
                            {
                                cn++;
                                sources.Insert(cn, file);
                            }
                        }
                        else
                        {
                            // Resolve relative paths with wildcards
                            string chunkWithoutWildcards = file;
                            string chunkWithWildcards = string.Empty;
                            int wildcardIndex = file.IndexOfAny(new[] {'*', '?'});
                            if (wildcardIndex != -1)
                            {
                                chunkWithoutWildcards = file.Substring(0, wildcardIndex);
                                chunkWithWildcards = file.Substring(wildcardIndex);
                            }
                            string path = Path.Combine(basePath, chunkWithoutWildcards + chunkWithWildcards);
                            List<string> paths = new List<string>(new string[1] {path});
                            if (path.Contains("*"))
                                if (path.Contains("*.ini"))
                                {
                                    paths.AddRange(Util.GetSubFiles(path));
                                    List<string> examplefiles =
                                        new List<string>(Util.GetSubFiles(path.Replace(".ini", ".ini.example")));
#if (!ISWIN)
                                    examplefiles.RemoveAll(delegate(string s)
                                    {
                                        return paths.Contains(s.Replace(".example", ""));
                                    });
#else
                                    examplefiles.RemoveAll(
                                        s => paths.Contains(s.Replace(".example", "")));
#endif
                                    paths.AddRange(examplefiles);
                                }
                                else
                                    paths.AddRange(Util.GetSubFiles(path));
#if (!ISWIN)
                            foreach (string p in paths)
                            {
                                if (!sources.Contains(p))
                                {
                                    cn++;
                                    sources.Insert(cn, p);
                                }
                            }
#else
                            foreach (string p in paths.Where(p => !sources.Contains(p)))
                            {
                                cn++;
                                sources.Insert(cn, p);
                            }
#endif
                        }
                    }
                    else if (k.StartsWith("RemoveInclude-"))
                    {
                        // read the config file to be included.
                        string file = config.GetString(k);
                        if (triedPaths.Contains(file))
                            continue;
                        triedPaths.Add(file);
                        if (IsUri(file))
                        {
                            if (!sources.Contains(file))
                            {
                                cn--;
                                sources.Remove(file);
                            }
                        }
                        else
                        {
                            // Resolve relative paths with wildcards
                            string chunkWithoutWildcards = file;
                            string chunkWithWildcards = string.Empty;
                            int wildcardIndex = file.IndexOfAny(new[] {'*', '?'});
                            if (wildcardIndex != -1)
                            {
                                chunkWithoutWildcards = file.Substring(0, wildcardIndex);
                                chunkWithWildcards = file.Substring(wildcardIndex);
                            }
                            string path = Path.Combine(basePath, chunkWithoutWildcards + chunkWithWildcards);
                            string[] paths = new string[1] {path};
                            if (path.Contains("*"))
                                paths = Util.GetSubFiles(path);
#if (!ISWIN)
                            foreach (string p in paths)
                            {
                                if (!sources.Contains(p))
                                {
                                    cn--;
                                    sources.Remove(p);
                                }
                            }
#else
                            foreach (string p in paths.Where(p => !sources.Contains(p)))
                            {
                                cn--;
                                sources.Remove(p);
                            }
#endif
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Check if we can convert the string to a URI
        /// </summary>
        /// <param name = "file">String uri to the remote resource</param>
        /// <returns>true if we can convert the string to a Uri object</returns>
        private bool IsUri(string file)
        {
            Uri configUri;

            return Uri.TryCreate(file, UriKind.Absolute,
                                 out configUri) && configUri.Scheme == Uri.UriSchemeHttp;
        }

        /// <summary>
        ///   Provide same ini loader functionality for standard ini and master ini - file system or XML over http
        /// </summary>
        /// <param name = "iniPath">Full path to the ini</param>
        /// <returns></returns>
        private bool ReadConfig(string iniPath, int i, IConfigSource source)
        {
            bool success = false;

            if (!IsUri(iniPath))
            {
                if (showIniLoading)
                    Console.WriteLine(string.Format("[CONFIG]: Reading configuration file {0}", Util.BasePathCombine(iniPath)));

                source.Merge(new IniConfigSource(iniPath, IniFileType.AuroraStyle));
                if (inidbg)
                {
                    WriteConfigFile(i, source);
                }
                success = true;
            }
            else
            {
                // The ini file path is a http URI
                // Try to read it
                try
                {
                    string file = Utilities.ReadExternalWebsite(iniPath);
                    string filename = Path.GetTempFileName();
                    File.WriteAllText(filename, file);

                    if (showIniLoading)
                        Console.WriteLine(string.Format("[CONFIG]: Reading configuration file {0}", Util.BasePathCombine(iniPath)));

                    source.Merge(new IniConfigSource(filename, IniFileType.AuroraStyle));
                    if (inidbg)
                    {
                        WriteConfigFile(i, source);
                    }
                    File.Delete(filename);
                    success = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("[CONFIG]: Exception reading config from URI {0}\n" + e, iniPath));
                    Environment.Exit(1);
                }
            }
            return success;
        }

        private void WriteConfigFile(int i, IConfigSource m_config)
        {
            string m_fileName = "ConfigFileDump" + i + ".ini";
            Console.WriteLine(string.Format("Writing config dump file to " + m_fileName));
            try
            {
                //Add the user
                FileStream stream = new FileStream(m_fileName, FileMode.Create);
                StreamWriter m_streamWriter = new StreamWriter(stream);
                m_streamWriter.BaseStream.Position += m_streamWriter.BaseStream.Length;
                m_streamWriter.WriteLine(m_config.ToString());
                m_streamWriter.Close();
            }
            catch
            {
            }
        }
    }
}