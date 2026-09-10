// Copyright © Datalust and contributors.
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

using System.Text.Json.Nodes;

namespace SeqCli.Data;

/// <summary>
/// Adds or updates fields on an event JSON document; the equivalent, in Seq's data model, of a
/// Serilog enricher.
/// </summary>
interface IEventEnricher
{
    void Enrich(JsonObject eventJson);
}
