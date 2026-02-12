using System.Collections.Concurrent;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Incidents;

namespace PublicSafetyLab.Infrastructure.Incidents;

public sealed class InMemoryIncidentRepository : IIncidentRepository
{
    private readonly ConcurrentDictionary<string, IncidentSnapshot> _store = new();

    public Task SaveAsync(Incident incident, CancellationToken cancellationToken)
    {
        var snapshot = incident.ToSnapshot();
        _store[BuildKey(snapshot.TenantId, snapshot.IncidentId)] = snapshot;
        return Task.CompletedTask;
    }

    public Task<Incident?> GetAsync(string tenantId, Guid incidentId, CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(BuildKey(tenantId, incidentId), out var snapshot))
        {
            return Task.FromResult<Incident?>(Incident.FromSnapshot(snapshot));
        }

        return Task.FromResult<Incident?>(null);
    }

    public Task<IReadOnlyList<Incident>> ListAsync(
        string tenantId,
        IncidentStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = _store.Values
            .Where(x => x.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase));

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= to.Value);
        }

        var incidents = query
            .Select(Incident.FromSnapshot)
            .ToArray();

        return Task.FromResult<IReadOnlyList<Incident>>(incidents);
    }

    private static string BuildKey(string tenantId, Guid incidentId) => $"{tenantId}:{incidentId}";
}
