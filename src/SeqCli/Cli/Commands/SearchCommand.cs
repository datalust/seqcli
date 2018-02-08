using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api.Model.Events;
using Seq.Api.Model.Shared;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace SeqCli.Cli.Commands
{
    [Command("search", "Retrieve log events that match a given filter",
        Example = "seqcli search -f \"@Exception like '%TimeoutException%'\" -c 30")]
    class SearchCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;
        readonly DateRangeFeature _range;
        string _filter;
        int _count = 1;

        public SearchCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "f=|filter=",
                "A filter to apply to the search, for example `Host = 'xmpweb-01.example.com'`",
                v => _filter = v);
            Options.Add(
                "c=|count=",
                $"The maximum number of events to retrieve; the default is {_count}",
                v => _count = int.Parse(v));

            _range = Enable<DateRangeFeature>();
            _output = Enable<OutputFormatFeature>();
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            try
            {
                using (var output = _output.CreateOutputLogger())
                {
                    var connection = _connectionFactory.Connect(_connection);

                    string filter = null;
                    if (!string.IsNullOrWhiteSpace(_filter))
                        filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression;

                    string lastReadEventId = null;
                    var remaining = _count;
                    while (remaining > 0)
                    {
                        var resultSet = await connection.Events.InSignalAsync(
                            filter: filter,
                            count: remaining,
                            fromDateUtc: _range.Start,
                            toDateUtc: _range.End,
                            afterId: lastReadEventId);

                        foreach (var evt in resultSet.Events)
                        {
                            remaining++;
                            output.Write(ToSerilogEvent(evt));
                        }

                        if (resultSet.Statistics.Status != ResultSetStatus.Partial)
                            break;

                        lastReadEventId = resultSet.Statistics.LastReadEventId;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not retrieve search result: {ErrorMessage}", ex.Message);
                return 1;
            }
        }

        LogEvent ToSerilogEvent(EventEntity evt)
        {
            // Not ideal; we use Serilog types throughout, currently, but this doesn't allow
            // for non-Serilog level names.
            if (!Enum.TryParse(evt.Level, true, out LogEventLevel level))
                level = LogEventLevel.Information;

            return new LogEvent(
                DateTimeOffset.Parse(evt.Timestamp),
                level,
                string.IsNullOrWhiteSpace(evt.Exception) ? null : new TextException(evt.Exception),
                new MessageTemplate(evt.MessageTemplateTokens.Select(ToMessageTemplateToken)),
                evt.Properties.Select(p => CreateProperty(p.Name, p.Value)));
        }

        MessageTemplateToken ToMessageTemplateToken(MessageTemplateTokenPart mttp)
        {
            // Also not ideal, we lose renderings, alignment etc. here.

            if (mttp.Text != null)
                return new TextToken(mttp.Text);
            return new PropertyToken(mttp.PropertyName, mttp.RawText ?? $"{{{mttp.PropertyName}}}");
        }

        LogEventProperty CreateProperty(string name, object value)
        {
            return new LogEventProperty(name, CreatePropertyValue(value));
        }

        LogEventPropertyValue CreatePropertyValue(object value)
        {
            var d = value as IDictionary<string, object>;
            if (d != null)
            {
                object tt;
                d.TryGetValue("$typeTag", out tt);
                return new StructureValue(
                    d.Where(kvp => kvp.Key != "$typeTag").Select(kvp => CreateProperty(kvp.Key, kvp.Value)),
                    tt as string);
            }

            var dd = value as IDictionary;
            if (dd != null)
            {
                return new DictionaryValue(dd.Keys
                    .Cast<object>()
                    .Select(k => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                        (ScalarValue)CreatePropertyValue(k),
                        CreatePropertyValue(dd[k]))));
            }

            if (value == null || value is string || !(value is IEnumerable))
            {
                return new ScalarValue(value);
            }

            var enumerable = (IEnumerable)value;
            return new SequenceValue(enumerable.Cast<object>().Select(CreatePropertyValue));
        }
    }
}
