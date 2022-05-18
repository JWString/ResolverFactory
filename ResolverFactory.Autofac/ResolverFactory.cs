using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace ResolverFactory.Autofac
{
    public class ResolverFactoryForAutofac : ResolverFactory
    {
        readonly ILifetimeScope _scope;

        public ResolverFactoryForAutofac(ILifetimeScope scope)
        {
            _scope = scope;
        }

        protected override IServiceProvider ResolveProvider(ref IServiceScope? scope, ref bool? scopeRequiresDisposal)
        {
            if (scope == null)
            {
                var afScope = _scope.BeginLifetimeScope();
                scope = afScope.ToServiceScope();
                scopeRequiresDisposal = true;
            }

            return scope.ServiceProvider;
        }
    }
}