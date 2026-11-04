using Microsoft.AspNetCore.Components;

namespace EvolutionApi.Dashboard.Services;

/// <summary>
/// Global application state for the dashboard. Provides auth state,
/// theme preference, current tenant context, and notification counts.
/// </summary>
public sealed class AppState
{
    // ── Auth ─────────────────────────────────────────────────────────
    public bool IsAuthenticated { get; private set; }
    public string? CurrentUserId { get; private set; }
    public string? CurrentUserEmail { get; private set; }
    public string? CurrentUserRole { get; private set; }

    // ── Theme ────────────────────────────────────────────────────────
    public bool IsDarkMode { get; private set; } = true;

    // ── Tenant Context (for SuperAdmin impersonation) ─────────────────
    public TenantDto? SelectedTenant { get; private set; }
    public bool IsSuperAdmin => CurrentUserRole == "SuperAdmin";

    // ── Notifications ─────────────────────────────────────────────────
    public int UnreadNotificationCount { get; private set; }

    // ── Sidebar ───────────────────────────────────────────────────────
    public bool SidebarCollapsed { get; private set; }
    public bool MobileNavigationOpen { get; private set; }

    // ── Events ────────────────────────────────────────────────────────
    public event Action? OnChange;

    public void SetAuthenticated(string userId, string email, string role)
    {
        IsAuthenticated = true;
        CurrentUserId = userId;
        CurrentUserEmail = email;
        CurrentUserRole = role;
        NotifyChange();
    }

    public void SetUnauthenticated()
    {
        IsAuthenticated = false;
        CurrentUserId = null;
        CurrentUserEmail = null;
        CurrentUserRole = null;
        SelectedTenant = null;
        NotifyChange();
    }

    public void SetTheme(bool isDark)
    {
        IsDarkMode = isDark;
        NotifyChange();
    }

    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        NotifyChange();
    }

    public void SelectTenant(TenantDto? tenant)
    {
        SelectedTenant = tenant;
        NotifyChange();
    }

    public void SetUnreadNotifications(int count)
    {
        UnreadNotificationCount = count;
        NotifyChange();
    }

    public void ToggleSidebar()
    {
        SidebarCollapsed = !SidebarCollapsed;
        NotifyChange();
    }

    public void ToggleMobileNavigation()
    {
        MobileNavigationOpen = !MobileNavigationOpen;
        if (MobileNavigationOpen) SidebarCollapsed = false;
        NotifyChange();
    }

    public void CloseMobileNavigation()
    {
        if (!MobileNavigationOpen) return;
        MobileNavigationOpen = false;
        NotifyChange();
    }

    public void SetSidebarCollapsed(bool collapsed)
    {
        SidebarCollapsed = collapsed;
        NotifyChange();
    }

    private void NotifyChange() => OnChange?.Invoke();
}

