using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PublicSafetyLab.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptionsMonitor<ApiKeyAuthenticationOptions> apiKeyOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, loggerFactory, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string ApiKeyHeaderName = "X-Api-Key";
    public const string LegacyTenantHeaderName = "X-Tenant-Id";
    public const string TenantIdClaimType = "tenant_id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var options = apiKeyOptions.CurrentValue;

        if (Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyValues))
        {
            var apiKey = apiKeyValues.ToString().Trim();
            var match = options.ApiKeys
                .FirstOrDefault(x => x.Key.Equals(apiKey, StringComparison.Ordinal));

            if (match is null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
            }

            if (string.IsNullOrWhiteSpace(match.TenantId))
            {
                return Task.FromResult(AuthenticateResult.Fail("API key is not mapped to a tenant."));
            }

            return Task.FromResult(CreateSuccess(match.TenantId.Trim(), "api-key"));
        }

        if (options.AllowLegacyTenantHeader &&
            Request.Headers.TryGetValue(LegacyTenantHeaderName, out var tenantValues) &&
            !string.IsNullOrWhiteSpace(tenantValues))
        {
            return Task.FromResult(CreateSuccess(tenantValues.ToString().Trim(), "legacy-tenant-header"));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }

    private static AuthenticateResult CreateSuccess(string tenantId, string authenticationType)
    {
        var claims = new[]
        {
            new Claim(TenantIdClaimType, tenantId),
            new Claim(ClaimTypes.NameIdentifier, tenantId)
        };

        var identity = new ClaimsIdentity(claims, authenticationType);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}

public sealed class ApiKeyAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool AllowLegacyTenantHeader { get; set; } = true;

    public List<ApiKeyTenantMapping> ApiKeys { get; set; } = [];
}

public sealed class ApiKeyTenantMapping
{
    public string Key { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}
