namespace EvolutionApi.Application.Common.Monitoring;

public interface IDashboardUsageProvider
{
    Task<DashboardUsageSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed record DashboardUsageSnapshot(
    DateTime SampledAtUtc,
    double? ProcessCpuPercent,
    long ProcessWorkingSetBytes,
    long DatabaseAllocatedBytes,
    IReadOnlyList<ClientUsageSnapshot> Clients);

public sealed record ClientUsageSnapshot(
    string InstanceId,
    string ClientId,
    string? WhatsAppNumber,
    string ConnectionStatus,
    long StoredMessages,
    long SessionRecords,
    long EstimatedDatabaseBytes);
