using Services;
using ResolverFactory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<ServiceA>();
builder.Services.AddSingleton<ServiceB>();
builder.Services.AddSingleton<ServiceC>();
builder.Services.AddResolverFactory<ServiceA>();
builder.Services.AddResolverFactory<ServiceB>();
builder.Services.AddResolverFactory<ServiceC>();
builder.Services.AddTransient<StandardServiceA>();
builder.Services.AddTransient<StandardServiceB>();
builder.Services.AddTransient<StandardServiceC>();

builder.Services.AddSingleton<NonCircular>();
builder.Services.AddTransient<IResolverFactory<NonCircular>, ResolverFactory<NonCircular>>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.Map("/resolved", (IResolverFactory<ServiceA> rServiceA, IResolverFactory<ServiceB> rServiceB, IResolverFactory<ServiceC> rServiceC) =>
{
    var messages = new string[]
    {
        rServiceA.CreateResolver(s => s.Test())(),
        rServiceB.CreateResolver(s => s.Test())(),
        rServiceC.CreateResolver(s => s.Test())()
    };

    return string.Join(" -> ", messages);
});

//app.Map("/circular", (StandardServiceA serviceA, StandardServiceB serviceB, StandardServiceC serviceC) =>
//{
//    var messages = new string[]
//    {
//        serviceA.Test(),
//        serviceB.Test(),
//        serviceC.Test()
//    };

//    return string.Join(" -> ", messages);
//});

app.Map("/noncircular", (IResolverFactory<NonCircular> rNonCircular) =>
{
    return rNonCircular.CreateResolver(s => s.Value)();
});


app.Run();
