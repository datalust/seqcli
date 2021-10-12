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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace SeqCli.Cli
{
    abstract class Command
    {
        readonly IList<CommandFeature> _features = new List<CommandFeature>();

        protected Command()
        {
            Options = new OptionSet();
        }

        public OptionSet Options { get; }

        public bool HasArgs => Options.Any();

        protected T Enable<T>()
            where T : CommandFeature, new()
        {
            var t = new T();
            return Enable(t);
        }

        protected T Enable<T>(T t)
            where T : CommandFeature
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            t.Enable(Options);
            _features.Add(t);
            return t;
        }

        public void PrintUsage()
        {
            var allOptions = new OptionSet();
            foreach (var option in Options)
            {
                allOptions.Add(option);
            }

            allOptions.Add("v|verbose", "Print verbose output to `STDERR`", _ => { });

            Console.Error.WriteLine("Arguments:");
            allOptions.WriteOptionDescriptions(Console.Error);
        }

        public async Task<int> Invoke(string[] args)
        {
            var unrecognised = Options.Parse(args).ToArray();

            var errs = _features.SelectMany(f => f.GetUsageErrors()).ToList();

            if (errs.Any())
            {
                ShowUsageErrors(errs);
                return 1;
            }

            return await Run(unrecognised);
        }

        protected virtual async Task<int> Run(string[] unrecognized)
        {
            if (unrecognized.Any())
            {
                ShowUsageErrors(new [] { "Unrecognized options: " + string.Join(", ", unrecognized) });
                return 1;
            }

            return await Run();
        }

        protected virtual Task<int> Run() { return Task.FromResult(0); }

        protected virtual void ShowUsageErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
#pragma warning disable Serilog004 // Constant MessageTemplate verifier
                Log.Error(error);
#pragma warning restore Serilog004 // Constant MessageTemplate verifier
            }
        }
    }
}
