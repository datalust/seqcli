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
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Cli.Commands.Node;

[Command("node", "list", "List nodes in the Seq cluster",
    Example = "seqcli node list --json")]
class ListCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    string? _name, _id;
        
    public ListCommand()
    {
        Options.Add(
            "n=|name=",
            "The name of the cluster node to list",
            n => _name = n);

        Options.Add(
            "i=|id=",
            "The id of a single cluster node to list",
            id => _id = id);
            
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }
        
    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var list = _id != null ? [await connection.Cluster.FindAsync(_id)]
            :
            (await connection.Cluster.ListAsync())
            .Where(n => _name == null || _name == n.Name);

        _output.GetOutputFormat(config).ListEntities(list);
            
        return 0;
    }
}