using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{
    public class AssemblyResolutionCache
    {
        protected AssemblyResolutionCache()
        {
            AllRelevantAssemblies = new Dictionary<string, FileInfo>();

            string workerManagerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            string workersDirectory = Path.Combine(workerManagerDirectory, @"."); // Intentionally do nothing
            Uri workersDirectoryUri = new Uri(workersDirectory);
            string workersDirectoryLocalPath = workersDirectoryUri.LocalPath;

            List<FileInfo> workersFiles = Utils.TraverseTree(workersDirectoryLocalPath, "*.dll");

            foreach (FileInfo fileInfo in workersFiles)
            {
                string simpleAssemblyName = Utils.RemoveSuffix(fileInfo.Name, ".dll");
                if (!AllRelevantAssemblies.ContainsKey(simpleAssemblyName))
                {
                    AllRelevantAssemblies.Add(simpleAssemblyName, fileInfo);
                }
            }
        }

        #region Dynamic Assembly references resolution

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Assembly ResolveAssemblyReference(object sender, ResolveEventArgs args)
        {
            AssemblyName referenceAssemblyName = new AssemblyName(args.Name);
            //Tracer.Trace("AssemblyResolutionCache trying to resolve {0}", referenceAssemblyName);

            // Debugging
            //if (referenceAssemblyName.Name == "Data")
            //{
            //    Tracer.Trace("AssemblyResolutionCache trying to resolve {0}", referenceAssemblyName);
            //}

            Assembly[] currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in from assembly in currentAssemblies let assemblyName = assembly.GetName() where AssemblyName.ReferenceMatchesDefinition(referenceAssemblyName, assemblyName) select assembly)
            {
                //Tracer.Trace("    Assembly Found in Domain");
                return assembly;
            }

            FileInfo fi;
            if (AllRelevantAssemblies.TryGetValue(referenceAssemblyName.Name, out fi))
            {
                return TryLoadAssemblyFromFile(fi.FullName, referenceAssemblyName);
            }
            //Tracer.Warning("    AssemblyResolutionCache does not know about that assembly");
            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        private static Assembly TryLoadAssemblyFromFile(string pathToAssemblyWeHave, AssemblyName referenceAssemblyName)
        {
            AssemblyName assemblyNameWeHave;
            try
            {
                assemblyNameWeHave = AssemblyName.GetAssemblyName(pathToAssemblyWeHave);
            }
            catch (Exception ex)
            {
                Tracer.Warning("    AssemblyName.GetAssemblyName({0}) throwed {1}", pathToAssemblyWeHave, ex);
                return null;
            }

            //if (assemblyNameWeHave.Name != strReferenceAssemblyName && assemblyNameWeHave.FullName != strReferenceAssemblyName)
            if (!AssemblyName.ReferenceMatchesDefinition(referenceAssemblyName, assemblyNameWeHave))
            {
                //Tracer.Warning("    AssemblyResolutionCache looks for {0}, but what we have at {1} has different name {2}",
                //    referenceAssemblyName, pathToAssemblyWeHave, assemblyNameWeHave);
                return null;
            }

            Assembly resolvedAssembly;
            try
            {
                resolvedAssembly = Assembly.LoadFrom(pathToAssemblyWeHave);
            }
            catch (Exception ex)
            {
                /* Do Nothing */
                Tracer.Warning("    Assembly.LoadFrom({0}) throwed {1}", pathToAssemblyWeHave, ex);
                return null;
            }
            //Tracer.Trace("    AssemblyResolutionCache resolved {0} to {1}", referenceAssemblyName, pathToAssemblyWeHave);
            return resolvedAssembly;
        }

        #endregion

        private static AssemblyResolutionCache _assemblyResolutionCache;
        public static AssemblyResolutionCache Instance
        {
            get { return _assemblyResolutionCache ?? (_assemblyResolutionCache = new AssemblyResolutionCache()); }
        }

        public Dictionary<string, FileInfo> AllRelevantAssemblies { get; private set; }
    }
}
