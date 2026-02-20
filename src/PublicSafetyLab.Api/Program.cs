using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PublicSafetyLab.Api.Authentication;
using PublicSafetyLab.Api.Middleware;
using PublicSafetyLab.Infrastructure.Configuration;
using PublicSafetyLab.Infrastructure.DependencyInjection;
using PublicSafetyLab.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new RenderedCompactJsonFormatter());
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ApiKeyAuthenticationOptions>(builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddPublicSafetyInfrastructure(builder.Configuration);
builder.Services.AddPublicSafetyHealthChecks(builder.Configuration);

var app = builder.Build();

try
{
    if (app.Configuration.GetValue<bool>("Database:AutoMigrateOnStartup"))
    {
        using var scope = app.Services.CreateScope();
        var resourceOptions = scope.ServiceProvider.GetRequiredService<IOptions<AwsResourceOptions>>().Value;
        var provider = ServiceCollectionExtensions.ResolveStorageProvider(resourceOptions);

        if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PublicSafetyDbContext>();
            dbContext.Database.Migrate();
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ApiExceptionHandlingMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/healthz/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapControllers();

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
