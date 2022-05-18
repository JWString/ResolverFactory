using Microsoft.Extensions.DependencyInjection;
using ResolverFactory.SimpleInjector;
using Services;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace ContainerFixtures
{
    public class SimpleInjectorContainerFixture : ContainerFixture
    {
        private bool _disposed;
        private readonly Container _container;

        public SimpleInjectorContainerFixture()
        {
            var container = new Container();
            container.RegisterResolverFactory();
            container.RegisterWithResolver<CycleServiceA>(Lifestyle.Singleton);
            container.RegisterWithResolver<CycleServiceB>(Lifestyle.Singleton);
            container.RegisterWithResolver<CycleServiceC>(Lifestyle.Singleton);
            container.RegisterWithResolver<StandardServiceA>(Lifestyle.Transient);
            container.RegisterWithResolver<StandardServiceB>(Lifestyle.Transient);
            container.RegisterWithResolver<StandardServiceC>(Lifestyle.Transient);
            container.RegisterWithResolver<StandardService>(Lifestyle.Transient);

            _container = container;
        }

        public override IServiceProvider ServiceProvider => _container;

        public override ResolverFactory.ResolverFactory CreateFactory()
        {
            return new ResolverFactoryForSimpleInjector(_container);
        }

        public override IServiceScope CreateScope()
        {
            return AsyncScopedLifestyle.BeginScope(_container).ToServiceScope();
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                _container.Dispose();
                _disposed = true;
            }
        }
    }
}