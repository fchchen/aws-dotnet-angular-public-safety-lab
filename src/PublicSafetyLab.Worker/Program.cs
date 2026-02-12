using PublicSafetyLab.Application.Common;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Infrastructure.DependencyInjection;
using PublicSafetyLab.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IncidentService>();
builder.Services.AddPublicSafetyInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
