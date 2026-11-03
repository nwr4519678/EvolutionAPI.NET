using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EvolutionApi.Dashboard.Models;

namespace EvolutionApi.Dashboard.Services;

public sealed class EvolutionApiClient(HttpClient http, DashboardState state)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public Uri BaseAddress => http.BaseAddress!;

    public async Task<IReadOnlyList<DashboardInstance>> GetInstancesAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "instance/fetchInstances", null, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<DashboardInstance>>(JsonOptions, ct) ?? [];
    }

    public async Task<CreateInstanceResponse> CreateInstanceAsync(CreateInstanceRequest request, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, "instance/create", JsonContent.Create(request), ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<CreateInstanceResponse>(JsonOptions, ct))!;
    }

    public async Task<ConnectResult> ConnectAsync(string instanceName, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, $"instance/connect/{Uri.EscapeDataString(instanceName)}", null, ct);
        await EnsureSuccessAsync(response, ct);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        return ConnectResult.FromJson(document.RootElement);
    }

    public async Task LogoutAsync(string instanceName, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"instance/logout/{Uri.EscapeDataString(instanceName)}", null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task DeleteAsync(string instanceName, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"instance/delete/{Uri.EscapeDataString(instanceName)}", null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<JsonElement> SendTextAsync(SendTextRequest request, string instanceName, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, $"message/sendText/{Uri.EscapeDataString(instanceName)}", JsonContent.Create(request), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, ct);
    }

    public async Task<DashboardHealth?> GetHealthAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("health/ready", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DashboardHealth>(JsonOptions, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (!string.IsNullOrWhiteSpace(state.ApiKey))
            request.Headers.TryAddWithoutValidation("apikey", state.ApiKey);
        return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct = default)
    {
        if (response.IsSuccessStatusCode) return;
        var detail = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(detail)) detail = response.ReasonPhrase ?? "The API request failed.";
        throw new HttpRequestException($"API returned {(int)response.StatusCode} ({response.ReasonPhrase}): {detail}", null, response.StatusCode);
    }
}
