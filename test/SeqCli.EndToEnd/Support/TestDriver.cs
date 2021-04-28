using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;

namespace SeqCli.EndToEnd.Support
{
    class TestDriver
    {
        readonly TestConfiguration _configuration;
        readonly IEnumerable<Meta<Func<Owned<IsolatedTestCase>>>> _cases;

        public TestDriver(
            TestConfiguration configuration,
            IEnumerable<Meta<Func<Owned<IsolatedTestCase>>>> cases)
        {
            _configuration = configuration;
            _cases = cases;
        }

        public async Task<int> Run()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"TESTING {_configuration.TestedBinary}");
            Console.ResetColor();

            var count = 0;
            var passed = new List<string>();
            var failed = new List<string>();

            foreach (var testCaseFactory in _cases.OrderBy(c => Guid.NewGuid()))
            {
                var isMultiuser = testCaseFactory.Metadata.TryGetValue("Multiuser", out var multiuser) && true.Equals(multiuser);
                if (isMultiuser != _configuration.IsMultiuser)
                    continue;

                count++;

                await using var testCase = testCaseFactory.Value();
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"RUNNING {testCase.Value.Description.PadRight(50)}");
                Console.ResetColor();

                try
                {
                    await testCase.Value.ExecuteTestCaseAsync();

                    passed.Add(testCase.Value.Description);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("PASS");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    failed.Add(testCase.Value.Description);

                    Console.Write(testCase.Value.Output);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FAIL");
                    Console.WriteLine(ex);
                    Console.ResetColor();
                }
            }

            var (color, failMsg) = failed.Count != 0 ? (ConsoleColor.Red, "Failures:") : (ConsoleColor.Green, "");
            Console.ForegroundColor = color;
            Console.WriteLine($"Done; {count} total, {passed.Count} pass, {failed.Count} fail. {failMsg}");

            foreach (var fail in failed)
            {
                Console.WriteLine($"   {fail}");
            }

            Console.ResetColor();
            return failed.Count;
        }
    }
}

