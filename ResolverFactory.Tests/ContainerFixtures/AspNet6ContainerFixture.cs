using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ResolverFactory.AspNet6;
using Services;

namespace ContainerFixtures
{
    public class StandardContainerFixture : ContainerFixture
    {
        private readonly IServiceProvider _serviceProvider;

        public StandardContainerFixture()
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            contextAccessor.Setup(m => m.HttpContext).Returns(httpContext);

            _serviceProvider = new ServiceCollection()
                .AddSingleton(contextAccessor.Object)
                .AddSingleton<CycleServiceA>()
                .AddSingleton<CycleServiceB>()
                .AddSingleton<CycleServiceC>()
                .AddTransient<StandardServiceA>()
                .AddTransient<StandardServiceB>()
                .AddTransient<StandardServiceC>()
                .AddTransient<StandardService>()
                .AddResolverFactoryForAspNet6()
                .BuildServiceProvider();

            httpContext.RequestServices = ServiceProvider;
        }

        public override IServiceProvider ServiceProvider => _serviceProvider;

        public override IServiceScope CreateScope()
        {
            return _serviceProvider.CreateScope();
        }

        public override ResolverFactory.ResolverFactory CreateFactory()
        {
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            contextAccessorMock.Setup(m => m.HttpContext).Returns(null as HttpContext);
            var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            return new ResolverFactoryForAspNet6(contextAccessorMock.Object, scopeFactory);
        }

        public override void Dispose() { }
    }
}