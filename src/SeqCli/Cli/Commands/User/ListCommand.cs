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

namespace SeqCli.Cli.Commands.User
{
    [Command("user", "list", "List users", Example="seqcli user list")]
    class ListCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly UserIdentityFeature _userIdentity;
        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        public ListCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            _userIdentity = Enable(new UserIdentityFeature("list"));
            _output = Enable(new OutputFormatFeature(config.Output));
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var list = _userIdentity.Id != null ?
                new[] { await connection.Users.FindAsync(_userIdentity.Id) } :
                (await connection.Users.ListAsync())
                    .Where(u => _userIdentity.Name == null || _userIdentity.Name == u.Username)
                    .ToArray();

            foreach (var user in list)
            {
                _output.WriteEntity(user);
            }
            
            return 0;
        }
    }
}
