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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api.Model.Alerting;
using Seq.Api.Model.LogEvents;
using Seq.Api.Model.Shared;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Mapping;
using SeqCli.Signals;
using SeqCli.Syntax;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.Alert;

[Command("alert", "create", "Create an alert",
    Example = "seqcli alert create -t 'Too many errors' --select \"count(*) as errors\" --from stream --signal signal-m33302 " +
              "--where \"@Level = 'Error'\" --window 5m --having \"errors > 10\" --notification-level Error --suppression-time 10m")]
class CreateCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;

    readonly List<string> _lateral = new();
    readonly List<string> _select = new();
    readonly List<string> _groupBy = new();
    readonly List<string> _notificationApps = new();

    string? _title, _description, _from, _signal, _where, _having, _window, _notificationLevel, _suppressionTime;
    bool _isProtected, _isDisabled;

    public CreateCommand()
    {
        Options.Add(
            "t=|title=",
            "A title for the alert",
            t => _title = ArgumentString.Normalize(t));

        Options.Add(
            "description=",
            "A description for the alert",
            d => _description = ArgumentString.Normalize(d));

        Options.Add(
            "from=",
            "The data source the alert queries; either `stream` (the default) or `series`",
            f => _from = ArgumentString.Normalize(f));

        Options.Add(
            "signal=",
            "A signal expression limiting the alert's input, for example `signal-1` or `signal-1,signal-2`",
            s => _signal = ArgumentString.Normalize(s));

        Options.Add(
            "lateral=",
            "A lateral join over a set-valued function applied to the data source, in the form `<setFunctionCall> as <alias>`, for example `unnest(http.server.request.duration.buckets) as bucket`; this argument can be used multiple times",
            l => _lateral.Add(ArgumentString.Normalize(l) ?? throw new ArgumentException("Lateral joins require a value.")));

        Options.Add(
            "where=",
            "A predicate that selects the events the alert will consider",
            w => _where = ArgumentString.Normalize(w));

        Options.Add(
            "select=",
            "A measurement the alert condition will test, for example `count(*) as errors`; this argument can be used multiple times",
            s => _select.Add(ArgumentString.Normalize(s) ?? throw new ArgumentException("Measurements require a value.")));

        Options.Add(
            "group-by=",
            "An expression to group measurements by, for example `ServiceName` or `ServiceName ci as service`; the `ci` modifier makes the grouping case-insensitive; this argument can be used multiple times",
            g => _groupBy.Add(ArgumentString.Normalize(g) ?? throw new ArgumentException("Groupings require a value.")));

        Options.Add(
            "window=",
            "The measurement window over which the alert condition is evaluated, as a duration, for example `1m` or `1h`",
            w => _window = ArgumentString.Normalize(w));

        Options.Add(
            "having=",
            "The alert condition; a predicate over the grouped measurements, for example `errors > 10`",
            h => _having = ArgumentString.Normalize(h));

        Options.Add(
            "notification-level=",
            "The level of the notifications raised by the alert, for example `Warning` or `Error`; the default is `Error`",
            l => _notificationLevel = ArgumentString.Normalize(l));

        Options.Add(
            "suppression-time=",
            "A duration for which notifications are suppressed after the alert triggers, for example `10m` or `1h`",
            s => _suppressionTime = ArgumentString.Normalize(s));

        Options.Add(
            "notification-app=",
            "The id of an app instance that will be notified when the alert triggers; this argument can be used multiple times",
            a => _notificationApps.Add(ArgumentString.Normalize(a) ?? throw new ArgumentException("Notification apps require a value.")));

        Options.Add(
            "protected",
            "Specify that the alert is editable only by administrators",
            _ => _isProtected = true);

        Options.Add(
            "disabled",
            "Create the alert in a disabled state; disabled alerts are not processed and do not send notifications",
            _ => _isDisabled = true);

        _connection = Enable<ConnectionFeature>();
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var alert = await connection.Alerts.TemplateAsync();
        alert.OwnerId = null;

        alert.Title = _title;
        alert.Description = _description;
        alert.IsProtected = _isProtected;
        alert.IsDisabled = _isDisabled;

        if (_from != null)
            alert.DataSource = Enum.Parse<DataSource>(_from, ignoreCase: true);

        if (_signal != null)
            alert.SignalExpression = SignalExpressionParser.ParseExpression(_signal);

        if (_lateral.Any())
        {
            alert.Joins.Clear();
            foreach (var lateral in _lateral)
                alert.Joins.Add(ParseLateral(lateral));
        }

        if (_where != null)
            alert.Where = (await connection.Expressions.ToStrictAsync(_where)).StrictExpression;

        if (_select.Any())
        {
            alert.Select.Clear();
            foreach (var measurement in _select)
                alert.Select.Add(ParseColumn(measurement));
        }

        if (_groupBy.Any())
        {
            alert.GroupBy.Clear();
            foreach (var grouping in _groupBy)
                alert.GroupBy.Add(ParseGrouping(grouping));
        }

        if (_window != null)
            alert.TimeGrouping = DurationMoniker.ToTimeSpan(_window);

        if (_having != null)
            alert.Having = _having;

        if (_notificationLevel != null)
            alert.NotificationLevel = Enum.Parse<LogEventLevel>(LevelMapping.ToFullLevelName(_notificationLevel));

        if (_suppressionTime != null)
            alert.SuppressionTime = DurationMoniker.ToTimeSpan(_suppressionTime);

        foreach (var appInstanceId in _notificationApps)
            alert.NotificationChannels.Add(new NotificationChannelPart { NotificationAppInstanceId = appInstanceId });

        alert = await connection.Alerts.AddAsync(alert);

        _output.GetOutputFormat(config).WriteEntity(alert);

        return 0;
    }

    static ColumnPart ParseColumn(string measurement)
    {
        var parsed = AliasedExpressionParser.ParseExpression(measurement);
        return new ColumnPart { Value = parsed.Expression, Label = parsed.Alias };
    }

    static GroupingColumnPart ParseGrouping(string grouping)
    {
        var parsed = AliasedExpressionParser.ParseExpression(grouping, allowCaseInsensitive: true);
        return new GroupingColumnPart
        {
            Value = parsed.Expression,
            Label = parsed.Alias,
            IsCaseInsensitive = parsed.IsCaseInsensitive
        };
    }

    static JoinPart ParseLateral(string lateral)
    {
        var parsed = AliasedExpressionParser.ParseExpression(lateral);
        return new JoinPart
        {
            Kind = JoinKind.Lateral,
            SetFunctionCall = parsed.Expression,
            Alias = parsed.Alias
        };
    }
}
