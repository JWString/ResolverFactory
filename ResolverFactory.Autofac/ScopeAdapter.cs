using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace ResolverFactory.Autofac
{
    internal class ScopeAdapter : IServiceScope, IServiceProvider
    {
        private bool _disposed;
        private readonly ILifetimeScope _afScope;

        public ScopeAdapter(ILifetimeScope afScope)
        {
            _afScope = afScope;
        }

        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType)
        {
            return _afScope.Resolve(serviceType);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _afScope.Dispose();
                _disposed = true;
            }
        }
    }
}