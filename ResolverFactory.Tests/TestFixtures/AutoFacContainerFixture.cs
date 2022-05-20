using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResolverFactory.AspNet6;
using Services;

namespace TestFixtures
{
    public class AutofacContainerFixture : TestFixture
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

            protected override IHost CreateHost(IHostBuilder builder)
            {
                builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
                return base.CreateHost(builder);
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