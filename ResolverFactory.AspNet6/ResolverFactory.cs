using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ResolverFactory.AspNet6
{
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
}