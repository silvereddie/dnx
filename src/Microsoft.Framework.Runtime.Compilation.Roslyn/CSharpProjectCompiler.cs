using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime.Internal;
using Microsoft.Framework.Runtime.Roslyn;
using NuGet.LibraryModel;
using NuGet.ProjectModel;

namespace Microsoft.Framework.Runtime.Compilation.Roslyn
{
    public class CSharpProjectCompiler : IProjectCompiler
    {
        private static readonly ILogger Log = RuntimeLogging.Logger<CSharpProjectCompiler>();

        public CSharpProjectCompiler()
        {
        }

        public IMetadataProjectReference CompileProject(Library projectLibrary, IEnumerable<ILibraryExport> importedLibraries)
        {
            using (Log.LogTimed($"Compiling {projectLibrary.Identity.Name}"))
            {
                var project = new Project(projectLibrary.GetRequiredItem<PackageSpec>(KnownLibraryProperties.PackageSpec));

                // Convert the imported metadata references into Roslyn metadata references
                var metadataReferences = importedLibraries
                    .SelectMany(e => e.MetadataReferences)
                    .Select(ConvertMetadataReference);

                Log.LogWarning("TODO: Compilation options!!");
                var parseOptions = new CSharpParseOptions();
                var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

                var trees = GetSyntaxTrees(
                    project.Files.SourceFiles,
                    importedLibraries.SelectMany(l => l.SourceReferences),
                    parseOptions);

                var compilation = CSharpCompilation.Create(
                    projectLibrary.Identity.Name,
                    trees,
                    metadataReferences,
                    compilationOptions); 

                compilation = ApplyVersionInfo(compilation, project, parseOptions);
                Log.LogWarning("TODO: Pre/Post Processing");

                return new RoslynProjectReference(project, compilation);
            }
        }

        private static CSharpCompilation ApplyVersionInfo(CSharpCompilation compilation, Project project,
            CSharpParseOptions parseOptions)
        {
            var emptyVersion = new Version(0, 0, 0, 0);

            // If the assembly version is empty then set the version
            if (compilation.Assembly.Identity.Version == emptyVersion)
            {
                return compilation.AddSyntaxTrees(new[]
                {
                    CSharpSyntaxTree.ParseText("[assembly: System.Reflection.AssemblyVersion(\"" + project.Version.Version + "\")]", parseOptions),
                    CSharpSyntaxTree.ParseText("[assembly: System.Reflection.AssemblyInformationalVersion(\"" + project.Version + "\")]", parseOptions)
                });
            }

            return compilation;
        }

        private MetadataReference ConvertMetadataReference(IMetadataReference metadataReference)
        {
            var roslynReference = metadataReference as IRoslynMetadataReference;

            if (roslynReference != null)
            {
                return roslynReference.MetadataReference;
            }

            var embeddedReference = metadataReference as IMetadataEmbeddedReference;

            if (embeddedReference != null)
            {
                return MetadataReference.CreateFromImage(embeddedReference.Contents);
            }

            var fileMetadataReference = metadataReference as IMetadataFileReference;

            if (fileMetadataReference != null)
            {
                return GetMetadataReference(fileMetadataReference.Path);
            }

            var projectReference = metadataReference as IMetadataProjectReference;
            if (projectReference != null)
            {
                using (var ms = new MemoryStream())
                {
                    projectReference.EmitReferenceAssembly(ms);

                    return MetadataReference.CreateFromImage(ms.ToArray());
                }
            }

            Log.LogError($"Unexpected Metadata Reference of type {metadataReference.GetType().FullName}");
            throw new NotSupportedException();
        }

        private MetadataReference GetMetadataReference(string path)
        {
            // TODO: Restore caching here

            using (var stream = File.OpenRead(path))
            {
                var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
                return AssemblyMetadata.Create(moduleMetadata).GetReference();
            }
        }
        private IList<SyntaxTree> GetSyntaxTrees(IEnumerable<string> sourceFiles,
                                                 IEnumerable<ISourceReference> sourceReferences,
                                                 CSharpParseOptions parseOptions)
        {
            var trees = new List<SyntaxTree>();

            foreach (var sourcePath in sourceFiles)
            {
                var syntaxTree = CreateSyntaxTree(sourcePath, parseOptions);

                trees.Add(syntaxTree);
            }

            foreach (var sourceFileReference in sourceReferences.OfType<ISourceFileReference>())
            {
                var sourcePath = sourceFileReference.Path;

                var syntaxTree = CreateSyntaxTree(sourcePath, parseOptions);

                trees.Add(syntaxTree);
            }

            return trees;
        }

        private SyntaxTree CreateSyntaxTree(string sourcePath, CSharpParseOptions parseOptions)
        {
            Log.LogDebug($"Parsing {sourcePath}");

            using (var stream = File.OpenRead(sourcePath))
            {
                var sourceText = SourceText.From(stream, encoding: Encoding.UTF8);

                return CSharpSyntaxTree.ParseText(sourceText, options: parseOptions, path: sourcePath);
            }
        }
    }
}