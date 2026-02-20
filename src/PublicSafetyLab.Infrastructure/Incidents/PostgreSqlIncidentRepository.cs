using Microsoft.EntityFrameworkCore;
using PublicSafetyLab.Application.Incidents;
using PublicSafetyLab.Contracts.Incidents;
using PublicSafetyLab.Domain.Incidents;
using PublicSafetyLab.Infrastructure.Persistence;
using PublicSafetyLab.Infrastructure.Persistence.Entities;

namespace PublicSafetyLab.Infrastructure.Incidents;

public sealed class PostgreSqlIncidentRepository(PublicSafetyDbContext dbContext) : IIncidentRepository
{
    public async Task SaveAsync(Incident incident, CancellationToken cancellationToken)
    {
        var snapshot = incident.ToSnapshot();

        var existing = await dbContext.Incidents
            .Include(x => x.EvidenceItems)
            .SingleOrDefaultAsync(
                x => x.Id == snapshot.IncidentId && x.TenantId == snapshot.TenantId,
                cancellationToken);

        if (existing is null)
        {
            dbContext.Incidents.Add(MapIncident(snapshot));
        }
        else
        {
            var expectedPreviousVersion = snapshot.Version - 1;
            if (expectedPreviousVersion < 1 || existing.Version != expectedPreviousVersion)
            {
                throw new DbUpdateConcurrencyException(
                    $"Incident {snapshot.IncidentId} for tenant {snapshot.TenantId} version mismatch. " +
                    $"Expected current version {expectedPreviousVersion}, found {existing.Version}.");
            }

            CopyIncident(snapshot, existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Incident?> GetAsync(string tenantId, Guid incidentId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Incidents
            .AsNoTracking()
            .Include(x => x.EvidenceItems)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == incidentId, cancellationToken);

        return entity is null
            ? null
            : MapDomain(entity);
    }

    public async Task<IReadOnlyList<Incident>> ListAsync(
        string tenantId,
        IncidentStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Incidents
            .AsNoTracking()
            .Include(x => x.EvidenceItems)
            .Where(x => x.TenantId == tenantId);

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

        var entities = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToArrayAsync(cancellationToken);
        return entities.Select(MapDomain).ToArray();
    }

    private static IncidentEntity MapIncident(IncidentSnapshot snapshot)
    {
        var entity = new IncidentEntity
        {
            Id = snapshot.IncidentId,
            TenantId = snapshot.TenantId,
            Title = snapshot.Title,
            Description = snapshot.Description,
            Priority = snapshot.Priority,
            Location = snapshot.Location,
            Status = snapshot.Status,
            CreatedAt = snapshot.CreatedAt,
            ReportedAt = snapshot.ReportedAt,
            QueuedAt = snapshot.QueuedAt,
            ProcessedAt = snapshot.ProcessedAt,
            FailureReason = snapshot.FailureReason,
            Version = snapshot.Version,
            EvidenceItems = []
        };

        for (var index = 0; index < snapshot.Evidence.Count; index++)
        {
            var evidence = snapshot.Evidence[index];
            entity.EvidenceItems.Add(new EvidenceItemEntity
            {
                Id = Guid.NewGuid(),
                IncidentId = snapshot.IncidentId,
                FileName = evidence.FileName,
                ObjectKey = evidence.ObjectKey,
                UploadedAt = evidence.UploadedAt,
                SortOrder = index
            });
        }

        return entity;
    }

    private static void CopyIncident(IncidentSnapshot snapshot, IncidentEntity entity)
    {
        entity.Title = snapshot.Title;
        entity.Description = snapshot.Description;
        entity.Priority = snapshot.Priority;
        entity.Location = snapshot.Location;
        entity.Status = snapshot.Status;
        entity.CreatedAt = snapshot.CreatedAt;
        entity.ReportedAt = snapshot.ReportedAt;
        entity.QueuedAt = snapshot.QueuedAt;
        entity.ProcessedAt = snapshot.ProcessedAt;
        entity.FailureReason = snapshot.FailureReason;
        entity.Version = snapshot.Version;

        var existingEvidenceByOrder = entity.EvidenceItems
            .OrderBy(x => x.SortOrder)
            .ToArray();

        for (var index = 0; index < snapshot.Evidence.Count; index++)
        {
            var evidence = snapshot.Evidence[index];
            var evidenceEntity = index < existingEvidenceByOrder.Length
                ? existingEvidenceByOrder[index]
                : new EvidenceItemEntity
                {
                    Id = Guid.NewGuid(),
                    IncidentId = entity.Id
                };

            evidenceEntity.FileName = evidence.FileName;
            evidenceEntity.ObjectKey = evidence.ObjectKey;
            evidenceEntity.UploadedAt = evidence.UploadedAt;
            evidenceEntity.SortOrder = index;
            evidenceEntity.IncidentId = entity.Id;

            if (index >= existingEvidenceByOrder.Length)
            {
                entity.EvidenceItems.Add(evidenceEntity);
            }
        }

        for (var index = snapshot.Evidence.Count; index < existingEvidenceByOrder.Length; index++)
        {
            entity.EvidenceItems.Remove(existingEvidenceByOrder[index]);
        }
    }

    private static Incident MapDomain(IncidentEntity entity)
    {
        var evidence = entity.EvidenceItems
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.UploadedAt)
            .Select(x => new EvidenceItem(
                FileName: x.FileName,
                ObjectKey: x.ObjectKey,
                UploadedAt: x.UploadedAt))
            .ToArray();

        return Incident.FromSnapshot(new IncidentSnapshot(
            IncidentId: entity.Id,
            TenantId: entity.TenantId,
            Title: entity.Title,
            Description: entity.Description,
            Priority: entity.Priority,
            Location: entity.Location,
            Status: entity.Status,
            CreatedAt: entity.CreatedAt,
            ReportedAt: entity.ReportedAt,
            QueuedAt: entity.QueuedAt,
            ProcessedAt: entity.ProcessedAt,
            FailureReason: entity.FailureReason,
            Evidence: evidence,
            Version: entity.Version));
    }
}
