// Copyright © Datalust Pty Ltd and Contributors
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

using System;
using SeqCli.Templates.Ast;

namespace SeqCli.Templates.Import;

class EntityTemplate
{
    public string ResourceGroup { get; }
    public string Name { get; }
    public JsonTemplate Entity { get; }

    public EntityTemplate(string resourceGroup, string name, JsonTemplate entity)
    {
        ResourceGroup = resourceGroup ?? throw new ArgumentNullException(nameof(resourceGroup));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    }
}