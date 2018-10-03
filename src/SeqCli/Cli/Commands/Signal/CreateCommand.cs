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

namespace SeqCli.Cli.Commands.Signal
{
    [Command("signal", "create", "Create a signal",
        Example = "seqcli signal create -t 'Exceptions' -f \"@Exception is not null\"")]
    class CreateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        string _title, _description, _filter, _group;
        bool _isProtected, _noGrouping;

        public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "t=|title=",
                "A title for the signal",
                t => _title = ArgumentString.Normalize(t));

            Options.Add(
                "description=",
                "A description for the signal",
                d => _description = ArgumentString.Normalize(d));

            Options.Add(
                "f=|filter=",
                "Filter to associate with the signal",
                f => _filter = ArgumentString.Normalize(f));

            Options.Add(
                "group=",
                "An explicit group name to associate with the signal; the default is to infer the group from the filter",
                g => _group = ArgumentString.Normalize(g));

            Options.Add(
                "no-group",
                "Specify that no group should be inferred; the default is to infer the group from the filter",
                _ => _noGrouping = true);

            Options.Add(
                "protected",
                "Specify that the signal is editable only by administrators",
                _ => _isProtected = true);

            _connection = Enable<ConnectionFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var signal = await connection.Signals.TemplateAsync();

            signal.Title = _title;
            signal.Description = _description;
            signal.IsProtected = _isProtected;

            if (_noGrouping)
            {
                signal.Grouping = SignalGrouping.None;
            }
            else if (_group != null)
            {
                signal.Grouping = SignalGrouping.Explicit;
                signal.ExplicitGroupName = _group;
            }
            else
            {
                signal.Grouping = SignalGrouping.Inferred;
            }

            if (_filter != null)
            {
                signal.Filters = new[]
                {
                    new SignalFilterPart
                    {
                        Filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression,
                        FilterNonStrict = _filter
                    }
                }.ToList();
            }

            signal = await connection.Signals.AddAsync(signal);

            _output.WriteEntity(signal);

            return 0;
        }
    }
}
