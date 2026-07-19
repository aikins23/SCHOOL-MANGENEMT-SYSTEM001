using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace KingdomPrep.Web.Security;

public sealed record PortalTenant(
    bool IsAuthenticated,
    string? Username,
    string? Email,
    int? EmploymentId,
    Guid? SchoolId)
{
    public bool HasSchoolScope => SchoolId.HasValue;
}

public interface IPortalTenantContext
{
    Task<PortalTenant> GetCurrentAsync();
}

public sealed class PortalTenantContext(AuthenticationStateProvider authenticationStateProvider) : IPortalTenantContext
{
    public async Task<PortalTenant> GetCurrentAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var isAuthenticated = user.Identity?.IsAuthenticated == true;

        return new PortalTenant(
            isAuthenticated,
            user.FindFirst(ClaimTypes.Name)?.Value,
            user.FindFirst(ClaimTypes.Email)?.Value,
            ParseInt(user.FindFirst("EmploymentId")?.Value),
            ParseGuid(user.FindFirst("SchoolId")?.Value));
    }

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var result) ? result : null;

    private static Guid? ParseGuid(string? value)
        => Guid.TryParse(value, out var result) ? result : null;
}
