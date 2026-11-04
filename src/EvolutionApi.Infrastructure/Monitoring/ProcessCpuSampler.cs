using System.Diagnostics;

namespace EvolutionApi.Infrastructure.Monitoring;

public sealed class ProcessCpuSampler
{
    private readonly object _sync = new();
    private TimeSpan? _previousCpu;
    private DateTimeOffset? _previousSample;

    public double? SamplePercent()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var now = DateTimeOffset.UtcNow;
        var cpu = process.TotalProcessorTime;

        lock (_sync)
        {
            var previousCpu = _previousCpu;
            var previousSample = _previousSample;
            _previousCpu = cpu;
            _previousSample = now;
            if (previousCpu is null || previousSample is null) return null;

            var elapsed = (now - previousSample.Value).TotalMilliseconds;
            if (elapsed <= 0) return null;
            var used = (cpu - previousCpu.Value).TotalMilliseconds;
            return Math.Clamp(used / (elapsed * Math.Max(1, Environment.ProcessorCount)) * 100d, 0, 100);
        }
    }
}
