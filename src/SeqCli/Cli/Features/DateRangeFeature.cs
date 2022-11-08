﻿// Copyright 2018 Datalust Pty Ltd
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

namespace SeqCli.Cli.Features
{
    class DateRangeFeature : CommandFeature
    {
        string _start, _end;

        public override void Enable(OptionSet options)
        {
            options.Add("start=", "ISO 8601 date/time to query from", v => _start = v);
            options.Add("end=", "ISO 8601 Date/time to query to", v => _end = v);
        }

        public DateTime? Start => _start != null ? DateTime.Parse(_start) : (DateTime?)null;
        public DateTime? End => _end != null ? DateTime.Parse(_end) : (DateTime?)null;
    }
}
