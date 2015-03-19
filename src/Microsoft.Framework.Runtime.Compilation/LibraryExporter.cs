using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime.Internal;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;

namespace Microsoft.Framework.Runtime.Compilation
{
    public class LibraryExporter
    {
        private static readonly ILogger Log = RuntimeLogging.Logger<LibraryExporter>();
        private readonly NuGetFramework _targetFramework;
        private readonly PackagePathResolver _packagePathResolver;

        public LibraryExporter(NuGetFramework targetFramework, PackagePathResolver packagePathResolver)
        {
            _targetFramework = targetFramework;
            _packagePathResolver = packagePathResolver;
        }

        /// <summary>
        /// Creates a <see cref="ILibraryExport"/> containing the references necessary
        /// to use the provided <see cref="Library"/> during compilation.
        /// </summary>
        /// <param name="library">The <see cref="Library"/> to export</param>
        /// <returns>A <see cref="ILibraryExport"/> containing the references exported by this library</returns>
        public ILibraryExport ExportLibrary(Library library)
        {
            switch(library.Identity.Type)
            {
                case LibraryTypes.Package:
                    return ExportPackageLibrary(library);
                case LibraryTypes.Project:
                    return ExportProjectLibrary(library);
                case LibraryTypes.Reference:
                    return ExportReferenceLibrary(library);
                default:
                    return LibraryExport.Empty;
            }
        }

        private ILibraryExport ExportPackageLibrary(Library library)
        {
            using (Log.LogTimed($"Exporting Package {library.Identity.Name}"))
            {
                // Get the lock file group and library
                var group = library.GetRequiredItem<LockFileFrameworkGroup>(KnownLibraryProperties.LockFileFrameworkGroup);
                var lockFileLibrary = library.GetRequiredItem<LockFileLibrary>(KnownLibraryProperties.LockFileLibrary);

                // Resolve the package root
                var packageRoot = _packagePathResolver.ResolvePackagePath(
                    lockFileLibrary.Sha,
                    lockFileLibrary.Name,
                    lockFileLibrary.Version);

                // Grab the compile time assemblies and their full paths
                var metadataReferences = new List<IMetadataReference>();
                foreach (var compileTimeAssembly in group.CompileTimeAssemblies)
                {
                    var reference = new MetadataFileReference(
                        Path.GetFileNameWithoutExtension(compileTimeAssembly),
                        Path.Combine(packageRoot, compileTimeAssembly));

                    metadataReferences.Add(reference);

                    if (Log.IsEnabled(LogLevel.Debug))
                    {
                        Log.LogDebug($"Exporting {compileTimeAssembly} from {library.Identity.Name}");
                    }
                }

                return new LibraryExport(metadataReferences);
            }

        }

        private ILibraryExport ExportProjectLibrary(Library library)
        {
            return LibraryExport.Empty;
        }

        private ILibraryExport ExportReferenceLibrary(Library library)
        {
            return LibraryExport.Empty;
        }
    }
}