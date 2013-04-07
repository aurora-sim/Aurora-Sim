/*
 * No license given,
 * taken from http://csharper.fairblog.ro/2010/05/compiling-c-projects-at-runtime-parsing-the-csproj-file 
 */

namespace RunTimeCompiler
{
    /// <summary>
    ///     This class should keep a list of known file extenstions and
    ///     the registered IProjectReader for each extenstion.
    ///     When a project file is loaded the ReadProject method is called
    ///     to read that project and get all data needed for the UI and
    ///     compilation.
    ///     This class is implemented as a singleton.
    /// </summary>
    public class ProjectReader
    {
        #region Singleton pattern

        /// <summary>
        ///     Private reference to a ProjectReader instance.
        /// </summary>
        private static ProjectReader _instance;

        /// <summary>
        ///     Private constructor.
        /// </summary>
        private ProjectReader()
        {
        }

        /// <summary>
        ///     The only accessor for a ProjectReader instance.
        /// </summary>
        public static ProjectReader Instance
        {
            get
            {
                if (_instance == null) _instance = new ProjectReader();
                return _instance;
            }
        }

        #endregion

        /// <summary>
        ///     This method is used to read the content of a project file and get
        ///     all the data neededfor UI and compilation.
        ///     Current implementation always use CsprojReader. It will be changed
        ///     as more project-readers will be developed.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public BasicProject ReadProject(string filename)
        {
            //TODO: Add some logic here: check filename extension, if it is
            //.csproj use CsprojReader, check if it can open the file and read
            //the project or use an other IProjectReader if the current version
            //of .csproj is unknown to CsprojReader.

            //Get the appropiate reader for the filetype.
            //As filetype is always .csproj i'll just use CsprojReader.
            CsprojReader reader = new CsprojReader();
            //TODO: Decide if this is needed. ReadProject should only be called after CanOpen, 
            //so I may not need to check it again.
            if (!reader.CanOpen(filename)) return null;
            return reader.ReadProject(filename);
        }
    }
}