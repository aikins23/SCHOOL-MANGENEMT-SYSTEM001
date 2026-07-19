using KingdomPrep.Web.Data;

namespace KingdomPrep.Web.Security;

public sealed class HeadmasterTenantProvider(IPortalTenantContext tenantContext) : IHeadmasterTenantProvider
{
    public async Task<Guid?> GetSchoolIdAsync()
    {
        var tenant = await tenantContext.GetCurrentAsync();
        return tenant.SchoolId;
    }
}
