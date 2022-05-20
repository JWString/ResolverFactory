using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Concurrent;

namespace TestFixtures
{

    public abstract class TestFixture : IDisposable
    {
        public abstract class WebApplication : WebApplicationFactory<Program> { }

        private bool _disposed;

        protected ConcurrentStack<WebApplication> Applications { get; private set; }

        public TestFixture()
        {
            Applications = new();
        }

        public abstract WebApplication CreateWebApplication();

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                foreach (var app in Applications)
                {
                    app.Dispose();
                }

                _disposed = true;
            }
        }
    }
}