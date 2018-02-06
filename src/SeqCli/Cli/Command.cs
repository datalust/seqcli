using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            if (Options.Any())
            {
                Console.Error.WriteLine("Arguments:");
                Options.WriteOptionDescriptions(Console.Error);
            }
        }

        public async Task<int> Invoke(string[] args)
        {
            try
            {
                var unrecognised = Options.Parse(args).ToArray();

                var errs = _features.SelectMany(f => f.GetUsageErrors()).ToList();

                if (errs.Any())
                {
                    ShowUsageErrors(errs);
                    return -1;
                }

                return await Run(unrecognised);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        protected virtual async Task<int> Run(string[] unrecognised)
        {
            if (unrecognised.Any())
            {
                ShowUsageErrors(new [] { "Unrecognized options: " + string.Join(", ", unrecognised) });
                return -1;
            }

            return await Run();
        }

        protected virtual Task<int> Run() { return Task.FromResult(0); }

        protected virtual void ShowUsageErrors(IEnumerable<string> errors)
        {
            var header = "Error:";
            foreach (var error in errors)
            {
                Printing.Define(header, error, 7, Console.Out);
                header = new string(' ', header.Length);
            }
        }

        protected bool Require(string value, string name)
        {
            if (!Requirement.IsNonEmpty(value))
            {
                ShowUsageErrors(new [] { Requirement.NonEmptyDescription(name) });
                return false;
            }
            return true;
        }
    }
}
