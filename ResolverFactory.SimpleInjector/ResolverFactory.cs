using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace ResolverFactory.SimpleInjector
{
    public class ResolverFactoryForSimpleInjector : ResolverFactory
    {
        private readonly Container _container;

        public ResolverFactoryForSimpleInjector(Container container)
        {
            _container = container;
        }

        protected override IServiceProvider ResolveProvider(ref IServiceScope? scope, ref bool? scopeRequiresDisposal)
        {
            if (scope == null)
            {
                var siScope = AsyncScopedLifestyle.BeginScope(_container);
                scope = siScope.ToServiceScope();
                scopeRequiresDisposal = true;
            }

            return scope.ServiceProvider;
        }
    }
}