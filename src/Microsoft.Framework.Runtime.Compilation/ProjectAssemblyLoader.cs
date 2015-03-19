using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime.Dependencies;
using Microsoft.Framework.Runtime.Internal;
using NuGet.Frameworks;
using NuGet.LibraryModel;

namespace Microsoft.Framework.Runtime.Compilation
{
    public class ProjectAssemblyLoaderFactory : IAssemblyLoaderFactory
    {
        public IAssemblyLoader Create(NuGetFramework runtimeFramework, IAssemblyLoadContextAccessor loadContextAccessor, DependencyManager dependencies)
        {
            return new ProjectAssemblyLoader(
                dependencies.GetLibraries(LibraryTypes.Project));
        }
    }

    /// <summary>
    /// Loads assemblies by compiling projects
    /// </summary>
    public class ProjectAssemblyLoader : IAssemblyLoader
    {
        private static readonly ILogger Log = RuntimeLogging.Logger<ProjectAssemblyLoader>();
        private Dictionary<string, Library> _projects = new Dictionary<string, Library>();

        public ProjectAssemblyLoader(IEnumerable<Library> projects)
        {
            _projects = projects.ToDictionary(l => l.Identity.Name);
        }

        public Assembly Load(string name)
        {
            Library library;
            if(!_projects.TryGetValue(name, out library))
            {
                return null;
            }

            using (Log.LogTimed($"Loading Project {name}"))
            {
                Log.LogWarning($"Project Loader not yet implemented. Unable to load {name}.");
                return null;
            }
        }
    }
}