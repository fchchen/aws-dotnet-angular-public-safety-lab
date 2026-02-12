using PublicSafetyLab.Api.Middleware;
using PublicSafetyLab.Application.Common;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IncidentService>();
builder.Services.AddPublicSafetyInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

public partial class Program;
