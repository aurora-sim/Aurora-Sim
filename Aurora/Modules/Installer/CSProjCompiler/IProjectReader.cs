/*
 * No license given,
 * taken from http://csharper.fairblog.ro/2010/05/compiling-c-projects-at-runtime-parsing-the-csproj-file 
 */

namespace RunTimeCompiler
{
    /// <summary>
    ///     This interface defines a way of working with project-reader.
    ///     Currently the only project-reader developed is the one for c# projects,
    ///     but many other readers can be developes for various languages/IDEs.
    /// </summary>
    public interface IProjectReader
    {
        /// <summary>
        ///     This method will be used to check if the specified project file
        ///     can be opened.
        ///     Implementations may involve operations like: checking the file extension,
        ///     reading the file header (or some specific sectors) and getting
        ///     the file version.
        ///     Note!
        ///     If the call returns true it does not guaranties calls to ReadProject
        ///     will be successfull.
        /// </summary>
        /// <param name="filename">
        ///     The name of the project file. It should include
        ///     path (absolute or relative).
        /// </param>
        /// <returns>
        ///     True if the project-reader "beleaves" he can read the project
        ///     file.
        /// </returns>
        bool CanOpen(string filename);

        /// <summary>
        ///     This method will be used to read a project file and extract all the
        ///     data needed for the UI and for compilation.
        /// </summary>
        /// <param name="filename">
        ///     The name of the project file. It should include
        ///     path (absolute or relative).
        /// </param>
        /// <returns>All the data needed for UI and compilation</returns>
        BasicProject ReadProject(string filename);
    }
}