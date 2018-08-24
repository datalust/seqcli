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
using System.Text;
using System.Threading.Tasks;
using Autofac;
using SeqCli.Cli;
using SeqCli.Util;
using Serilog;
using Serilog.Events;

namespace SeqCli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.Console(
                    outputTemplate: "{Message:lj}{NewLine}{Exception}",
                    standardErrorFromLevel: LevelAlias.Minimum)
                .CreateLogger();
            
            try
            {
                Console.InputEncoding =
                    Console.OutputEncoding =
                        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

                TaskScheduler.UnobservedTaskException += 
                    (s,e) => Log.Error(e.Exception, "Unobserved task exception");
                
                var builder = new ContainerBuilder();
                builder.RegisterModule<SeqCliModule>();

                using (var container = builder.Build())
                {
                    var clh = container.Resolve<CommandLineHost>();
                    var exit = await clh.Run(args);
                    return exit;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unhandled command exception");
                Log.Fatal("The command failed: {UnhandledExceptionMessage}", Presentation.FormattedMessage(ex));
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}