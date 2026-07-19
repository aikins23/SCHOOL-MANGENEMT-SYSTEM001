using KingdomPrep.Web.Api;
using System;
using Xunit;

namespace KingdomPrep.Web.Tests;

public class SyncHealthServiceTests
{
    [Fact]
    public void ComputeHealthStatus_NoActiveDevices()
    {
        var status = SyncHealthService.ComputeHealthStatus(0, DateTime.UtcNow, 0, DateTime.UtcNow);

        Assert.Equal("No active devices", status);
    }

    [Fact]
    public void ComputeHealthStatus_ConflictsNeedAttention()
    {
        var now = DateTime.UtcNow;

        var status = SyncHealthService.ComputeHealthStatus(1, now, 2, now);

        Assert.Equal("Needs attention", status);
    }

    [Fact]
    public void ComputeHealthStatus_StaleWhenLastSeenOlderThanTwoDays()
    {
        var now = DateTime.UtcNow;

        var status = SyncHealthService.ComputeHealthStatus(1, now.AddDays(-3), 0, now);

        Assert.Equal("Stale", status);
    }

    [Fact]
    public void ComputeHealthStatus_HealthyForRecentDeviceContact()
    {
        var now = DateTime.UtcNow;

        var status = SyncHealthService.ComputeHealthStatus(1, now.AddHours(-3), 0, now);

        Assert.Equal("Healthy", status);
    }
}
