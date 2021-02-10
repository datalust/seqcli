// Copyright Datalust Pty Ltd and Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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