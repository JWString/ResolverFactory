using Moq;
using Xunit;
using Services;

namespace ResolverFactoryTests
{

    public class UnitTests
    {
        IServiceProvider ServiceProvider { get; set; }

        public UnitTests()
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            contextAccessor.Setup(m => m.HttpContext).Returns(httpContext);

            ServiceProvider = new ServiceCollection()
                .AddSingleton(contextAccessor.Object)
                .AddSingleton<CycleServiceA>()
                .AddSingleton<CycleServiceB>()
                .AddSingleton<CycleServiceC>()
                .AddTransient<StandardServiceA>()
                .AddTransient<StandardServiceB>()
                .AddTransient<StandardServiceC>()
                .AddTransient<StandardService>()
                .AddResolverFactoryForAspNet6()
                .AddResolverForService<CycleServiceA>()
                .AddResolverForService<CycleServiceB>()
                .AddResolverForService<CycleServiceC>()
                .AddResolverForService<StandardServiceA>()
                .AddResolverForService<StandardServiceB>()
                .AddResolverForService<StandardServiceC>()
                .AddResolverForService<StandardService>()
                .BuildServiceProvider();

            httpContext.RequestServices = ServiceProvider;           
        }

        [Fact]
        public void Resolves()
        {
            var resolver = ServiceProvider.GetRequiredService<IResolver<StandardService>>();
            Assert.Equal("StandardService", resolver.Resolve(s => s.Value));

            var resolver2 = ServiceProvider.GetRequiredService<IResolver<StandardServiceA>>();
            Assert.Equal("StandardServiceB", resolver2.Resolve(s => s.Test()));

            var r1 = ServiceProvider.GetRequiredService<IResolver<StandardServiceA>>();
            var r2 = ServiceProvider.GetRequiredService<IResolver<StandardServiceB>>();
            var r3 = ServiceProvider.GetRequiredService<IResolver<StandardServiceC>>();
            var result = r1.Resolve(s1 => s1.Test());
            result += " -> " + r2.Resolve(s2 => s2.Test());
            result += " -> " + r3.Resolve(s3 => s3.Test());

            Assert.Equal("StandardServiceB -> StandardServiceC -> StandardServiceA", result);
        }

        [Fact]
        public void DetectsCycles()
        {
            Exception? ex = null;

            try
            {
                var resolver = ServiceProvider.GetRequiredService<IResolver<CycleServiceA>>();
                resolver.Resolve(s => s.Test());
            }
            catch (InvalidOperationException caught)
            {
                ex = caught;
            }

            Assert.NotNull(ex);            
            Assert.IsType<InvalidOperationException>(ex);
            ex = null;

            try
            {
                var r1 = ServiceProvider.GetRequiredService<IResolver<StandardServiceA>>();
                var r2 = ServiceProvider.GetRequiredService<IResolver<StandardServiceB>>();
                var r3 = ServiceProvider.GetRequiredService<IResolver<StandardServiceC>>();
                r1.Resolve(s1 =>
                {
                    r2.Resolve(s2 =>
                    {
                        r3.Resolve(s3 =>
                        {
                            s3.Test();
                        });
                    });
                });
            }
            catch (InvalidOperationException caught)
            {
                ex = caught;
            }

            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        public void SharesScope()
        {
            var r1 = ServiceProvider.GetRequiredService<IResolver<StandardServiceA>>();
            var r2 = ServiceProvider.GetRequiredService<IResolver<StandardServiceB>>();
            var r3 = ServiceProvider.GetRequiredService<IResolver<StandardServiceC>>();
            var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            bool s1Disposed = false;
            bool s2Disposed = false;
            bool s3Disposed = false;

            var scope = scopeFactory.CreateScope();

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
        public void ManagesScopeAndDisposes()
        {
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            contextAccessorMock.Setup(m => m.HttpContext).Returns(null as HttpContext);
            var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
            var factory = new ResolverFactoryForAspNet6(contextAccessorMock.Object, scopeFactory);

            var r1 = factory.CreateResolver<StandardServiceA>();
            var r2 = factory.CreateResolver<StandardServiceB>();
            var r3 = factory.CreateResolver<StandardServiceC>();

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
        public async void Parallelizes()
        {
            var r1 = ServiceProvider.GetRequiredService<IResolver<StandardServiceA>>();
            var r2 = ServiceProvider.GetRequiredService<IResolver<StandardServiceB>>();
            var r3 = ServiceProvider.GetRequiredService<IResolver<StandardServiceC>>();
            var scopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            int s1DisposedCount = 0;
            int s2DisposedCount = 0;
            int s3DisposedCount = 0;

            var threads = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();
            var scope = scopeFactory.CreateScope();

            for (int i = 0; i < 1000; i++)
            {
                await r1.Resolve(s1 => Task.Run(async () =>
                {
                    var c = threads.GetOrAdd(Environment.CurrentManagedThreadId, 0);
                    threads.TryUpdate(Environment.CurrentManagedThreadId, c + 1, c);
                    s1.OnDispose = () => { s1DisposedCount++; };
                    await r2.Resolve(s2 => Task.Run(async () =>
                    {
                        var c = threads.GetOrAdd(Environment.CurrentManagedThreadId, 0);
                        threads.TryUpdate(Environment.CurrentManagedThreadId, c + 1, c);
                        s2.OnDispose = () => { s2DisposedCount++; };
                        await r3.Resolve(s3 => Task.Run(() =>
                        {
                            var c = threads.GetOrAdd(Environment.CurrentManagedThreadId, 0);
                            threads.TryUpdate(Environment.CurrentManagedThreadId, c + 1, c);
                            s3.OnDispose = () => { s3DisposedCount++; };
                        }), scope);
                    }), scope);
                }), scope);
            }

            scope.Dispose();

            Assert.True(threads.Count > 1);
            Assert.True(threads.Values.All(c => c > 0));
            Assert.Equal(1000, s1DisposedCount);
            Assert.Equal(1000, s2DisposedCount);
            Assert.Equal(1000, s3DisposedCount);
        }
    }
}