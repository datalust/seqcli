using System.Reflection;
using Autofac;
using SeqCli.Cli;

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
        }
    }
}