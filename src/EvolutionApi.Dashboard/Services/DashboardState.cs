namespace EvolutionApi.Dashboard.Services;

public sealed class DashboardState
{
    public bool IsManagerAuthenticated { get; private set; }
    public string? SelectedInstance { get; private set; }
    public event Action? Changed;

    public void SetManagerAuthenticated(bool value)
    {
        IsManagerAuthenticated = value;
        Changed?.Invoke();
    }

    public void SelectInstance(string? name)
    {
        SelectedInstance = name;
        Changed?.Invoke();
    }
}
