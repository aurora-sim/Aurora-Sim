/*
 * No license given,
 * taken from http://csharper.fairblog.ro/2010/05/compiling-c-projects-at-runtime-parsing-the-csproj-file 
 */

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.CSharp;

namespace RunTimeCompiler
{
    /// <summary>
    ///     Compiles a csproj.
    ///     I should probably define an interface and access the functionality available here
    ///     via ProjectReader or a similair class.
    /// </summary>
    internal class CsprojCompiler
    {
        /// <summary>
        ///     This method compiles the poroject specified as parameter.
        ///     It can only be used for CSharp projects, but can be modified to support
        ///     some other .Net project types.
        /// </summary>
        /// <param name="project"></param>
        public void Compile(BasicProject project)
        {
            CodeDomProvider codeProvider;
            CompilerParameters parameters = new CompilerParameters();
            Dictionary<string, string> providerOptions = new Dictionary<string, string>();
            string[] sources;
            string buildMessage;
            providerOptions.Add("CompilerVersion", project.Settings.TargetFrameworkVersion);
            //I use CSharpCodeProvider instead of 
            //CodeDomProvider.CreateProvider("CSharp", providerOptions) as it is only
            //available starting with .Net 4.0
            //Also CodeDomProvider.CreateProvider("CSharp") does not allows setting the
            //compiler version
            codeProvider = new CSharpCodeProvider(providerOptions);
            parameters.GenerateExecutable = IsExe(project);
            parameters.OutputAssembly = GetOutputFilename(project);
            parameters.WarningLevel = project.Settings.WarningLevel == "" ? 0 : int.Parse(project.Settings.WarningLevel);
            parameters.TreatWarningsAsErrors = false;
            parameters.GenerateInMemory = false;
            parameters.CompilerOptions = GetCompilerOptions(project);
            parameters.ReferencedAssemblies.AddRange(GetReferences(project));
            //parameters.EmbeddedResources.Add("[Resources.resources]");
            sources = ReadSourceFiles(project);
            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, sources);
            if (results.Errors.Count > 0)
            {
                buildMessage = results.Errors.Cast<CompilerError>()
                                      .Aggregate(string.Empty,
                                                 (current, CompErr) =>
                                                 current + "Line number " + CompErr.Line + ", Error Number: " +
                                                 CompErr.ErrorNumber + ", '" + CompErr.ErrorText + ";" +
                                                 Environment.NewLine + Environment.NewLine);

                project.BuildOutput = buildMessage;
                MessageBox.Show(buildMessage);
            }
            else
            {
                //CompileSatelliteAssemblies();
                buildMessage = "Project built successfully!";
                project.BuildOutput = buildMessage;
            }
        }

        /// <summary>
        ///     This method is used to get the list of references to be specified in the
        ///     CompilerParameters for a CodeDomProvider.
        ///     It should get the fully qualified names of each reference, but a simple
        ///     name (with the .dll extension)  may be enough in most cases.
        ///     The current implementation appears to "work ok" with
        ///     very simple applications but it has two problems:
        ///     1) It returns the name of the file and not the fully qualified name.
        ///     2) It assumes the name of the file is the assembly title plus the
        ///     ".dll" extension.
        ///     A better implementation is needed.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private string[] GetReferences(BasicProject project)
        {
            //TODO: Correct this code as the name of the file is not guaranted to be +".dll"!
            string[] resources = new string[project.References.Count];
            for (int i = 0; i < project.References.Count; i++)
            {
                resources[i] = project.References[i] + ".dll";
            }
            return resources;
        }

        /// <summary>
        ///     The method is used to provide the source code for the CodeDomProvider.
        ///     It reads the content of the source files and returns it.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private string[] ReadSourceFiles(BasicProject project)
        {
            string filename;
            string[] sources = new string[project.SourceFiles.Count];
            for (int i = 0; i < project.SourceFiles.Count; i++)
            {
                filename = Path.Combine(project.ProjectFolder, project.SourceFiles[i].Location);
                filename = Path.Combine(filename, project.SourceFiles[i].Name);
                sources[i] = File.ReadAllText(filename);
            }
            return sources;
        }

        /// <summary>
        ///     This method is used to get the compiler oprions to be specified
        ///     in the CompilerParameters for a CodeDomProvider.
        ///     It determines the compiler options based on the settings from the csproj file.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private string GetCompilerOptions(BasicProject project)
        {
            string compilerOptions = string.Empty;
            if (project.Settings.Optimize.ToLowerInvariant() == "true")
                compilerOptions += @"/optimize ";
            return compilerOptions;
        }

        /// <summary>
        ///     This method is used to get GenerateExecutable settings to be specified
        ///     in the CompilerParameters for a CodeDomProvider.
        ///     It returns true if the OutputType specified in the csproj file is winexe or exe.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private bool IsExe(BasicProject project)
        {
            return project.Settings.OutputType.ToLowerInvariant() == "winexe"
                   | project.Settings.OutputType.ToLowerInvariant() == "exe";
        }

        /// <summary>
        ///     It gets the absolute path to the output folder.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private string GetOutputPath(BasicProject project)
        {
            return Path.Combine(project.ProjectFolder, project.Settings.OutputPath);
        }

        /// <summary>
        ///     This method is used to get OutputAssembly settings to be specified
        ///     in the CompilerParameters for a CodeDomProvider.
        ///     It returns the absolute path where to place the compiled assembly.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        private string GetOutputFilename(BasicProject project)
        {
            string filename = project.Settings.AssemblyName + (IsExe(project) ? ".exe" : ".dll");
            return Path.Combine(GetOutputPath(project), filename);
        }
    }
}