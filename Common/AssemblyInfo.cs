// $Id: AssemblyInfo.cs 129 2006-04-06 12:00:46Z pilya $

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security.Permissions;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle(".Front Common Library 1.0")]
[assembly: AssemblyDescription("Contains helper classes used in .Front based programs")]
[assembly: AssemblyCompany("Pilikan Programmers Group")]
[assembly: AssemblyCopyright("2002-2006 Pilikan Programmers Group.")]
[assembly: AssemblyDelaySign(false)]

#if (DEBUG)
[assembly: AssemblyConfiguration("DEBUG")]
#else
[assembly: AssemblyConfiguration("RETAIL")]
#endif

[assembly:CLSCompliant(true)]
[assembly:ComVisible(false)]


