using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Seq.Api;
using Serilog;

namespace SeqCli.EndToEnd.Support
{
    // Built-in decorator support didn't transfer metadata.
    class IsolatedTestCaseRegistrationSource : IRegistrationSource
    {
        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
        {
            if (!(service is TypedService ts && ts.ServiceType == typeof(IsolatedTestCase)))
                yield break;

            var innerService = ts.ChangeType(typeof(ICliTestCase));
            foreach (var inner in registrationAccessor(innerService))
            {
                yield return new ComponentRegistration(
                    Guid.NewGuid(),
                    new DelegateActivator(typeof(IsolatedTestCase), (ctx, p) =>
                    {
                        var tc = (ICliTestCase) ctx.ResolveComponent(new ResolveRequest(innerService, inner, p));
                        return new IsolatedTestCase(
                            ctx.Resolve<Lazy<ITestProcess>>(),
                            ctx.Resolve<Lazy<SeqConnection>>(),
                            ctx.Resolve<Lazy<ILogger>>(),
                            ctx.Resolve<CliCommandRunner>(),
                            ctx.Resolve<Lazy<LicenseSetup>>(),
                            tc);
                    }),
                    new CurrentScopeLifetime(),
                    InstanceSharing.None,
                    InstanceOwnership.OwnedByLifetimeScope,
                    new[] {service},
                    inner.Metadata);
            }
        }

        public bool IsAdapterForIndividualComponents { get; } = true;
    }
}