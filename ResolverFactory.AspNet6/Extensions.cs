using Microsoft.Extensions.DependencyInjection;

namespace ResolverFactory.AspNet6
{
    public static class Extensions
    {
        public static IServiceCollection AddResolverFactoryForAspNet6(this IServiceCollection collection)
        {
            return collection
                .AddSingleton<ResolverFactory, ResolverFactoryForAspNet6>()
                .AddTransient(typeof(IResolver<>), typeof(ResolverAdapter<>));
        }
    }
}