// Copyright 2018 Datalust Pty Ltd
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
using System.Globalization;
using System.Linq;

namespace SeqCli.Syntax
{
    static class DurationMoniker
    {
        public static string FromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero || Math.Abs(timeSpan.Ticks) < TimeSpan.FromMilliseconds(1).Ticks)
                return "0d";

            return new[] {
                Component(timeSpan.TotalDays, "d"),
                Component(timeSpan.TotalHours, "h"),
                Component(timeSpan.TotalMinutes, "m"),
                Component(timeSpan.TotalSeconds, "s"),
                Component(timeSpan.TotalMilliseconds, "ms")
            }.First(c => c != "");
        }

        static string Component(double value, string moniker)
        {
            if (moniker == null) throw new ArgumentNullException(nameof(moniker));

            if (Math.Abs(value) < double.Epsilon || Math.Abs(value - (int)value) > double.Epsilon)
                return "";

            return ((int)value).ToString(CultureInfo.InvariantCulture) + moniker;
        }

        public static TimeSpan ToTimeSpan(string duration)
        {
            if (duration == null) throw new ArgumentNullException(nameof(duration));

            // This is not at all robust; we could use a decent duration parser for use here in `seqcli`.

            if (duration.EndsWith("ms"))
                return TimeSpan.FromMilliseconds(int.Parse(duration.Substring(0, duration.Length - 2)));

            var value = int.Parse(duration.Substring(0, duration.Length - 1), CultureInfo.InvariantCulture);
            switch (duration[^1])
            {
                case 'd':
                    return TimeSpan.FromDays(value);
                case 'h':
                    return TimeSpan.FromHours(value);
                case 'm':
                    return TimeSpan.FromMinutes(value);
                case 's':
                    return TimeSpan.FromSeconds(value);
                default:
                    throw new ArgumentException($"Unrecognized duration `{duration}`.");
            }
        }
    }
}
