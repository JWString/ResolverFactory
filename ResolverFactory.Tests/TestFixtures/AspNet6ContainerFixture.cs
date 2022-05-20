using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ResolverFactory.AspNet6;
using Services;

namespace TestFixtures
{
    public class StandardContainerFixture : TestFixture
    {
        private class LocalApplication : WebApplication
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureServices(services =>
                {
                    services
                        .AddHttpContextAccessor()
                        .AddSingleton<CycleServiceA>()
                        .AddSingleton<CycleServiceB>()
                        .AddSingleton<CycleServiceC>()
                        .AddTransient<StandardServiceA>()
                        .AddTransient<StandardServiceB>()
                        .AddTransient<StandardServiceC>()
                        .AddTransient<StandardService>()
                        .AddSingleton<DisposeCounter>()
                        .AddResolverFactory();
                });
            }
        }

        public override WebApplication CreateWebApplication()
        {
            var app = new LocalApplication();
            Applications.Push(app);
            return app;
        }
    }
}