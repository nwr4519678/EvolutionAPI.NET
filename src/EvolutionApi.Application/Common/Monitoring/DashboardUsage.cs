namespace EvolutionApi.Application.Common.Monitoring;

public interface IDashboardUsageProvider
{
    Task<DashboardUsageSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed record DashboardUsageSnapshot(
    DateTime SampledAtUtc,
    double? ProcessCpuPercent,
    long ProcessWorkingSetBytes,
    long ProcessPrivateWorkingSetBytes,
    bool IsPrivateWorkingSetAvailable,
    long ProcessPrivateBytes,
    long ManagedHeapBytes,
    long AvailableMemoryBytes,
    long DatabaseAllocatedBytes,
    IReadOnlyList<ClientUsageSnapshot> Clients);

public sealed record ClientUsageSnapshot(
    string ClientId,
    string ClientName,
    int InstanceCount,
    int ConnectedInstances,
    long StoredMessages,
    long RecentMessages,
    long SessionRecords,
    long EstimatedDatabaseBytes);
