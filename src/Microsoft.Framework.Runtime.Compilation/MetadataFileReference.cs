using System;

namespace Microsoft.Framework.Runtime.Compilation
{
    internal class MetadataFileReference : IMetadataReference
    {
        public MetadataFileReference(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; }
        public string Path { get; }
    }
}