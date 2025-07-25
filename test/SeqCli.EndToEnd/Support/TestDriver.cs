﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;

namespace SeqCli.EndToEnd.Support;

class TestDriver
{
    readonly IEnumerable<Meta<Func<Owned<IsolatedTestCase>>>> _cases;

    public TestDriver(
        IEnumerable<Meta<Func<Owned<IsolatedTestCase>>>> cases)
    {
        _cases = cases;
    }

    public async Task<int> Run()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"TESTING {TestConfiguration.TestedBinary}");
        Console.ResetColor();

        int count = 0, passedCount = 0, skippedCount = 0;
        var failed = new List<string>();

        await Parallel.ForEachAsync(_cases.OrderBy(_ => Guid.NewGuid()), async (testCaseFactory, _) =>
        {
            count++;

            await using var testCase = testCaseFactory.Value();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"RUNNING {testCase.Value.Description,-50}");
            Console.ResetColor();

            var isMultiuser = testCaseFactory.Metadata.TryGetValue("Multiuser", out var multiuser) &&
                              true.Equals(multiuser);
            testCaseFactory.Metadata.TryGetValue("MinimumApiVersion", out var minSeqVersion);
            if (isMultiuser != testCase.Value.Configuration.IsMultiuser || minSeqVersion != null &&
                !await testCase.Value.IsSupportedApiVersion((string)minSeqVersion))
            {
                skippedCount++;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("SKIP");
                return;
            }

            try
            {
                await testCase.Value.ExecuteTestCaseAsync();
                passedCount++;
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
        });

        var (color, failMsg) = failed.Count != 0 ? (ConsoleColor.Red, "Failures:") : (ConsoleColor.Green, "");
        Console.ForegroundColor = color;
        Console.WriteLine($"Done; {count} total, {passedCount} passed, {failed.Count} failed, {skippedCount} skipped. {failMsg}");

        foreach (var fail in failed)
        {
            Console.WriteLine($"   {fail}");
        }

        Console.ResetColor();
        return failed.Count;
    }
}