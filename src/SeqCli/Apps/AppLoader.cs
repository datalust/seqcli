// Copyright 2019 Datalust Pty Ltd
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Seq.Apps;
using Serilog;

namespace SeqCli.Apps
{
    class AppLoader : IDisposable
    {
        readonly string _packageBinaryPath;

        // These are used for interop between the host process and the app. The
        // app _must_ be able to load on the unified version.
        readonly Assembly[] _contracts =
        {
            typeof(SeqApp).Assembly,
            typeof(Log).Assembly,
        };
        
        public AppLoader(string packageBinaryPath)
        {
            _packageBinaryPath = packageBinaryPath ?? throw new ArgumentNullException(nameof(packageBinaryPath));
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public bool TryLoadSeqAppType(string? seqAppTypeName, [NotNullWhen(true)] out Type? seqAppType)
        {
            seqAppType = null;
            var packageAssemblies = LoadPackageAssemblies();

            foreach (var loadedAssembly in packageAssemblies)
            {
                if (seqAppTypeName != null)
                {
                    seqAppType = loadedAssembly.GetType(seqAppTypeName);
                    if (seqAppType != null)
                        return true;
                }
                else
                {
                    foreach (var exportedType in loadedAssembly.ExportedTypes)
                    {
                        if (exportedType.GetCustomAttribute<SeqAppAttribute>() != null)
                        {
                            if (seqAppType == null)
                                seqAppType = exportedType;
                            else
                                throw new InvalidOperationException(
                                    "More than one [SeqApp] type was found; specify an app type explicitly on the command line.");
                        }
                    }
                }
            }

            if (seqAppType == null && seqAppTypeName != null)
                seqAppType = Type.GetType(seqAppTypeName);

            return seqAppType != null;
        }

        IEnumerable<Assembly> LoadPackageAssemblies()
        {
            var loaded = new Dictionary<string, Assembly>();

            foreach (var assemblyFile in Directory.GetFiles(_packageBinaryPath, "*.dll"))
            {
                var fn = Path.GetFileNameWithoutExtension(assemblyFile);

                if (_contracts
                    .Any(hosted => hosted.GetName().Name!.Equals(fn, StringComparison.OrdinalIgnoreCase)))
                    continue;

                try
                {
                    var assembly = Assembly.LoadFrom(assemblyFile);
                    loaded.Add(assembly.FullName!, assembly);
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }
            }

            return loaded.Values;
        }

        Assembly? OnAssemblyResolve(object? _, ResolveEventArgs e)
        {
            var target = new AssemblyName(e.Name);

            foreach (var contract in _contracts)
            {
                if (contract.GetName().Name!.Equals(target.Name))
                    return contract;
            }

            var assemblyFile = Path.Combine(_packageBinaryPath, target.Name + ".dll");
            if (File.Exists(assemblyFile)) return Assembly.LoadFrom(assemblyFile);
            
            return null;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }
    }
}
