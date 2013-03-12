/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Aurora.Framework
{
    public class PathHelpers
    {
        const string usernameVar = "%username%";
        public static string PathUsername(string Path) //supports using %username% in place of username
        {
            if (Path.IndexOf(usernameVar, StringComparison.CurrentCultureIgnoreCase) == -1) //does not contain username var
            {
                return Path;
            }
            else //contains username var
            {
                string userName = Environment.UserName; //check system for current username
                return Regex.Replace(Path, usernameVar, userName, RegexOptions.IgnoreCase); //return Path with the system username
            }
        }

        const string HomedriveVar = "%homedrive%";
        public static string PathHomeDrive(string Path) //supports for %homedrive%, gives the drive letter on Windows
        {
            if (Path.IndexOf(HomedriveVar, StringComparison.CurrentCultureIgnoreCase) == -1) //does not contain username var
            {
                return Path;
            }
            else
            {
                if (Util.IsLinux)
                {
                    return Path;
                }
                else
                {
                    string DriveLetter = Environment.GetEnvironmentVariable("HOMEDRIVE");

                    return Regex.Replace(Path, HomedriveVar, DriveLetter, RegexOptions.IgnoreCase);
                }
            }
        }

        public static string PathTilde(string Path) //supports ~ for home dir at beginning of Path
        {
            if (Path.IndexOf("~", StringComparison.CurrentCultureIgnoreCase) == -1) //does not contain ~
            {
                return Path;
            }
            else
            {
                if (Path[0].ToString() == "~")
                {
                    string homePath = "";

                    if (Util.IsLinux)
                    {
                        homePath = Environment.GetEnvironmentVariable("HOME");
                    }
                    else
                    {
                        homePath = Environment.GetEnvironmentVariable("userprofile");
                    }

                    Path = Path.Substring(1);
                    return homePath + Path;
                }
                else
                {
                    return Path;
                }
            }
        }

        public static string ForceEndSlash(string Path)
        {
            string LastCharacter = Path.Last().ToString();

            if (LastCharacter == "/" || LastCharacter == "\\")
            {
                return Path;
            }
            else
            {
                return Path + "/";
            }
        }

        public static string ComputeFullPath(string Path) //single function that calls the functions that help compute a full url Path
        {
            return ComputeFullPath(Path, true);
        }

        public static string ComputeFullPath(string Path, bool forceSlash) //single function that calls the functions that help compute a full url Path
        {
            Path = PathHomeDrive(
                PathUsername(
                    PathTilde(Path)
                )
            );

            if (forceSlash)
            {
                Path = ForceEndSlash(Path);
            }

            return Path;
        }
    }
}
