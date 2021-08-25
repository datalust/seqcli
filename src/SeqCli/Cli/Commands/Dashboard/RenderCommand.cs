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
using Newtonsoft.Json;
using Seq.Api.Model.Dashboarding;
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Syntax;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Dashboard
{
    [Command("dashboard", "render", "Produce a CSV or JSON result set from a dashboard chart", 
        Example="seqcli dashboard render -i dashboard-159 -c 'Response Time (ms)' --last 7d --by 1h")]
    class RenderCommand : Command
    {
        const int MaximumReturnedHitRows = 10000;

        readonly SeqConnectionFactory _connectionFactory;

        readonly DateRangeFeature _range;
        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;
        readonly SignalExpressionFeature _signal;
        readonly TimeoutFeature _timeout;

        string _id, _lastDuration, _intervalDuration, _chartTitle;

        public RenderCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "i=|id=",
                "The id of a single dashboard to render",
                t => _id = ArgumentString.Normalize(t));
            Options.Add("c=|chart=", "The title of a chart on the dashboard to render",
                c => _chartTitle = ArgumentString.Normalize(c));
            Options.Add("last=",
                "A duration over which the chart should be rendered, e.g. `7d`; this will be aligned to an interval boundary; either `--last` or `--start` and `--end` must be specified",
                v => _lastDuration = ArgumentString.Normalize(v));
            Options.Add("by=",
                "The time-slice interval for the chart data, as a duration, e.g. `1h`",
                v => _intervalDuration = ArgumentString.Normalize(v));
            _range = Enable<DateRangeFeature>();
            _signal = Enable<SignalExpressionFeature>();
            _timeout = Enable<TimeoutFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            if (_id == null)
            {
                Log.Error("A dashboard id is required");
                return 1;
            }

            if (_chartTitle == null)
            {
                Log.Error("A chart name is required");
                return 1;
            }

            if (_range.Start == null && _range.End != null ||
                _range.End == null && _range.Start != null)
            {
                Log.Error("If `--start` or `--end` is specified, both must be specified");
                return 1;
            }

            if (_lastDuration == null && _range.Start == null)
            {
                Log.Error("A `--last` duration, or range `--start` and `--end`, is required");
                return 1;
            }

            if (_lastDuration != null && _range.Start != null)
            {
                Log.Error("Only one of either `--last` or a `--start`/`--end` range can be specified");
                return 1;
            }

            var dashboard = await connection.Dashboards.FindAsync(_id);
            var charts = dashboard.Charts.Where(c => c.Title == _chartTitle).ToArray();

            if (charts.Length == 0)
            {
                Log.Error("No matching chart was found");
                return 1;
            }

            if (charts.Length > 1)
            {
                Log.Error("More than one matching chart was found");
                return 1;
            }

            var chart = charts.Single();
            var query = chart.Queries.Single();

            var signal = Intersect(
                _signal.Signal, 
                dashboard.SignalExpression,
                chart.SignalExpression,
                query.SignalExpression);

            TimeSpan? timeGrouping = null;
            if (_intervalDuration != null)
                timeGrouping = DurationMoniker.ToTimeSpan(_intervalDuration);

            DateTime rangeStart, rangeEnd;
            if (_range.Start.HasValue)
            {
                rangeStart = _range.Start.Value;
                // ReSharper disable once PossibleInvalidOperationException
                rangeEnd = _range.End.Value;
            }
            else
            {
                // Note, this is local time.
                rangeEnd = DateTime.Now;
                var last = DurationMoniker.ToTimeSpan(_lastDuration);
                rangeStart = rangeEnd - last;
                if (timeGrouping.HasValue)
                {
                    if (timeGrouping.Value >= TimeSpan.FromDays(1))
                        rangeStart = new DateTime(rangeStart.Year, rangeStart.Month, rangeStart.Day, 0, 0, 0, DateTimeKind.Local);
                    else if (timeGrouping.Value >= TimeSpan.FromHours(1))
                        rangeStart = new DateTime(rangeStart.Year, rangeStart.Month, rangeStart.Day, rangeStart.Hour, 0, 0, DateTimeKind.Local);
                    else if (timeGrouping.Value >= TimeSpan.FromMinutes(1))
                        rangeStart = new DateTime(rangeStart.Year, rangeStart.Month, rangeStart.Day, rangeStart.Hour, rangeStart.Minute, 0, DateTimeKind.Local);
                    else
                        rangeStart = new DateTime(rangeStart.Year, rangeStart.Month, rangeStart.Day, rangeStart.Hour, rangeStart.Minute, rangeStart.Second, DateTimeKind.Local);
                }
            }

            var q = BuildSqlQuery(query, rangeStart, rangeEnd, timeGrouping);

            var timeout = _timeout.ApplyTimeout(connection.Client.HttpClient);
            
            if (_output.Json)
            {
                var result = await connection.Data.QueryAsync(q, signal: signal, timeout: timeout);

                // Some friendlier JSON output is definitely possible here
                Console.WriteLine(JsonConvert.SerializeObject(result));
            }
            else
            {
                var result = await connection.Data.QueryCsvAsync(q, signal: signal, timeout: timeout);
                _output.WriteCsv(result);
            }

            return 0;
        }

        static string BuildSqlQuery(ChartQueryPart query, DateTime rangeStart, DateTime rangeEnd, TimeSpan? timeGrouping)
        {
            var sql = new QueryBuilder();

            foreach (var measurement in query.Measurements)
                sql.Select(measurement.Value, measurement.Label);

            sql.FromStream = true;

            sql.Where($"@Timestamp >= DateTime('{rangeStart:O}')");
            sql.Where($"@Timestamp < DateTime('{rangeEnd:O}')");

            if (!string.IsNullOrEmpty(query.Where))
                sql.Where(query.Where);

            if (timeGrouping.HasValue)
                sql.GroupBy(timeGrouping.Value);

            foreach (var grouping in query.GroupBy)
                sql.GroupBy(grouping);            

            sql.Limit = MaximumReturnedHitRows;

            return sql.Build();
        }

        static SignalExpressionPart Intersect(params SignalExpressionPart[] expressions)
        {
            var result = (SignalExpressionPart) null;

            foreach (var s in expressions)
            {
                if (result == null)
                    result = s;
                else if (s != null)
                    result = SignalExpressionPart.Intersection(result, s);
            }

            return result;
        }
    }
}
