using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using LexisNexis.Evolution.Infrastructure;

namespace LexisNexis.Evolution.Overdrive
{
    public class WorkersInventory
    {
        //[DebuggerNonUserCode] // Suppressing expected first chance exceptions from polluting debugger output window
        protected WorkersInventory()
        {
            WorkerCards = new List<WorkerCard>();

            string workerManagerDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            Debug.Assert(workerManagerDirectory != null, "workerManagerDirectory != null");
            string workersDirectory = Path.Combine(workerManagerDirectory, @"."); // Intentionally do nothing
            Uri workersDirectoryUri = new Uri(workersDirectory);
            string workersDirectoryLocalPath = workersDirectoryUri.LocalPath;

            List<FileInfo> workersFiles = Utils.TraverseTree(workersDirectoryLocalPath, "*Worker*.dll");

            // Load for reflection the type which is the base for all other worker types
            Assembly overdriveCoreAssembly = Assembly.ReflectionOnlyLoad("OverdriveCore");
            Type baseWorkerType = overdriveCoreAssembly.GetType("LexisNexis.Evolution.Overdrive.WorkerBase");

            // Enumerate all DLLs which may contain workers
            foreach (FileInfo fileInfo in workersFiles)
            {
                Assembly assembly = null;
                try
                {
                    // Reflection only context is required to be able to introspect assemblies with different bitness
                    assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
                    //Tracer.Trace("Introspecting file {0} looking for workers",  fileInfo.FullName);
                }
                catch (Exception)
                {
                    continue; // Ignore those DLLs we can't load. They are probably not a valid .NET assemblies.
                }

                List<Type> workerTypes = LoadAllTypesForReflection(assembly).Where(t => t != baseWorkerType && t.IsSubclassOf(baseWorkerType)).ToList();

                foreach (var workerType in workerTypes)
                {
                    WorkerCard workerCard = BuildWorkerCard(fileInfo.FullName, workerType.FullName);

                    // Make sure we don't get duplicate worker for the same role
                    if (WorkerCards.Any(wc => wc.RoleType == workerCard.RoleType))
                    {
                        Tracer.Warning("WorkerManager found assembly {0}, but there already is a worker for the role {1}. Ignoring that worker.",
                            workerCard.AssemblyPath, workerCard.RoleType);
                        continue;
                    }

                    //Tracer.Info("Worker {0} found in {1}", workerCard.RoleType, workerCard.AssemblyPath);
                    WorkerCards.Add(workerCard);
                }
            }
        }

        private Type[] LoadAllTypesForReflection(Assembly assembly)
        {
            //Tracer.Trace("Loading types for assembly {0}", assembly.CodeBase);
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
            try
            {
                return assembly.GetTypes();
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= ReflectionOnlyAssemblyResolve;
            }
        }

        [DebuggerNonUserCode] // Suppressing expected first chance exceptions from polluting debugger output window
        private static Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            //Tracer.Trace("Resolving referenced assembly {0}", args.Name);
            Assembly resolvedReferencedAssembly = null;
            try
            {
                resolvedReferencedAssembly = Assembly.ReflectionOnlyLoad(args.Name);
                //Tracer.Trace("Resolved through Load");
            }
            catch (Exception)
            {
                Debug.Assert(args.RequestingAssembly.CodeBase != null, "assembly.CodeBase != null");
                string probingPath = Path.GetDirectoryName(args.RequestingAssembly.CodeBase);
                AssemblyName assemblyName = new AssemblyName(args.Name);
                probingPath = Path.Combine(probingPath, assemblyName.Name + ".dll");
                probingPath = Utils.CanonicalizePath(probingPath);
                resolvedReferencedAssembly = Assembly.ReflectionOnlyLoadFrom(probingPath);
                //Tracer.Trace("Resolved from {0}", probingPath);
            }
            return resolvedReferencedAssembly;
        }

        private WorkerCard BuildWorkerCard(string assemblyPath, string typeName)
        {
            WorkerCard workerCard = new WorkerCard();
            workerCard.AssemblyPath = assemblyPath;

            int start = typeName.LastIndexOf('.');
            string strRole = typeName.Substring(start + 1);
            strRole = Utils.RemoveSuffix(strRole, "Worker");
            workerCard.RoleType = new RoleType(strRole);

            workerCard.TypeName = typeName;

            #region Set max worker instances
            var configKey = workerCard.RoleType + "MaxInstances";
            string strMaxInstances = ConfigurationManager.AppSettings[configKey];
            if (null == strMaxInstances)
            {
                workerCard.MaxNumOfInstances = 1000; // Effectively if maximum number of worker instances per machine is not set we consider it infinite
            }
            else
            {
                uint uintMaxInstances;
                if (uint.TryParse(strMaxInstances, out uintMaxInstances))
                {
                    workerCard.MaxNumOfInstances = uintMaxInstances;
                }
                else
                {
                    Tracer.Error("Maximum number of instances for the worker {0} is configured as {1} which is not a number", strRole, strMaxInstances);
                    workerCard.MaxNumOfInstances = 1000; // Effectively if maximum number of worker instances per machine is not set we consider it infinite
                }
            }
            #endregion

            return workerCard;
        }

        public WorkerCard FindWorkerForRole(RoleType roleType)
        {
            return WorkerCards.FirstOrDefault(workerCard => roleType == workerCard.RoleType);
        }

        public List<WorkerCard> WorkerCards { get; set; }

        private static WorkersInventory _workersInventory;
        public static WorkersInventory Instance
        {
            get { return _workersInventory ?? (_workersInventory = new WorkersInventory()); }
        }
    }

    public class WorkerCard
    {
        public RoleType RoleType { get; set; }
        public string AssemblyPath { get; set; }
        public string TypeName { get; set; }
        public uint MaxNumOfInstances { get; set; }

    }
}
 