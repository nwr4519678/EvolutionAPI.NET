namespace EvolutionApi.Dashboard.Services;

public sealed class DashboardState
{
    public string ApiKey { get; private set; } = string.Empty;
    public string? SelectedInstance { get; private set; }
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    public event Action? Changed;

    public void SetApiKey(string value)
    {
        ApiKey = value.Trim();
        Changed?.Invoke();
    }

    public void SelectInstance(string? name)
    {
        SelectedInstance = name;
        Changed?.Invoke();
    }
}
