using Microsoft.Extensions.DependencyInjection;

namespace ResolverFactory.Autofac
{
    public class ResolverAdapter<TService> : IResolver<TService>
    where TService : notnull
    {
        private readonly IResolver<TService> _resolver;

        public ResolverAdapter(ResolverFactory factory)
        {
            _resolver = factory.CreateResolver<TService>();
        }

        public TResult Resolve<TResult>(Func<TService, TResult> onResolved, IServiceScope? scope = null)
        {
            return _resolver.Resolve(onResolved, scope);
        }

        public void Resolve(Action<TService> onResolved, IServiceScope? scope = null)
        {
            _resolver.Resolve(onResolved, scope);
        }
    }
}