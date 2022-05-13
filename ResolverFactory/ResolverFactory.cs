using System.Collections.Concurrent;

namespace ResolverFactory
{
    public interface IResolverFactory<TService>
    {
        Func<TResult> CreateResolver<TResult>(Func<TService, TResult> onResolved, IServiceScope? scope = null);
    }

    //The base class handles cycle detection
    //Cycle detection is limited here to invocations of any resolver within the same thread
    //This is intended to prevent infinite loops that may be hard to detect between dependencies
    public class ResolverFactory
    {
        private readonly static ConcurrentDictionary<int, HashSet<int>> _callRegistry;
        private readonly static object _lock;
        private static int _lastId;
        private readonly int _id;

        static ResolverFactory()
        {
            _callRegistry = new ConcurrentDictionary<int, HashSet<int>>();
            _lock = new();
            _lastId = 0;
        }

        public ResolverFactory()
        {
            lock (_lock)
            {
                _id = ++_lastId;
            }
        }

        protected void RegisterForCycleDetection()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var set = _callRegistry.GetOrAdd(threadId, new HashSet<int>());

            if (!set.Add(_id))
            {
                throw new InvalidOperationException("Cycle detected by ResolverFactory RegisterForCycleDetection.");
            }
        }

        protected void UnregisterFromCycleDetection()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;

            if (!_callRegistry.TryGetValue(threadId, out var set) || !set.Remove(_id))
            {
                throw new Exception("A ResolverFactory call registry was deconstructed in an unexpected way.");
            }

            if (set!.Count == 0)
            {
                if (!_callRegistry.Remove(threadId, out _))
                {
                    throw new Exception("A ResolverFactory call registry was deconstructed in an unexpected way.");
                }
            }
        }

    }

    public class ResolverFactory<TService> : ResolverFactory, IResolverFactory<TService>
    where TService : notnull
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IServiceScopeFactory _scopeFactory;

        public ResolverFactory(IHttpContextAccessor contextAccessor, IServiceScopeFactory scopeFactory)
        {
            _contextAccessor = contextAccessor;
            _scopeFactory = scopeFactory;
        }

        private IServiceProvider? Provider
        {
            get
            {
                return _contextAccessor.HttpContext?.RequestServices;
            }
        }

        //The first layer of resolution wraps subsequent calls within cycle detection
        private Func<TResult> CreateL1Resolver<TResult>(Func<TResult> l2Resolver)
        {
            return () =>
            {
                RegisterForCycleDetection();

                try
                {
                    return l2Resolver();
                }
                finally
                {
                    UnregisterFromCycleDetection();
                }
            };
        }

        //The second layer of resolution focuses on scope and provider resolution
        //If a scope is provided this is the default source for service resolution
        //If a scope is not provided the resolver will try to get the current HttpContext's service collection
        //If an HttpContext is not found, the call is outside of scope, so a scope for this call is created and is disposed on completion
        private Func<TResult> CreateL2Resolver<TResult>(Func<IServiceProvider, TResult> l3Resolver, IServiceScope? scope)
        {
            if (scope != null)
            {
                return () => l3Resolver(scope.ServiceProvider);

            }
            else if (Provider != null)
            {
                return () => l3Resolver(Provider);
            }
            else
            {
                return () =>
                {
                    var newScope = _scopeFactory.CreateScope();

                    try
                    {
                        return l3Resolver(newScope.ServiceProvider);
                    }
                    finally
                    {
                        newScope.Dispose();
                    }
                };
            }
        }

        //The third layer of resolution resolves the service and invokes the onResolved callback
        private Func<IServiceProvider, TResult> CreateL3Resolver<TResult>(Func<TService, TResult> onResolved)
        {
            return (IServiceProvider provider) =>
            {
                var service = provider.GetRequiredService<TService>();
                return onResolved(service);
            };
        }

        //Finally, CreateResolver puts all of the pieces together to construct the resolver
        public Func<TResult> CreateResolver<TResult>(Func<TService, TResult> onResolved, IServiceScope? scope = null)
        {
            var l3Resolver = CreateL3Resolver(onResolved);
            var l2Resolver = CreateL2Resolver(l3Resolver, scope);
            var l1Resolver = CreateL1Resolver(l2Resolver);
            return l1Resolver;
        }
    }

    // A simplified implementation intended for mocking
    public class SimplifiedResolverFactory<TService> : IResolverFactory<TService>
    {
        private TService Service { get; set; }

        public SimplifiedResolverFactory(TService service)
        {
            Service = service;
        }

        // For a unit test, we shouldn't really need to worry about scope here, so scope is ignored
        public Func<TReturn> CreateResolver<TReturn>(Func<TService, TReturn> onResolved, IServiceScope? _)
        {
            return () =>
            {
                return onResolved(Service);
            };
        }
    }

    public static class ResolverFactoryExtensions
    {
        //Resolver factories are added as singletons for cycle detection
        public static IServiceCollection AddResolverFactory<TService>(this IServiceCollection collection)
        where TService : notnull
        {
            return collection.AddSingleton<IResolverFactory<TService>, ResolverFactory<TService>>();
        }
    }
}



