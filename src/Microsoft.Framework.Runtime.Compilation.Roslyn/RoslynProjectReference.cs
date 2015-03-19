using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime.Internal;

namespace Microsoft.Framework.Runtime.Compilation.Roslyn
{
    internal class RoslynProjectReference : IMetadataProjectReference
    {
        private static readonly ILogger Log = RuntimeLogging.Logger<RoslynProjectReference>();
        private readonly Project _project;
        private readonly CSharpCompilation _compilation;

        public RoslynProjectReference(Project project, CSharpCompilation compilation)
        {
            _project = project;
            _compilation = compilation;
        }

        public string Name { get { return _project.Name; } }

        public string ProjectPath { get { return _project.FilePath; } }

        public IDiagnosticResult EmitAssembly(string outputPath)
        {
            throw new NotImplementedException();
        }

        public void EmitReferenceAssembly(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IDiagnosticResult GetDiagnostics()
        {
            throw new NotImplementedException();
        }

        public IList<ISourceReference> GetSources()
        {
            throw new NotImplementedException();
        }

        public Assembly Load(IAssemblyLoadContext loadContext)
        {
            Assembly assembly = null;

            using (var pdbStream = new MemoryStream())
            using (var assemblyStream = new MemoryStream())
            {
                Log.LogWarning("TODO: Resources!");

                EmitResult emitResult = null;
                using (Log.LogTimed($"Emitting assembly for {Name}"))
                {
                    emitResult = _compilation.Emit(assemblyStream, pdbStream: pdbStream);
                }

                if (!emitResult.Success)
                {
                    throw new Exception("TODO: You no code good!");
                }

                pdbStream.Seek(0, SeekOrigin.Begin);
                assemblyStream.Seek(0, SeekOrigin.Begin);

                using (Log.LogTimed($"Loading emitted assembly for {Name}"))
                {
                    assembly = loadContext.LoadStream(assemblyStream, pdbStream);
                }
            }
            return assembly;
        }

        public override string ToString()
        {
            return "Project: " + ProjectPath;
        }
    }
}