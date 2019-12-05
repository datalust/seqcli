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
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.User
{
    [Command("user", "create", "Create a user",
        Example = "seqcli user create -n alice -d 'Alice Example' -r 'User (read/write)' --password-stdin")]
    class CreateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        string _username, _displayName, _roleTitle, _filter, _emailAddress, _password;
        bool _passwordStdin;

        public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "n=|name=",
                "A unique username for the user",
                u => _username = ArgumentString.Normalize(u));

            Options.Add(
                "d=|display-name=",
                "A long-form name to aid in identifying the user",
                d => _displayName = ArgumentString.Normalize(d));

            Options.Add(
                "f=|filter=",
                "A view filter that limits the events visible to the user",
                f => _filter = ArgumentString.Normalize(f));

            Options.Add(
                "r=|role=",
                "The title of a role that grants the user permissions on the server; if not specified, the default new user role will be assigned",
                r => _roleTitle = ArgumentString.Normalize(r));

            Options.Add(
                "e=|email=",
                "The user's email address (enables a Gravatar image for the user)",
                e => _emailAddress = ArgumentString.Normalize(e));

            Options.Add(
                "p=|password=",
                "An initial password for the user, if username/password authentication is in use; note that `--password-stdin` is more secure",
                p => _password = ArgumentString.Normalize(p));

            Options.Add(
                "password-stdin",
                "Read the initial password for the user from `STDIN`, if username/password authentication is in use",
                _ => _passwordStdin = true);

            _connection = Enable<ConnectionFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var user = await connection.Users.TemplateAsync();

            user.Username = _username;
            user.DisplayName = _displayName;
            user.EmailAddress = _emailAddress;

            if (_passwordStdin)
            {
                if (_password != null)
                {
                    Log.Error("The `password` and `password-stdin` options are mutually exclusive");
                    return 1;
                }

                while (string.IsNullOrEmpty(_password))
                    _password = await Console.In.ReadLineAsync();
            }

            if (_password != null)
            {
                user.NewPassword = _password;
                user.MustChangePassword = true;
            }
            
            if (_roleTitle == null)
            {
                if (!user.RoleIds.Any())
                    Log.Warning("No role has been specified; the user will not have any permissions on the server");
            }
            else
            {
                user.RoleIds.Clear();
                var roles = await connection.Roles.ListAsync();
                var role = roles.SingleOrDefault(r => r.Title == _roleTitle);
                if (role == null)
                {
                    Log.Error("No role with title {RoleTitle} could be found; available roles are: {RoleTitles}", _roleTitle, roles.Select(r => r.Title).ToArray());
                    return 1;
                }

                user.RoleIds.Add(role.Title);
            }

            if (_filter != null)
            {
                user.ViewFilter = new SignalFilterPart
                {
                    Filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression,
                    FilterNonStrict = _filter
                };
            }

            user = await connection.Users.AddAsync(user);

            _output.WriteEntity(user);

            return 0;
        }
    }
}
