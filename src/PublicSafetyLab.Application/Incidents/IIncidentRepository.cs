using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Incidents;

namespace PublicSafetyLab.Application.Incidents;

public interface IIncidentRepository
{
    Task SaveAsync(Incident incident, CancellationToken cancellationToken);

    Task<Incident?> GetAsync(string tenantId, Guid incidentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Incident>> ListAsync(
        string tenantId,
        IncidentStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken);
}
