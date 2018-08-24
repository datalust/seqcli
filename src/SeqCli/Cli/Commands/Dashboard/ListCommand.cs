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
using SeqCli.Config;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.Dashboard
{
    [Command("dashboard", "list", "List dashboards", Example="seqcli dashboard list")]
    class ListCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly EntityIdentityFeature _entityIdentity;
        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        string _ownerId;

        public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            _entityIdentity = Enable(new EntityIdentityFeature("dashboard", "list"));

            Options.Add(
                "o=|owner=",
                "The id of the user to list dashboards for; by default, shared dashboards are listed",
                o => _ownerId = o);

            _output = Enable(new OutputFormatFeature(config.Output));
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var ownerId = string.IsNullOrWhiteSpace(_ownerId) ? null : _ownerId.Trim();

            if (ownerId != null && _entityIdentity.Id != null)
            {
                Log.Error("Only one of either `owner` or `id` can be specified");
                return -1;
            }

            var connection = _connectionFactory.Connect(_connection);

            var list = _entityIdentity.Id != null ?
                new[] { await connection.Dashboards.FindAsync(_entityIdentity.Id) } :
                (await connection.Dashboards.ListAsync(ownerId: ownerId, shared: ownerId == null))
                    .Where(d => _entityIdentity.Title == null || _entityIdentity.Title == d.Title)
                    .ToArray();

            foreach (var dashboardEntity in list)
            {
                _output.WriteEntity(dashboardEntity);
            }
            
            return 0;
        }
    }
}
