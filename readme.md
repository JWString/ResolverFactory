# Resolver Factory

## Purpose

`ResolverFactory` is an implementation of an experimental IoC pattern that enables components which leverage it to offload functionality pertaining to dependencies to an external component for execution and resolution when required, safely and with cycle prevention.

## Motivations

* Rigid adherence to typical constructor injection leads to component lifetime conflicts, which are responsible for:

  * Captured disposable transient dependencies resulting in memory leaks
  * Scoped dependencies as singleton instances
  * Captive dependencies

* Setter injection in singleton services can undermine thread safety.  
* Service location hides dependencies.
* Injected factories can produce dependencies which are not managed by an IoC container.

`ResolverFactory` is intended as a solution to problems related to all of the above.  Rather than by relying on dependencies injected at service instantiation, or by fetching dependencies as needed via a common service locator dependency, a service offloads code which depends on unresolved dependencies to an external `Resolver` to handle everything accordingly, when it is required.  

## Intended Use

#### Configuring `ResolverFactory`

Simply add a call to the `AddResolverFactory()` extension method to your `IServiceCollection` configuration at app startup.  Any additional dependencies managed by the IoC container should be available as `IResolver<TService>`.

#### Injecting dependency-specific resolvers:
```c#
 public class MyService
 {

      IResolver<ServiceA> rServiceA;
      IResolver<ServiceB> rServiceB;

      public MyService(IResolver<ServiceA> rServiceA, IResolver<ServiceB> rServiceB)
      {
           this.rServiceA = rServiceA;
           this.rServiceB = rServiceB;
      }

 }
```

#### Recommended use of `IResolver`

In most cases, an `IResolver` should be used as in the following example:

```c#
var result = rServiceA.Resolve(serviceA => {

     var records = rDbContext.Resolve(dbContext =>
          dbConext.Records
               .Where(r => r.Modified)
               .ToList()
     );
  
     var serviceBResult = rServiceB.Resolve(serviceB => serviceB.DoWork(records));
  
     return serviceA.ConsumeResult(serviceBResult);
  
});
```
An `IResolver` will attempt to locate the current `HttpContext` and its related service container if one is available.  If an `HttpContext` is not available, such as in cases of a deferred task running outside of the context of an http request, and if a scope is not provided via optional argument, an `IServiceScope` will be created for the scope of the outermost `onResolved` callback.  

However, any scope created for dependency resolution will not be automatically shared among resolves across threads, unless all resolves are supplied with a single, externally managed `IServiceScope` via the optional parameter.  

## Recommended Refactorings

Consider the following simplified example...

**When a scoped service needs a singleton (as nature intended):**
```c#
var result = rService.Resolve(service => service.GetResult());
```

When making any of the following changes to any service that the above depends on either directly or indirectly, the previously mentioned code should be changed as follows...

**After the service is changed to a singleton and its dependency to a scoped service:**
```c#
var result = rService.Resolve(service => service.GetResult());
```
**AFter the lifetime of any dependency of the service's dependency is changed:**
```c#
var result = rService.Resolve(service => service.GetResult());
```
**After the original code is migrated to a deferred task running outside of the context of an http request:**
```c#
var result = rService.Resolve(service => service.GetResult());
```
**After changing your IoC container:**
```c#
var result = rService.Resolve(service => service.GetResult());
```
By now it should obvious that the pattern, if applied as intended and if leveraged to a sufficient extent, enables the lifetime of services to be reconfigured without breaking the services that depend on them.  