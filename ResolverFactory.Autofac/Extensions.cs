using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace ResolverFactory.Autofac
{
    public static class Extensions
    {
        public static void RegisterResolverFactory(this ContainerBuilder builder)
        {
            builder.RegisterType<ResolverFactoryForAutofac>().As<ResolverFactory>().SingleInstance();
            builder.RegisterGeneric(typeof(ResolverAdapter<>)).As(typeof(IResolver<>)).InstancePerDependency();
        }

        public static IServiceScope ToServiceScope(this ILifetimeScope scope)
        {
            return new ScopeAdapter(scope);
        }
    }
}