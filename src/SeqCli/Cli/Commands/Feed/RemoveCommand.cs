// Copyright 2018 Datalust Pty Ltd and Contributors
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
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.Feed
{
    [Command("feed", "remove", "Remove a NuGet feed from the server",
        Example="seqcli feed remove -n CI")]
    class RemoveCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        
        readonly ConnectionFeature _connection;
        
        string _name, _id;
        
        public RemoveCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "n=|name=",
                "The name of the feed to remove",
                n => _name = n);

            Options.Add(
                "i=|id=",
                "The id of a single feed to remove",
                id => _id = id);
            
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            if (_name == null && _id == null)
            {
                Log.Error("A `name` or `id` must be specified");
                return 1;
            }

            var connection = _connectionFactory.Connect(_connection);

            var toRemove = _id != null ?
                new[] {await connection.Feeds.FindAsync(_id)} :
                (await connection.Feeds.ListAsync())
                    .Where(f => _name == f.Name) 
                    .ToArray();

            if (!toRemove.Any())
            {
                Log.Error("No matching feed was found");
                return 1;
            }

            foreach (var feed in toRemove)
                await connection.Feeds.RemoveAsync(feed);

            return 0;
        }
    }
}
