using System.Collections.Concurrent;

namespace Services
{
    public interface IResolver<TService>
    {
        TResult Resolve<TResult>(Func<TService, TResult> onResolved, IServiceScope? scope = null);
        void Resolve(Action<TService> onResolved, IServiceScope? scope = null);
    }

    public abstract class ResolverFactory
    {
        private class ResolutionContext
        {
            public HashSet<Type> CallRegistry = new();
            public IServiceScope? Scope = null;
            public bool? ScopeRequiresDisposal = null;
        }

        protected class Resolver<TService> : IResolver<TService>
        where TService : notnull
        {
            private readonly Func<Type, IServiceScope?, IServiceProvider> _registerWithResolutionContext;
            private readonly Action<Type> _unregisterFromResolutionContext;

            public Resolver(
                Func<Type, IServiceScope?, IServiceProvider> registerWithResolutionContext,
                Action<Type> unregisterFromResolutionContext)
            {
                _registerWithResolutionContext = registerWithResolutionContext;
                _unregisterFromResolutionContext = unregisterFromResolutionContext;
            }

            public virtual TResult Resolve<TResult>(Func<TService, TResult> onResolved, IServiceScope? scope = null)
            {
                var provider = _registerWithResolutionContext(typeof(TService), scope);

                try
                {
                    var service = provider.GetRequiredService<TService>();
                    return onResolved(service);
                }
                finally
                {
                    _unregisterFromResolutionContext(typeof(TService));
                }
            }

            public virtual void Resolve(Action<TService> onResolved, IServiceScope? scope = null)
            {
                var provider = _registerWithResolutionContext(typeof(TService), scope);

                try
                {
                    var service = provider.GetRequiredService<TService>();
                    onResolved(service);
                }
                finally
                {
                    _unregisterFromResolutionContext(typeof(TService));
                }
            }
        }

        private readonly ConcurrentDictionary<int, ResolutionContext> _contexts;

        public ResolverFactory()
        {
            _contexts = new ConcurrentDictionary<int, ResolutionContext>();
        }

        protected IServiceProvider RegisterWithResolutionContext(Type type, IServiceScope? scope)
        {
            var threadId = Environment.CurrentManagedThreadId;
            var context = _contexts.GetOrAdd(threadId, new ResolutionContext());

            if (!context.CallRegistry.Add(type))
            {
                throw new InvalidOperationException("Cycle detected by ResolverFactory RegisterWithResolutionContext.");
            }

            IServiceProvider provider;

            if (scope != null)
            {
                if (context.Scope == null)
                {
                    context.Scope = scope;
                }

                bool? ignore = null;
                provider = ResolveProvider(ref scope, ref ignore);
            }
            else
            {
                provider = ResolveProvider(ref context.Scope, ref context.ScopeRequiresDisposal);
            }

            return provider;
        }

        protected void UnregisterFromResolutionContext(Type type)
        {
            var threadId = Environment.CurrentManagedThreadId;
            ResolutionContext? context = null;

            try
            {
                if (!_contexts.TryGetValue(threadId, out context) || !context.CallRegistry.Remove(type))
                {
                    throw new Exception("A ResolverFactory call registry was deconstructed in an unexpected way.");
                }

                if (context?.CallRegistry.Count == 0 && !_contexts.Remove(threadId, out _))
                {
                    throw new Exception("A ResolverFactory call registry was deconstructed in an unexpected way.");
                }
            }
            finally
            {
                if (context?.CallRegistry.Count == 0 && context.ScopeRequiresDisposal.HasValue && context.ScopeRequiresDisposal.Value)
                {
                    context.Scope!.Dispose();
                }
            }
        }

        protected abstract IServiceProvider ResolveProvider(ref IServiceScope? scope, ref bool? scopeRequiresDisposal);

        public virtual IResolver<TService> CreateResolver<TService>()
        where TService : notnull
        {
            return new Resolver<TService>(RegisterWithResolutionContext, UnregisterFromResolutionContext);
        }

    }

    public class ResolverFactoryForAspNet6 : ResolverFactory
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IServiceScopeFactory _scopeFactory;

        public ResolverFactoryForAspNet6(IHttpContextAccessor contextAccessor, IServiceScopeFactory scopeFactory)
        {
            _contextAccessor = contextAccessor;
            _scopeFactory = scopeFactory;
        }

        protected override IServiceProvider ResolveProvider(ref IServiceScope? scope, ref bool? scopeRequiresDisposal)
        {
            IServiceProvider provider;

            if (scope != null)
            {
                provider = scope.ServiceProvider;
            }
            else if (_contextAccessor.HttpContext != null)
            {
                provider = _contextAccessor.HttpContext!.RequestServices;
            }
            else
            {
                scope = _scopeFactory.CreateScope();
                scopeRequiresDisposal = true;
                provider = scope.ServiceProvider;
            }

            return provider;
        }
    }

    public static class ResolverFactoryExtensions
    {
        public static IServiceCollection AddResolverFactoryForAspNet6(this IServiceCollection collection)
        {
            return collection.AddSingleton(typeof(ResolverFactory), typeof(ResolverFactoryForAspNet6));
        }

        public static IServiceCollection AddResolverForService<TService>(this IServiceCollection collection)
        where TService : notnull
        {
            return collection.AddTransient(provider => provider.GetRequiredService<ResolverFactory>().CreateResolver<TService>());
        }
    }
}