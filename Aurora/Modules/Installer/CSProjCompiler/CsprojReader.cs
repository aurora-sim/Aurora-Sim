/*
 * No license given,
 * taken from http://csharper.fairblog.ro/2010/05/compiling-c-projects-at-runtime-parsing-the-csproj-file 
 */

namespace RunTimeCompiler
{
    /// <summary>
    ///     This is a reader for C# project files, or more precisely a
    ///     wrapper to a reader of all .csproj.
    ///     As the structure of the .csproj file appears to change with the version
    ///     of Visual Studio, this class must use the appropriate .csproj reader.
    ///     AllCsprojReader (the only class used currently to process .csproj files)
    ///     support .csproj files created with Visual Studio 2005+.
    ///     Visual Studio 2010 is the most recent version of Visual Studio, and the
    ///     .csproj files it genererates are processed ok by AllCsprojReader.
    ///     I do not know if .csproj files created with Visual Studio 2001-2003 can
    ///     be processed successfully.
    /// </summary>
    public class CsprojReader : IProjectReader
    {
        #region IProjectReader Members

        /// <summary>
        ///     Defined in IProjectReader.
        ///     It is used to check if the specified file can be opened by this
        ///     project-reader.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool CanOpen(string filename)
        {
            //TODO: Add some logic here: check extension to be .csproj,
            //open the .csproj file and get Visual Studio version and see
            //if that version of .csproj can be open...
            return true;
        }

        /// <summary>
        ///     Defined in IProjectReader.
        ///     It is used to retriece all the data needed for UI and compilation.
        /// </summary>
        /// <param name="filename">The name (and path) of the C# project file.</param>
        /// <returns></returns>
        public BasicProject ReadProject(string filename)
        {
            AllCsprojReader reader;
            if (!CanOpen(filename)) return null;
            reader = new AllCsprojReader();
            return reader.ReadProject(filename);
        }

        #endregion
    }
}