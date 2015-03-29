﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Framework.Runtime.Compilation;

namespace Microsoft.Framework.Runtime
{
    internal class DesignTimeProjectReference : IMetadataProjectReference
    {
        private readonly ICompilationProject _project;
        private readonly CompileResponse _response;

        public DesignTimeProjectReference(ICompilationProject project, CompileResponse response)
        {
            _project = project;
            _response = response;
        }

        public string Name { get { return _project.Name; } }

        public string ProjectPath
        {
            get
            {
                return _project.ProjectDirectory;
            }
        }

        public IDiagnosticResult GetDiagnostics()
        {
            bool hasErrors = _response.Diagnostics.HasErrors();
            return new DiagnosticResult(hasErrors, _response.Diagnostics);
        }

        public IList<ISourceReference> GetSources()
        {
            throw new NotSupportedException();
        }

        public Assembly Load(IAssemblyLoadContext loadContext)
        {
            if (_response.Diagnostics.HasErrors())
            {
                throw new DesignTimeCompilationException(_response.Diagnostics);
            }

            if (_response.AssemblyPath != null)
            {
                return loadContext.LoadFile(_response.AssemblyPath);
            }

            if (_response.PdbBytes == null)
            {
                return loadContext.LoadStream(new MemoryStream(_response.AssemblyBytes), assemblySymbols: null);
            }

            return loadContext.LoadStream(new MemoryStream(_response.AssemblyBytes),
                                           new MemoryStream(_response.PdbBytes));
        }

        public void EmitReferenceAssembly(Stream stream)
        {
            if (_response.AssemblyPath != null)
            {
                using (var fs = File.OpenRead(_response.AssemblyPath))
                {
                    fs.CopyTo(stream);
                }
            }
            else
            {
                stream.Write(_response.AssemblyBytes, 0, _response.AssemblyBytes.Length);
            }
        }

        public IDiagnosticResult EmitAssembly(string outputPath)
        {
            throw new NotSupportedException();
        }
    }
}