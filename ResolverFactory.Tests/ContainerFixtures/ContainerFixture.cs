using Microsoft.Extensions.DependencyInjection;

namespace ContainerFixtures
{

    public abstract class ContainerFixture : IDisposable
    {
        public abstract IServiceProvider ServiceProvider { get; }

        public abstract IServiceScope CreateScope();

        public abstract ResolverFactory.ResolverFactory CreateFactory();

        public abstract void Dispose();
    }
}