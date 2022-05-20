using Microsoft.AspNetCore.Mvc;
using ResolverFactory;
using Services;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("StandardService", ([FromServices] IResolver<StandardService> r) => r.Resolve((service) => service.Value));

app.MapGet("StandardServiceA", ([FromServices] IResolver<StandardServiceA> r) => r.Resolve((service) => service.Value));

app.MapGet("StandardServiceB", ([FromServices] IResolver<StandardServiceB> r) => r.Resolve((service) => service.Value));

app.MapGet("StandardServiceC", ([FromServices] IResolver<StandardServiceC> r) => r.Resolve((service) => service.Value));

app.MapGet("Cycle1", ([FromServices] IResolver<CycleServiceA> r, HttpContext context) =>
{
    try
    {
        return r.Resolve((service) => service.Test());
    }
    catch (InvalidOperationException ex)
    {
        context.Response.StatusCode = 500;
        return ex.Message;
    }
});

app.MapGet(
    "Cycle2",
    (
        [FromServices] IResolver<StandardServiceA> r1,
        [FromServices] IResolver<StandardServiceB> r2,
        [FromServices] IResolver<StandardServiceC> r3,
        HttpContext context
    ) =>
    {
        try
        {
            return r1.Resolve(s1 =>
            {
                return r2.Resolve(s2 =>
                {
                    return r3.Resolve(s3 =>
                    {
                        return s3.Test();
                    });
                });
            });
        }
        catch (InvalidOperationException ex)
        {
            context.Response.StatusCode = 500;
            return ex.Message;
        }
    }
);

app.MapGet(
    "ManagedByRequestLifetimeScope",
    (
        [FromServices] IResolver<StandardServiceA> r1,
        [FromServices] IResolver<StandardServiceB> r2,
        [FromServices] IResolver<StandardServiceC> r3,
        [FromServices] DisposeCounter counter
    ) =>
    {
        var result = r1.Resolve(s1 =>
        {
            s1.OnDispose = () => { counter.Count++; };
            return s1.Value + " -> " + r2.Resolve(s2 =>
            {
                s2.OnDispose = () => { counter.Count++; };
                return s2.Value + " -> " + r3.Resolve(s3 =>
                {
                    s3.OnDispose = () => { counter.Count++; };
                    return s3.Value;
                });
            });
        });

        return result;
    }
);

app.MapGet("DisposedCount", ([FromServices] DisposeCounter counter) =>
{
    return counter.Count;
});

app.Run();

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}
