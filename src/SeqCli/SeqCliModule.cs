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

using System.Reflection;
using Autofac;
using SeqCli.Cli;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Encryptor;

namespace SeqCli;

class SeqCliModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(new StoragePathFeature());
        builder.RegisterType<CommandLineHost>();
        builder.RegisterAssemblyTypes(typeof(Program).GetTypeInfo().Assembly)
            .As<Command>()
            .WithMetadataFrom<CommandAttribute>();
        builder.RegisterType<SeqConnectionFactory>();
        builder.Register(c => RuntimeConfigurationLoader.Load(c.Resolve<StoragePathFeature>())).SingleInstance();
        builder.Register(c => c.Resolve<SeqCliConfig>().Connection).SingleInstance();
        builder.Register(c => c.Resolve<SeqCliConfig>().Output).SingleInstance();
        builder.Register(c => c.Resolve<SeqCliConfig>().Encryption.DataProtector()).As<IDataProtector>();
    }
}