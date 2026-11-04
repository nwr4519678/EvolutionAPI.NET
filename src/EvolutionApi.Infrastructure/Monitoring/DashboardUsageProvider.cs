using System.Data.Common;
using System.Diagnostics;
using EvolutionApi.Application.Common.Monitoring;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Monitoring;

public sealed class DashboardUsageProvider(AppDbContext db, ProcessCpuSampler cpuSampler) : IDashboardUsageProvider
{
    public async Task<DashboardUsageSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var clients = await ReadClientUsageAsync(cancellationToken);
        var databaseBytes = await ReadDatabaseAllocatedBytesAsync(cancellationToken);
        using var process = Process.GetCurrentProcess();
        process.Refresh();

        return new DashboardUsageSnapshot(
            DateTime.UtcNow,
            cpuSampler.SamplePercent(),
            process.WorkingSet64,
            databaseBytes,
            clients);
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
            SELECT i.Id AS InstanceId,
                   i.Name AS ClientId,
                   i.Number AS WhatsAppNumber,
                   i.ConnectionStatus,
                   (SELECT COUNT_BIG(*) FROM dbo.Message m WHERE m.InstanceId = i.Id) AS StoredMessages,
                   (SELECT COUNT_BIG(*) FROM dbo.SessionAuth s WHERE s.InstanceId = i.Id) AS SessionRecords,
                   COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(s.SessionKey), 0) + COALESCE(DATALENGTH(s.EncryptedJson), 0) AS bigint)) FROM dbo.SessionAuth s WHERE s.InstanceId = i.Id), 0)
                   + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(m.Id), 0) + COALESCE(DATALENGTH(m.RemoteJid), 0) + COALESCE(DATALENGTH(m.MessageJson), 0) AS bigint)) FROM dbo.Message m WHERE m.InstanceId = i.Id), 0)
                   + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(c.Id), 0) + COALESCE(DATALENGTH(c.Name), 0) + COALESCE(DATALENGTH(c.LastMessageJson), 0) AS bigint)) FROM dbo.Chat c WHERE c.InstanceId = i.Id), 0)
                   + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(c.Id), 0) + COALESCE(DATALENGTH(c.PushName), 0) + COALESCE(DATALENGTH(c.VerifiedName), 0) + COALESCE(DATALENGTH(c.ProfilePicUrl), 0) AS bigint)) FROM dbo.Contact c WHERE c.InstanceId = i.Id), 0)
                   + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(k.Category), 0) + COALESCE(DATALENGTH(k.KeyId), 0) + DATALENGTH(k.KeyData) AS bigint)) FROM dbo.SignalKey k WHERE k.InstanceId = i.Id), 0)
                   + COALESCE((SELECT SUM(CAST(COALESCE(DATALENGTH(o.Operation), 0) + COALESCE(DATALENGTH(o.PayloadJson), 0) + COALESCE(DATALENGTH(o.ResultJson), 0) AS bigint)) FROM dbo.OutboundMessage o WHERE o.InstanceId = i.Id), 0)
                   AS EstimatedDatabaseBytes
            FROM dbo.Instance i
            ORDER BY i.Name
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
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt64(4),
                    reader.GetInt64(5),
                    reader.GetInt64(6)));
            }
            return rows;
        }
        finally
        {
            if (closeAfter) await connection.CloseAsync();
        }
    }
}
