using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using System.Collections.Concurrent;

namespace ResolverFactory.SimpleInjector
{
    internal class ScopeAdapter : IServiceScope, IServiceProvider
    {
        private bool _disposed;
        private readonly Scope _siScope;
        private ConcurrentStack<IDisposable> _disposables;

        public ScopeAdapter(Scope siScope)
        {
            _siScope = siScope;
            _disposables = new();
        }

        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType)
        {
            var instance = _siScope.Container!.GetInstance(serviceType);

            if (_siScope.TypeIsDisposableTransient(serviceType))
            {
                _disposables.Push((IDisposable)instance);
            }

            return instance;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _siScope.Dispose();

                foreach (var d in _disposables)
                {
                    d.Dispose();
                }

                _disposed = true;
            }
        }
    }
}