using TestFixtures;
using Microsoft.Extensions.DependencyInjection;
using ResolverFactory;
using Services;
using System.Collections.Concurrent;
using Xunit;

namespace Tests
{
    public abstract class ResolverTests : IDisposable
    {
        private bool _disposed;
        private readonly TestFixture _fixture;

        public ResolverTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _fixture.Dispose();
                _disposed = true;
            }
        }

        [Fact]
        public async void Resolves()
        {
            var app = _fixture.CreateWebApplication();
            var client = app.CreateClient();

            var result = await client.GetStringAsync("/StandardService");
            var resultA = await client.GetStringAsync("/StandardServiceA");
            var resultB = await client.GetStringAsync("/StandardServiceB");
            var resultC = await client.GetStringAsync("/StandardServiceC");

            Assert.Equal("StandardService", result);
            Assert.Equal("StandardServiceA", resultA);
            Assert.Equal("StandardServiceB", resultB);
            Assert.Equal("StandardServiceC", resultC);
        }

        [Fact]
        public async void DetectsCycles()
        {
            var app = _fixture.CreateWebApplication();
            var client = app.CreateClient();

            var response1 = await client.GetAsync("/Cycle1");
            var message1 = await response1.Content.ReadAsStringAsync();
            var response2 = await client.GetAsync("/Cycle2");
            var message2 = await response2.Content.ReadAsStringAsync();

            Assert.Equal(500, (int)response1.StatusCode);
            Assert.Equal("Cycle detected by ResolverFactory RegisterWithResolutionContext.", message1);
            Assert.Equal(500, (int)response2.StatusCode);
            Assert.Equal("Cycle detected by ResolverFactory RegisterWithResolutionContext.", message2);
        }

        [Fact]
        public void SharesScope()
        {
            var app = _fixture.CreateWebApplication();
            var r1 = app.Services.GetRequiredService<IResolver<StandardServiceA>>();
            var r2 = app.Services.GetRequiredService<IResolver<StandardServiceB>>();
            var r3 = app.Services.GetRequiredService<IResolver<StandardServiceC>>();

            bool s1Disposed = false;
            bool s2Disposed = false;
            bool s3Disposed = false;

            var scope = app.Services.CreateScope();

            var result = r1.Resolve(s1 =>
            {
                s1.OnDispose = () => { s1Disposed = true; };

                var result = s1.Value + " -> ";

                result += r2.Resolve(s2 =>
                {
                    s2.OnDispose = () => { s2Disposed = true; };

                    return s2.Value + " -> ";
                });

                result += r3.Resolve(s3 =>
                {
                    s3.OnDispose = () => { s3Disposed = true; };

                    return s3.Value;
                });

                return result;
            }, scope);

            scope.Dispose();

            Assert.Equal("StandardServiceA -> StandardServiceB -> StandardServiceC", result);
            Assert.True(s1Disposed);
            Assert.True(s2Disposed);
            Assert.True(s3Disposed);
        }

        [Fact]
        public void ManagesScope()
        {
            var app = _fixture.CreateWebApplication();
            var r1 = app.Services.GetRequiredService<IResolver<StandardServiceA>>();
            var r2 = app.Services.GetRequiredService<IResolver<StandardServiceB>>();
            var r3 = app.Services.GetRequiredService<IResolver<StandardServiceC>>();

            bool s1Disposed = false;
            bool s2Disposed = false;
            bool s3Disposed = false;

            var result = r1.Resolve(s1 =>
            {
                s1.OnDispose = () => { s1Disposed = true; };

                var result = s1.Value + " -> ";

                result += r2.Resolve(s2 =>
                {
                    s2.OnDispose = () => { s2Disposed = true; };

                    return s2.Value;
                });

                result += " -> ";

                result += r3.Resolve(s3 =>
                {
                    s3.OnDispose = () => { s3Disposed = true; };

                    return s3.Value;
                });

                return result;
            });

            Assert.Equal("StandardServiceA -> StandardServiceB -> StandardServiceC", result);
            Assert.True(s1Disposed);
            Assert.True(s2Disposed);
            Assert.True(s3Disposed);
        }

        [Fact]
        public async void UsesRequestLifetimeScope()
        {
            var app = _fixture.CreateWebApplication();
            var client = app.CreateClient();

            var result = await client.GetStringAsync("/ManagedByRequestLifetimeScope");
            var scopePersisted = bool.Parse(await client.GetStringAsync("/ScopedBoolean"));
            var count = int.Parse(await client.GetStringAsync("/DisposedCount"));

            Assert.Equal("StandardServiceA -> StandardServiceB -> StandardServiceC", result);
            Assert.False(scopePersisted);
            Assert.Equal(3, count);
        }

        [Fact]
        public async void Parallelizes()
        {
            var app = _fixture.CreateWebApplication();
            var r1 = app.Services.GetRequiredService<IResolver<StandardServiceA>>();
            var r2 = app.Services.GetRequiredService<IResolver<StandardServiceB>>();
            var r3 = app.Services.GetRequiredService<IResolver<StandardServiceC>>();
            int s1DisposedCount = 0;
            int s2DisposedCount = 0;
            int s3DisposedCount = 0;
            var threads = new ConcurrentDictionary<int, int>();
            var tasks = new ConcurrentStack<Task>();
            var scope = app.Services.CreateScope();

            for (int i = 0; i < 1000; i++)
            {
                tasks.Push(r1.Resolve(s1 => Task.Run(() =>
                {
                    var c = threads.GetOrAdd(Environment.CurrentManagedThreadId, 0);
                    threads.TryUpdate(Environment.CurrentManagedThreadId, c + 1, c);
                    s1.OnDispose = () => { s1DisposedCount++; };
                    tasks.Push(r2.Resolve(s2 => Task.Run(() =>
                    {
                        var c = threads.GetOrAdd(Environment.CurrentManagedThreadId, 0);
                        threads.TryUpdate(Environment.CurrentManagedThreadId, c + 1, c);
                        s2.OnDispose = () => { s2DisposedCount++; };
                        tasks.Push(r3.Resolve(s3 => Task.Run(() =>
                        {
                            var c = threads.GetOrAdd(Environment.CurrentManagedThreadId, 0);
                            threads.TryUpdate(Environment.CurrentManagedThreadId, c + 1, c);
                            s3.OnDispose = () => { s3DisposedCount++; };
                        }), scope));
                    }), scope));
                }), scope));
            }

            await Task.WhenAll(tasks);
            scope.Dispose();

            Assert.True(threads.Count > 1);
            Assert.Equal(3000, threads.Values.Sum());
            Assert.Equal(1000, s1DisposedCount);
            Assert.Equal(1000, s2DisposedCount);
            Assert.Equal(1000, s3DisposedCount);
        }
    }
}