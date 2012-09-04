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
using System.IO;
using System.Text;

namespace Aurora.Framework
{
    public class VersionInfo
    {
        #region Flavour enum

        public enum Flavour
        {
            Unknown,
            Dev,
            RC1,
            RC2,
            Release,
            Post_Fixes
        }

        #endregion

        public const string VERSION_NUMBER = "0.5.3";
        public const Flavour VERSION_FLAVOUR = Flavour.Dev;
        public const string VERSION_NAME = "Aurora";

        public const int VERSIONINFO_VERSION_LENGTH = 27;

        ///<value>
        ///  This is the external interface version.  It is separate from the OpenSimulator project version.
        /// 
        ///  This version number should be 
        ///  increased by 1 every time a code change makes the previous OpenSimulator revision incompatible
        ///  with the new revision.  This will usually be due to interregion or grid facing interface changes.
        /// 
        ///  Changes which are compatible with an older revision (e.g. older revisions experience degraded functionality
        ///  but not outright failure) do not need a version number increment.
        /// 
        ///  Having this version number allows the grid service to reject connections from regions running a version
        ///  of the code that is too old. 
        ///
        ///</value>
        public static readonly int MajorInterfaceVersion = 6;

        public static string Version
        {
            get { return GetVersionString(VERSION_NUMBER, VERSION_FLAVOUR); }
        }

        public static string GetVersionString(string versionNumber, Flavour flavour)
        {
            string versionString = VERSION_NAME + " " + versionNumber + " " + flavour;
            versionString = versionString.PadRight(VERSIONINFO_VERSION_LENGTH);

            // Add commit hash and date information if available
            // The commit hash and date are stored in a file bin/.version
            // This file can automatically created by a post
            // commit script in the opensim git master repository or
            // by issuing the follwoing command from the top level
            // directory of the opensim repository
            // git log -n 1 --pretty="format:%h-%ci" >bin/.version
            // For the full git commit hash use %H instead of %h
            //
            string gitCommitFileName = ".version";

            string pathToGitFile = Path.Combine(Environment.CurrentDirectory,
                                                Path.Combine("..\\", Path.Combine(".git", "logs")));

            if (Directory.Exists(pathToGitFile))
            {
                string gitFile = Path.Combine(pathToGitFile, "HEAD");
                if (File.Exists(gitFile))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(gitFile);
                        string lastLine = lines[lines.Length - 1];
                        string[] splitLastLine = lastLine.Split(new string[2] { " ", "\t" },
                                                                StringSplitOptions.RemoveEmptyEntries);
                        versionString = "Aurora-" + splitLastLine[1].Substring(0, 6) /*First 6 digits of the commit hash*/+
                                        " " + splitLastLine[5] /*Time zone info*/;
                        FileStream s = File.Open(gitCommitFileName, FileMode.Create);
                        byte[] data = Encoding.UTF8.GetBytes(versionString);
                        s.Write(data, 0, data.Length);
                        s.Close();
                    }
                    catch { }
                }
            }
            else if (File.Exists(gitCommitFileName))
            {
                StreamReader CommitFile = File.OpenText(gitCommitFileName);
                versionString = CommitFile.ReadLine();
                CommitFile.Close();
            }
            return versionString;
        }
    }
}