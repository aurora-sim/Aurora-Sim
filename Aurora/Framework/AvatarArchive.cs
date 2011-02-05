using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public class AvatarArchive
    {
        /// <summary>
        /// Name of the archive
        /// </summary>
        public string Name;
        /// <summary>
        /// XML of the archive
        /// </summary>
        public string ArchiveXML;
		
		/// <summary>
        /// uuid of a text that shows off this archive
        /// </summary>
        public string Snapshot;

        /// <summary>
        /// 1 or 0 if its public
        /// </summary>
        public int IsPublic;
    }
}
