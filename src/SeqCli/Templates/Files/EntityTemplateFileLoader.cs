using System.IO;
using System.Linq;
using SeqCli.Templates.Parser;

namespace SeqCli.Templates.Files
{
    static class EntityTemplateFileLoader
    {
        public static bool Load(string path, out EntityTemplateFile template, out string error)
        {
            if (!File.Exists(path))
            {
                template = null;
                error = $"The file `{path}` was not found.";
                return false;
            }

            var withoutExt = Path.GetFileNameWithoutExtension(path);
            if (!withoutExt.Contains("-"))
            {
                template = null;
                error = "Template filenames must be in `{id prefix}-{name}` dashed format.";
                return false;
            }

            var source = File.ReadAllText(path);
            if (!JsonTemplateParser.TryParse(source, out var root, out var parseError, out _))
            {
                template = null;
                error = $"{path}: {parseError}";
                return false;
            }

            var resourceGroup = withoutExt.Split('-').First();
            if (!resourceGroup.EndsWith("y"))
            {
                resourceGroup += "s";
            }
            else
            {
                resourceGroup = resourceGroup.TrimEnd('y') + "ies";
            }

            var filename = Path.GetFileName(path);
            
            template = new EntityTemplateFile(path, resourceGroup, filename, root);
            error = null;
            return true;
        }
    }
}