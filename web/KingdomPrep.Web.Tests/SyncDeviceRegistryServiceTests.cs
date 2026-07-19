using KingdomPrep.Web.Api;
using KingdomPrep.Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KingdomPrep.Web.Tests;

public class SyncDeviceRegistryServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateSyncApiKey_RequiresValue(string? syncApiKey)
    {
        var message = SyncDeviceRegistryService.ValidateSyncApiKey(syncApiKey);

        Assert.Equal("A unique sync API key is required.", message);
    }

    [Fact]
    public void ValidateSyncApiKey_RejectsShortKeys()
    {
        var message = SyncDeviceRegistryService.ValidateSyncApiKey("short-key");

        Assert.Equal("Sync API key must be at least 32 characters.", message);
    }

    [Fact]
    public void ValidateSyncApiKey_RejectsSharedSyncKeyReuse()
    {
        var sharedKey = "shared-sync-key-with-more-than-32-characters";

        var message = SyncDeviceRegistryService.ValidateSyncApiKey(sharedKey, sharedSyncApiKey: sharedKey);

        Assert.Equal("Device sync API key cannot reuse the shared sync key.", message);
    }

    [Fact]
    public void ValidateSyncApiKey_RejectsProvisioningKeyReuse()
    {
        var provisioningKey = "provisioning-key-with-more-than-32-characters";

        var message = SyncDeviceRegistryService.ValidateSyncApiKey(provisioningKey, provisioningKey: provisioningKey);

        Assert.Equal("Device sync API key cannot reuse the provisioning key.", message);
    }

    [Fact]
    public void ValidateSyncApiKey_AcceptsStrongUniqueKey()
    {
        var message = SyncDeviceRegistryService.ValidateSyncApiKey("device-key-with-more-than-32-characters");

        Assert.Null(message);
    }

    [Fact]
    public async Task SetDeviceActiveAsync_TogglesRegisteredDeviceAuthorization()
    {
        using var database = await SqlServerTestDatabase.TryCreateAsync();
        if (database == null)
            return;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(database.ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        var config = new ConfigurationBuilder().Build();
        var service = new SyncDeviceRegistryService(db, config, NullLogger<SyncDeviceRegistryService>.Instance);
        var schoolId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var syncKey = "device-key-with-more-than-32-characters";

        var registration = await service.RegisterAsync(new SyncDeviceRegistrationRequest
        {
            SchoolId = schoolId,
            DeviceId = deviceId,
            DeviceName = "Accounts Office",
            SyncApiKey = syncKey,
            IsActive = true,
            LicenseStatus = "Active"
        }, CancellationToken.None);

        Assert.True(registration.Ok);

        var deactivate = await service.SetDeviceActiveAsync(schoolId, deviceId, false, "admin", CancellationToken.None);
        Assert.True(deactivate.Ok);

        var inactiveAuth = await service.AuthorizeAsync(schoolId, deviceId, syncKey, CancellationToken.None);
        Assert.False(inactiveAuth.Ok);
        Assert.Equal(StatusCodes.Status403Forbidden, inactiveAuth.StatusCode);
        Assert.Equal("This sync device is inactive.", inactiveAuth.Message);

        var activate = await service.SetDeviceActiveAsync(schoolId, deviceId, true, "admin", CancellationToken.None);
        Assert.True(activate.Ok);

        var activeAuth = await service.AuthorizeAsync(schoolId, deviceId, syncKey, CancellationToken.None);
        Assert.True(activeAuth.Ok);
        Assert.Equal("RegisteredDevice", activeAuth.Mode);
    }
}
