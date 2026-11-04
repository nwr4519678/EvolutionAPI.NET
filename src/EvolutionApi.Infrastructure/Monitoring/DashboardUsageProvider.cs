using System.Data.Common;
using System.Diagnostics;
using EvolutionApi.Application.Common.Monitoring;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Management;

namespace EvolutionApi.Infrastructure.Monitoring;

public sealed class DashboardUsageProvider(AppDbContext db, ProcessCpuSampler cpuSampler) : IDashboardUsageProvider
{
    public async Task<DashboardUsageSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var clients = await ReadClientUsageAsync(cancellationToken);
        var databaseBytes = await ReadDatabaseAllocatedBytesAsync(cancellationToken);
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var privateWorkingSetBytes = TryReadPrivateWorkingSetBytes(process.Id);

        return new DashboardUsageSnapshot(
            DateTime.UtcNow,
            cpuSampler.SamplePercent(),
            process.WorkingSet64,
            privateWorkingSetBytes ?? process.WorkingSet64,
            privateWorkingSetBytes.HasValue,
            process.PrivateMemorySize64,
            GC.GetTotalMemory(forceFullCollection: false),
            GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            databaseBytes,
            clients);
    }

    private static long? TryReadPrivateWorkingSetBytes(int processId)
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT WorkingSetPrivate FROM Win32_PerfFormattedData_PerfProc_Process WHERE IDProcess = {processId}");
            using var results = searcher.Get();
            foreach (ManagementObject row in results)
            {
                using (row)
                {
                    var value = row["WorkingSetPrivate"];
                    if (value is not null) return Convert.ToInt64(value);
                }
            }
        }
        catch (Exception)
        {
            // WMI is best-effort; keep the usage endpoint available and fall back
            // to the process working set on hosts where the counter is unavailable.
        }

        return null;
    }

    private async Task<long> ReadDatabaseAllocatedBytesAsync(CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var closeAfter = connection.State != System.Data.ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CAST(COALESCE(SUM(size), 0) AS bigint) * 8192 FROM sys.database_files";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null or DBNull ? 0 : Convert.ToInt64(result);
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }

    private async Task<IReadOnlyList<ClientUsageSnapshot>> ReadClientUsageAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COALESCE(t.Id, i.Id) AS ClientId,
                   COALESCE(t.Name, i.Name) AS ClientName,
                   CAST(1 AS bigint) AS InstanceCount,
                   CAST(CASE WHEN LOWER(i.ConnectionStatus) IN ('open', 'connected') THEN 1 ELSE 0 END AS bigint) AS ConnectedInstances,
                   (SELECT COUNT_BIG(*) FROM dbo.Message m WHERE m.InstanceId = i.Id) AS StoredMessages,
                   (SELECT COUNT_BIG(*) FROM dbo.Message m WHERE m.InstanceId = i.Id AND m.Timestamp >= DATEADD(day, -1, SYSUTCDATETIME())) AS RecentMessages,
                   (SELECT COUNT_BIG(*) FROM dbo.SessionAuth s WHERE s.InstanceId = i.Id) AS SessionRecords,
                   COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(s.SessionKey), 0) + COALESCE(DATALENGTH(s.EncryptedJson), 0) AS bigint)) FROM dbo.SessionAuth s WHERE s.InstanceId = i.Id), 0)
                     + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(m.Id), 0) + COALESCE(DATALENGTH(m.RemoteJid), 0) + COALESCE(DATALENGTH(m.MessageJson), 0) AS bigint)) FROM dbo.Message m WHERE m.InstanceId = i.Id), 0)
                     + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(c.Id), 0) + COALESCE(DATALENGTH(c.Name), 0) + COALESCE(DATALENGTH(c.LastMessageJson), 0) AS bigint)) FROM dbo.Chat c WHERE c.InstanceId = i.Id), 0)
                     + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(c.Id), 0) + COALESCE(DATALENGTH(c.PushName), 0) + COALESCE(DATALENGTH(c.VerifiedName), 0) + COALESCE(DATALENGTH(c.ProfilePicUrl), 0) AS bigint)) FROM dbo.Contact c WHERE c.InstanceId = i.Id), 0)
                     + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(k.Category), 0) + COALESCE(DATALENGTH(k.KeyId), 0) + DATALENGTH(k.KeyData) AS bigint)) FROM dbo.SignalKey k WHERE k.InstanceId = i.Id), 0)
                     + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(o.Operation), 0) + COALESCE(DATALENGTH(o.PayloadJson), 0) + COALESCE(DATALENGTH(o.ResultJson), 0) AS bigint)) FROM dbo.OutboundMessage o WHERE o.InstanceId = i.Id), 0) AS EstimatedDatabaseBytes
            FROM dbo.Instance i
            LEFT JOIN dbo.Tenant t ON t.Id = i.TenantId
            ORDER BY ClientName
            """;

        var connection = db.Database.GetDbConnection();
        var closeAfter = connection.State != System.Data.ConnectionState.Open;
        if (closeAfter) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<ClientUsageSnapshot>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new ClientUsageSnapshot(
                    reader.GetString(0),
                    reader.GetString(1),
                    checked((int)reader.GetInt64(2)),
                    checked((int)reader.GetInt64(3)),
                    reader.GetInt64(4),
                    reader.GetInt64(5),
                    reader.GetInt64(6),
                    reader.GetInt64(7)));
            }
            return rows
                .GroupBy(x => x.ClientId, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ClientUsageSnapshot(
                    group.Key,
                    group.First().ClientName,
                    group.Sum(x => x.InstanceCount),
                    group.Sum(x => x.ConnectedInstances),
                    group.Sum(x => x.StoredMessages),
                    group.Sum(x => x.RecentMessages),
                    group.Sum(x => x.SessionRecords),
                    group.Sum(x => x.EstimatedDatabaseBytes)))
                .ToArray();
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }
}
