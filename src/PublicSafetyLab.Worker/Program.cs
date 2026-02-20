using PublicSafetyLab.Infrastructure.DependencyInjection;
using PublicSafetyLab.Worker;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddPublicSafetyInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

try
{
    var host = builder.Build();
    await host.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}
