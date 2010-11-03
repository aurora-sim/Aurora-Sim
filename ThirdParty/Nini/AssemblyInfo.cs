#region Copyright
//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of 
// which has been included with this distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.Reflection;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

#if (NET_1_0)
[assembly: AssemblyTitle("Nini for .NET Framework 1.0")]
#elif (NET_1_1)
[assembly: AssemblyTitle("Nini for .NET Framework 1.1")]
#elif (NET_2_0)
[assembly: AssemblyTitle("Nini for .NET Framework 2.0")]
#elif (MONO_1_1)
[assembly: AssemblyTitle("Nini for Mono 1.1")]
#elif (NET_COMPACT_1_0)
[assembly: AssemblyTitle("Nini for .NET Compact Framework 1.0")]
#else
[assembly: AssemblyTitle("Nini")]
#endif

[assembly: AssemblyDescription(".NET Configuration Library - http://nini.sourceforge.net/")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Brent R. Matzelle")]
[assembly: AssemblyProduct("Nini")]
[assembly: AssemblyCopyright("Copyright (c) 2006 Brent R. Matzelle. All Rights Reserved.")]
[assembly: AssemblyTrademark("Copyright (c) 2006 Brent R. Matzelle. All Rights Reserved.")]
[assembly: AssemblyDefaultAlias("Nini")]
[assembly: AssemblyCulture("")]

#if STRONG
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile(@"..\..\Nini.key")]
#endif

[assembly: System.Reflection.AssemblyVersion("1.1.0.0")]

[assembly:CLSCompliant(true)] // Required for CLS compliance

// Mark as false by default and explicity set others as true
[assembly:ComVisible(false)]

// Permview attributes
#if (NET_COMPACT_1_0)
#else
[assembly:IsolatedStorageFilePermission(SecurityAction.RequestMinimum)]
[assembly:SecurityPermission(SecurityAction.RequestRefuse)]
[assembly:FileIOPermission(SecurityAction.RequestMinimum)]
#endif
