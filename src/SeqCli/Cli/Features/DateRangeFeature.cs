using System;

namespace SeqCli.Cli.Features
{
    class DateRangeFeature : CommandFeature
    {
        string _start, _end;
        bool _noDefaultRange;
        readonly DateTime _now = DateTime.UtcNow;

        public override void Enable(OptionSet options)
        {
            options.Add("start=", "ISO 8601 date/time to query from (default: now - 24h)", v => _start = v);
            options.Add("end=", "Date/time to query to (default: now)", v => _end = v);
            options.Add("n|no-default-range", "If specified, missing `--start` and `--end` values will not be defaulted",
                _ => _noDefaultRange = true);
        }

        public DateTime? Start => _start != null ? DateTime.Parse(_start) : _noDefaultRange ? (DateTime?)null : _now - TimeSpan.FromDays(1);
        public DateTime? End => _end != null ? DateTime.Parse(_end) : _noDefaultRange ? (DateTime?)null : _now;
    }
}
