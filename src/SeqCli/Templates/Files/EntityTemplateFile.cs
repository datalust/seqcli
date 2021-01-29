using System;
using SeqCli.Templates.Ast;

namespace SeqCli.Templates.Files
{
    class EntityTemplateFile
    {
        public string Path { get; }
        public string ResourceGroup { get; }
        public string Name { get; }
        public JsonTemplate Entity { get; }

        public EntityTemplateFile(string path, string resourceGroup, string name, JsonTemplate entity)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            ResourceGroup = resourceGroup ?? throw new ArgumentNullException(nameof(resourceGroup));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }
}
