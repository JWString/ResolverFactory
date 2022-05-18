using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ResolverFactory.Autofac;
using Services;

namespace ContainerFixtures
{
    public class AutofacContainerFixture : ContainerFixture
    {
        private bool _disposed;
        private readonly IContainer _container;
        private readonly IServiceScope _scope;

        private class HttpContextAccessor : IHttpContextAccessor
        {
            readonly HttpContext _context;

            public HttpContextAccessor(HttpContext context)
            {
                _context = context;
            }

            public HttpContext? HttpContext
            {
                get => _context;
                set => throw new NotImplementedException();
            }
        }

        public AutofacContainerFixture()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<CycleServiceA>().SingleInstance();
            builder.RegisterType<CycleServiceB>().SingleInstance();
            builder.RegisterType<CycleServiceC>().SingleInstance();
            builder.RegisterType<StandardServiceA>().InstancePerDependency();
            builder.RegisterType<StandardServiceB>().InstancePerDependency();
            builder.RegisterType<StandardServiceC>().InstancePerDependency();
            builder.RegisterType<StandardService>().InstancePerDependency();
            builder.RegisterResolverFactory();

            _container = builder.Build();
            _scope = _container.Resolve<ILifetimeScope>().ToServiceScope();
        }

        public override IServiceProvider ServiceProvider => _scope.ServiceProvider;

        public override IServiceScope CreateScope()
        {
            return _container.BeginLifetimeScope().ToServiceScope();
        }

        public override ResolverFactory.ResolverFactory CreateFactory()
        {
            return new ResolverFactoryForAutofac(_container.Resolve<ILifetimeScope>());
        }

        public override void Dispose()
        {
            if (!_disposed)
            {
                _scope.Dispose();
                _disposed = true;
            }
        }
    }
}