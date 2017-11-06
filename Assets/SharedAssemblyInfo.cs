using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("In Touch Technologies")]
[assembly: AssemblyProduct("Esatto Virtual Printer")]
[assembly: AssemblyCopyright("© In Touch Technologies.  All rights reserved.")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)]

// changing versioning requires updating GUIDs of interfaces and assemblies
// and requires an update to the driver and wxs.
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]
[assembly: ComCompatibleVersion(3, 0, 0, 0)]
