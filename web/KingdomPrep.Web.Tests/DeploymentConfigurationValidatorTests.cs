using KingdomPrep.Web.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace KingdomPrep.Web.Tests;

public class DeploymentConfigurationValidatorTests
{
    [Fact]
    public void Validate_AllowsDevelopmentDefaults()
    {
        var config = NewConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=(localdb)\\MSSQLLocalDB;Database=Neat_Academy;Trusted_Connection=True",
            ["AllowedHosts"] = "*"
        });

        var errors = DeploymentConfigurationValidator.Validate(config, new TestEnvironment("Development"));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RejectsUnsafeProductionDefaults()
    {
        var config = NewConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=(localdb)\\MSSQLLocalDB;Database=Neat_Academy;Trusted_Connection=True",
            ["AllowedHosts"] = "*",
            ["Sync:AllowSharedApiKeyFallback"] = "true",
            ["Security:AllowLegacyPlainTextPasswords"] = "true",
            ["Security:SyncUploadMaxBytes"] = "0"
        });

        var errors = DeploymentConfigurationValidator.Validate(config, new TestEnvironment("Production"));

        Assert.Contains(errors, e => e.Contains("ConnectionStrings:Default", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("Paystack:PublicKey", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("Paystack:SecretKey", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("Sync:ProvisioningKey", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("AllowedHosts", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("AllowSharedApiKeyFallback", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("AllowLegacyPlainTextPasswords", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Contains("SyncUploadMaxBytes", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_AcceptsProductionConfiguration()
    {
        var config = NewConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=tcp:sql.example.com,1433;Database=Neat_Academy;User ID=app;Password=StrongPassword123;Encrypt=True;TrustServerCertificate=False",
            ["AllowedHosts"] = "school.example.com",
            ["Paystack:PublicKey"] = "pk_example_value_for_configuration_validation",
            ["Paystack:SecretKey"] = "sk_example_value_for_configuration_validation",
            ["Sync:ProvisioningKey"] = "provisioning-key-with-more-than-32-characters",
            ["Sync:AllowSharedApiKeyFallback"] = "false",
            ["Security:AllowLegacyPlainTextPasswords"] = "false",
            ["Security:SyncUploadMaxBytes"] = "2097152"
        });

        var errors = DeploymentConfigurationValidator.Validate(config, new TestEnvironment("Production"));

        Assert.Empty(errors);
    }

    [Fact]
    public void ThrowIfInvalid_ThrowsForInvalidProductionConfiguration()
    {
        var config = NewConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Server=localhost;Database=Neat_Academy;Trusted_Connection=True",
            ["AllowedHosts"] = "*"
        });

        Assert.Throws<DeploymentConfigurationException>(() =>
            DeploymentConfigurationValidator.ThrowIfInvalid(config, new TestEnvironment("Production")));
    }

    private static IConfiguration NewConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "KingdomPrep.Web.Tests";
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
