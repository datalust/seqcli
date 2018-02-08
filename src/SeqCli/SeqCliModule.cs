using System.Reflection;
using Autofac;
using SeqCli.Cli;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli
{
    class SeqCliModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CommandLineHost>();
            builder.RegisterAssemblyTypes(typeof(Program).GetTypeInfo().Assembly)
                .As<Command>()
                .WithMetadataFrom<CommandAttribute>();
            builder.RegisterType<SeqConnectionFactory>();
            builder.Register(c => SeqCliConfig.Read()).SingleInstance();
        }
    }
}