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

namespace SeqCli.Cli.Commands.Bench;

// ReSharper disable ClassNeverInstantiated.Global AutoPropertyCanBeMadeGetOnly.Global UnusedAutoPropertyAccessor.Global

class QueryBenchCase
{
    public required string Id { get; set; }
    public required string Query { get; set; }
    public string? SignalExpression { get; set; }
    
    // Not used programmatically at this time.
    // ReSharper disable once UnusedMember.Global
    public string? Notes { get; set; }
}