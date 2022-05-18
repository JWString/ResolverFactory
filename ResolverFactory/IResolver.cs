using Microsoft.Extensions.DependencyInjection;

namespace ResolverFactory
{
    public interface IResolver<TService>
    {
        TResult Resolve<TResult>(Func<TService, TResult> onResolved, IServiceScope? scope = null);
        void Resolve(Action<TService> onResolved, IServiceScope? scope = null);
    }
}