/*
 * No license given,
 * taken from http://csharper.fairblog.ro/2010/05/compiling-c-projects-at-runtime-parsing-the-csproj-file 
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace RunTimeCompiler
{
    /// <summary>
    ///     This class should be able to process all (almost) kinds of .csproj files
    ///     and retrieve the data needed for UI and compilation.
    ///     It should succesfully process .csproj files comming from Visual Studio
    ///     2005+.
    ///     It proccesses ok files from Visual Studio 2010 (this is the most recent
    ///     version), but the structure of the .csproj may change in future versions
    ///     of Visual Studio, and therefore the class may not be able to process that
    ///     .csproj files.
    ///     It is also unknown if .csproj files created with Visual Studio 2001 or 2003
    ///     can be processed.
    /// </summary>
    public class AllCsprojReader
    {
        /// <summary>
        ///     It is used to retrieve the value of a property from the
        ///     generic properties section (as opposite to
        ///     configuration-specific section).
        /// </summary>
        /// <param name="doc">Generic section (xml node)</param>
        /// <param name="mgr"></param>
        /// <param name="property">The name of the property/setting</param>
        /// <returns></returns>
        private string GetPropertyValue(XmlDocument doc, XmlNamespaceManager mgr, string property)
        {
            //TODO: Review this cod as here may be a bug. 
            //It retrieves the node that defines that property, but it does not check
            //if the node is in the general section, not in one of the 
            //configuration-specific sections. 
            //Luckily it "appears" that general settings are not present in the 
            //configuration-specific sections, but that needs to be confirmed (at least 
            //for the properties currently retrieved) or the
            //cod needs to be changed.
            XmlNode node;
            node = doc.SelectSingleNode(string.Format("/x:Project/x:PropertyGroup/x:{0}", property), mgr);
            if (node == null) return string.Empty;
            return node.InnerText;
        }

        /// <summary>
        ///     It is used to get the node that contains the configuraiton-specific settings
        ///     for the Configuration and Platform specified.
        ///     Currently it looks for the section with the "Condition" attribute like:
        ///     '$(Configuration)|$(Platform)' == '[Configuration]|[Platform]'
        ///     The node should look like:
        ///     &lt; PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' " &gt;
        ///     ...
        ///     &lt; PropertyGroup /&gt;
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mgr"></param>
        /// <param name="configuration"></param>
        /// <param name="platform"></param>
        /// <returns></returns>
        private XmlNode GetCrtConfigurationNode(XmlDocument doc, XmlNamespaceManager mgr, string configuration,
                                                string platform)
        {
            //TODO: Review this code and make it less "hard-coded".
            //The current version of the code is "working" with all .csproj I could 
            //find/generate, but a less hard-coded version will be nicer.
            XmlNodeList nodes;
            string lookForValue;
            lookForValue = string.Format("'$(Configuration)|$(Platform)' == '{0}|{1}'", configuration, platform);
            nodes = doc.SelectNodes("/x:Project/x:PropertyGroup", mgr);
            return (from XmlNode node in nodes
                    where node.Attributes != null
                    let attribute = node.Attributes["Condition"]
                    where attribute != null && !string.IsNullOrEmpty(attribute.Value)
                    where attribute.Value.Trim() == lookForValue
                    select node).FirstOrDefault();
        }

        /// <summary>
        ///     It is used to retrieve the value of a property from the
        ///     configuration-specific section.
        /// </summary>
        /// <param name="crtConfigurationNode">Configuration-specific section (xml node)</param>
        /// <param name="mgr"></param>
        /// <param name="property">The name of the property/setting</param>
        /// <returns></returns>
        private string GetCrtConfigurationValue(XmlNode crtConfigurationNode, XmlNamespaceManager mgr, string property)
        {
            XmlNode node;
            if (crtConfigurationNode == null) return string.Empty;
            node = crtConfigurationNode.SelectSingleNode(string.Format("./x:{0}", property), mgr);
            if (node == null) return string.Empty;
            return node.InnerText;
        }

        /// <summary>
        ///     Gets the list of referenced assemblies.
        ///     Referenced assemblies are found in:
        ///     &lt; project &gt; &lt; ItemGroup &gt; &lt; Reference &gt;
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mgr"></param>
        /// <returns></returns>
        private List<string> GetReferences(XmlDocument doc, XmlNamespaceManager mgr)
        {
            XmlNodeList nodes;
            nodes = doc.SelectNodes("/x:Project/x:ItemGroup/x:Reference", mgr);
            return (from XmlNode child in nodes select child.Attributes["Include"].InnerText).ToList();
        }

        /// <summary>
        ///     Gets the content files.
        ///     Content files are found in:
        ///     &lt; project &gt; &lt; ItemGroup &gt; &lt; Content &gt;
        ///     Then:
        ///     get name from the atribute "Include",
        ///     get CopyToOutputDirectory from child element "CopyToOutputDirectory"
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mgr"></param>
        /// <returns></returns>
        private List<ProjectContentFile> GetContent(XmlDocument doc, XmlNamespaceManager mgr)
        {
            XmlNodeList nodes;
            List<ProjectContentFile> files;
            XmlNode node;
            string filename;
            int index;
            ProjectContentFile file;
            files = new List<ProjectContentFile>();
            nodes = doc.SelectNodes("/x:Project/x:ItemGroup/x:Content", mgr);
            foreach (XmlNode child in nodes)
            {
                file = new ProjectContentFile();
                filename = child.Attributes["Include"].InnerText;
                index = -1;
                if (filename.Contains(Path.DirectorySeparatorChar.ToString()))
                {
                    index = filename.LastIndexOf(Path.DirectorySeparatorChar);
                }
                file.Name = filename.Substring(index + 1);
                file.Location = filename.Substring(0, index + 1);
                node = child.SelectSingleNode("./x:CopyToOutputDirectory", mgr);
                if (node != null)
                {
                    file.CopyToOutputDirectory = true;
                }
                files.Add(file);
            }
            return files;
        }

        /// <summary>
        ///     Gets the source files.
        ///     Source files are found in:
        ///     &lt; project &gt; &lt; ItemGroup &gt; &lt; Compile &gt;
        ///     Then:
        ///     get name from the atribute "Include",
        ///     get DependentUpon from child element "DependentUpon",
        ///     get CopyToOutputDirectory from child element "CopyToOutputDirectory"
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mgr"></param>
        /// <returns></returns>
        private List<ProjectSourceFile> GetSources(XmlDocument doc, XmlNamespaceManager mgr)
        {
            XmlNodeList nodes;
            List<ProjectSourceFile> files;
            ProjectSourceFile file;
            XmlNode node;
            string filename;
            int index;
            files = new List<ProjectSourceFile>();
            nodes = doc.SelectNodes("/x:Project/x:ItemGroup/x:Compile", mgr);
            foreach (XmlNode child in nodes)
            {
                file = new ProjectSourceFile();
                filename = child.Attributes["Include"].InnerText;
                index = -1;
                if (filename.Contains(Path.DirectorySeparatorChar.ToString()))
                {
                    index = filename.LastIndexOf(Path.DirectorySeparatorChar);
                }
                file.Name = filename.Substring(index + 1);
                file.Location = filename.Substring(0, index + 1);
                node = child.SelectSingleNode("./x:DependentUpon", mgr);
                if (node != null)
                {
                    file.DependentUpon = node.InnerText;
                }
                node = child.SelectSingleNode("./x:CopyToOutputDirectory", mgr);
                if (node != null)
                {
                    file.CopyToOutputDirectory = true;
                }
                files.Add(file);
            }
            return files;
        }

        /// <summary>
        ///     Gets the resource files.
        ///     Resource files are found in:
        ///     &lt; project &gt; &lt; ItemGroup &gt; &lt; EmbeddedResource &gt;
        ///     Then:
        ///     get name from the atribute "Include",
        ///     get DependentUpon from child element "DependentUpon",
        ///     get CopyToOutputDirectory from child element "CopyToOutputDirectory"
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mgr"></param>
        /// <returns></returns>
        private List<ProjectResourceFile> GetResources(XmlDocument doc, XmlNamespaceManager mgr)
        {
            XmlNodeList nodes;
            List<ProjectResourceFile> files;
            ProjectResourceFile file;
            XmlNode node;
            string filename;
            int index;
            files = new List<ProjectResourceFile>();
            nodes = doc.SelectNodes("/x:Project/x:ItemGroup/x:EmbeddedResource", mgr);
            foreach (XmlNode child in nodes)
            {
                file = new ProjectResourceFile();
                filename = child.Attributes["Include"].InnerText;
                index = -1;
                if (filename.Contains(Path.DirectorySeparatorChar.ToString()))
                {
                    index = filename.LastIndexOf(Path.DirectorySeparatorChar);
                }
                file.Name = filename.Substring(index + 1);
                file.Location = filename.Substring(0, index + 1);
                node = child.SelectSingleNode("./x:DependentUpon", mgr);
                if (node != null)
                {
                    file.DependentUpon = node.InnerText;
                }
                node = child.SelectSingleNode("./x:CopyToOutputDirectory", mgr);
                if (node != null)
                {
                    file.CopyToOutputDirectory = true;
                }
                files.Add(file);
            }
            return files;
        }

        /// <summary>
        ///     Gets the config files.
        ///     Config files are found in:
        ///     &lt; project &gt; &lt; ItemGroup &gt; &lt; None &gt;
        ///     Then:
        ///     get name from the atribute "Include",
        ///     get CopyToOutputDirectory from child element "CopyToOutputDirectory"
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mgr"></param>
        /// <returns></returns>
        private List<ProjectConfigFile> GetConfigs(XmlDocument doc, XmlNamespaceManager mgr)
        {
            XmlNodeList nodes;
            XmlNode node;
            List<ProjectConfigFile> files;
            ProjectConfigFile file;
            string filename;
            int index;
            files = new List<ProjectConfigFile>();
            nodes = doc.SelectNodes("/x:Project/x:ItemGroup/x:None", mgr);
            foreach (XmlNode child in nodes)
            {
                file = new ProjectConfigFile();
                filename = child.Attributes["Include"].InnerText;
                index = -1;
                if (filename.Contains(Path.DirectorySeparatorChar.ToString()))
                {
                    index = filename.LastIndexOf(Path.DirectorySeparatorChar);
                }
                file.Name = filename.Substring(index + 1);
                file.Location = filename.Substring(0, index + 1);
                node = child.SelectSingleNode("./x:CopyToOutputDirectory", mgr);
                if (node != null)
                {
                    file.CopyToOutputDirectory = true;
                }
                files.Add(file);
            }
            return files;
        }

        /// <summary>
        ///     It is used to read the project settings.
        ///     It reads general settings (framework version, proect type
        ///     winexe/dll/console...,assembly name etc) and the settings
        ///     for the active configuration (debug/release...) (output folder,
        ///     warning level etc).
        ///     Notes!
        ///     1. Form the genereal section it gets the Configuration and Platform
        ///     (aka Debug/Relese, AnyCPU) and the searches for the section having the
        ///     condition:
        ///     '$(Configuration)|$(Platform)' == '[Configuration]|[Platform]'
        ///     That section is used to get configuraiton-specific settings.
        ///     2. Some important settings like .Net framework version and file alignment
        ///     were added after .Net 2.0. So when these nodes are missing the values "v2.0"
        ///     and "512" are used.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="mgr"></param>
        /// <returns></returns>
        private ProjectSettings GetSettings(XmlDocument doc, XmlNamespaceManager mgr)
        {
            ProjectSettings settings;
            settings = new ProjectSettings();
            string configuration = string.Empty;
            string platform = string.Empty;
            XmlNode crtConfigurationNode;
            //Get Configuration and Platform as these willl be needed to get
            //the active configuration.
            configuration = GetPropertyValue(doc, mgr, "Configuration");
            platform = GetPropertyValue(doc, mgr, "Platform");
            //Get the active configuration.
            crtConfigurationNode = GetCrtConfigurationNode(doc, mgr, configuration, platform);
            //Retrieve settings.
            settings.AppDesignerFolder = GetPropertyValue(doc, mgr, "AppDesignerFolder");
            settings.AssemblyName = GetPropertyValue(doc, mgr, "AssemblyName");
            settings.CompilationConstants = GetCrtConfigurationValue(crtConfigurationNode, mgr, "DefineConstants");
            //FileAlignment was added after .Net 2.0, so it is missing 
            //in projects created with Visual Studio 2005. The default 
            //(512) should be used.
            settings.FileAlignment = GetPropertyValue(doc, mgr, "FileAlignment");
            if (string.IsNullOrEmpty(settings.FileAlignment)) settings.FileAlignment = "512";
            settings.Optimize = GetCrtConfigurationValue(crtConfigurationNode, mgr, "Optimize");
            settings.OutputPath = GetCrtConfigurationValue(crtConfigurationNode, mgr, "OutputPath");
            settings.OutputType = GetPropertyValue(doc, mgr, "OutputType");
            settings.ProductVersion = GetPropertyValue(doc, mgr, "ProductVersion");
            settings.ProjectGuid = GetPropertyValue(doc, mgr, "ProjectGuid");
            settings.RootNamespace = GetPropertyValue(doc, mgr, "RootNamespace");
            settings.SchemaVersion = GetPropertyValue(doc, mgr, "SchemaVersion");
            //TargetFrameworkVersion was added after .Net 2.0, so it is 
            //missing in projects created with Visual Studio 2005. 
            //The default (v2.0) should be used.
            settings.TargetFrameworkVersion = GetPropertyValue(doc, mgr, "TargetFrameworkVersion");
            if (string.IsNullOrEmpty(settings.TargetFrameworkVersion)) settings.TargetFrameworkVersion = "v2.0";
            settings.WarningLevel = GetCrtConfigurationValue(crtConfigurationNode, mgr, "WarningLevel");
            return settings;
        }

        /// <summary>
        ///     This method is called to read the specified .csproj file and retrieve all
        ///     the data needed for UI or compilation.
        ///     It loads the project file as an XML and then get the value of the
        ///     relevant nodes.
        ///     Note!
        ///     It does not get every information availbale in .csproj. It only retrieves
        ///     the information considered relevant for UI and compilation.
        ///     Attention!
        ///     There may be information in .csproj that are important for the compilation
        ///     and I may be unaware of these or I may deliberately choose to ignore them.
        ///     For example, I decided not to support projects that contain references to
        ///     other projects.
        /// </summary>
        /// <param name="filename">The name (and path) of the .csproj file.</param>
        /// <returns>The data needed for UI and compilation.</returns>
        public BasicProject ReadProject(string filename)
        {
            XmlDocument doc;
            XmlNamespaceManager mgr;
            BasicProject basicProject = null;
            doc = new XmlDocument();
            doc.Load(filename);
            mgr = new XmlNamespaceManager(doc.NameTable);
            mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");
            basicProject = new BasicProject
                               {
                                   Settings = GetSettings(doc, mgr),
                                   References = GetReferences(doc, mgr),
                                   ContentFiles = GetContent(doc, mgr),
                                   SourceFiles = GetSources(doc, mgr),
                                   ResourceFiles = GetResources(doc, mgr),
                                   ConfigFiles = GetConfigs(doc, mgr),
                                   ProjectFile = filename,
                                   ProjectFolder = Path.GetDirectoryName(filename)
                               };
            return basicProject;
        }
    }
}