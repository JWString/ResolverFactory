using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Diagnostics;

namespace ResolverFactory.SimpleInjector
{
    public static class Extensions
    {
        public static void RegisterResolverFactory(this Container container)
        {
            container.Register<ResolverFactory, ResolverFactoryForSimpleInjector>(Lifestyle.Singleton);
        }

        public static void RegisterWithResolver<TService>(this Container container, Lifestyle lifestyle)
        where TService : class
        {
            container.Register<TService>(lifestyle);
            var serviceRegistration = container.GetRegistration<TService>(true)!.Registration;

            container.Register<IResolver<TService>>(() => container.GetInstance<ResolverFactory>().CreateResolver<TService>(), Lifestyle.Transient);
            var resolverRegistration = container.GetRegistration<IResolver<TService>>(true)!.Registration;

            serviceRegistration.SuppressDiagnosticWarning(DiagnosticType.LifestyleMismatch, "ResolverFactory is designed to eliminate lifetime conflicts.");
            resolverRegistration.SuppressDiagnosticWarning(DiagnosticType.LifestyleMismatch, "ResolverFactory is designed to eliminate lifetime conflicts.");
            serviceRegistration.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "ResolverFactory handles disposal of transients.");
            resolverRegistration.SuppressDiagnosticWarning(DiagnosticType.DisposableTransientComponent, "ResolverFactory handles disposal of transients.");
        }

        public static IServiceScope ToServiceScope(this Scope scope)
        {
            return new ScopeAdapter(scope);
        }

        public static bool TypeIsDisposableTransient(this Scope scope, Type type)
        {
            var isTransient = scope.Container!.GetRegistration(type)!.Lifestyle.Name == Lifestyle.Transient.Name;
            var isDisposable = type.GetInterface("IDisposable") != null;
            return isTransient && isDisposable;
        }
    }
}