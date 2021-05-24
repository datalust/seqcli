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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using SeqCli.Templates.Ast;
using SeqCli.Templates.Export;
using SeqCli.Templates.Parser;

#nullable enable

namespace SeqCli.Templates.Import
{
    static class EntityTemplateLoader
    {
        public static bool Load(string path, [MaybeNullWhen(false)] out EntityTemplate template, [MaybeNullWhen(true)] out string error)
        {
            if (!File.Exists(path))
            {
                template = null;
                error = "the template file was not found";
                return false;
            }

            var source = File.ReadAllText(path);
            if (!JsonTemplateParser.TryParse(source, out var root, out var parseError, out _))
            {
                template = null;
                error = parseError;
                return false;
            }

            if (root is not JsonTemplateObject rootDictionary ||
                !rootDictionary.Members.TryGetValue("$entity", out var resourceToken) ||
                resourceToken is not JsonTemplateString resource ||
                resource.Value is null)
            {
                template = null;
                error = "the template must include an `$entity` property";
                return false;
            }
            
            var resourceGroup = EntityName.ToResourceGroup(resource.Value);
            var filename = Path.GetFileName(path);
            
            template = new EntityTemplate(resourceGroup, filename, root);
            error = null;
            return true;
        }
    }
}
