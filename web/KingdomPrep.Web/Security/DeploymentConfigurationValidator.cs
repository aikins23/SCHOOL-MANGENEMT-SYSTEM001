using KingdomPrep.Web.Api;
using Microsoft.Data.SqlClient;

namespace KingdomPrep.Web.Security;

public sealed class DeploymentConfigurationException(string message) : InvalidOperationException(message);

public static class DeploymentConfigurationValidator
{
    public static IReadOnlyList<string> Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var errors = new List<string>();

        if (environment.IsDevelopment())
        {
            return errors;
        }

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add("ConnectionStrings:Default is required.");
        }
        else if (UsesLocalDevelopmentDatabase(connectionString))
        {
            errors.Add("ConnectionStrings:Default must not use LocalDB or localhost in production.");
        }

        RequirePrefixedSecret(configuration, "Paystack:PublicKey", "pk_", errors);
        RequirePrefixedSecret(configuration, "Paystack:SecretKey", "sk_", errors);
        RequireMinimumLength(configuration, "Sync:ProvisioningKey", SyncDeviceRegistryService.MinimumSyncApiKeyLength, errors);

        if (bool.TryParse(configuration["Sync:AllowSharedApiKeyFallback"], out var sharedFallback) && sharedFallback)
        {
            errors.Add("Sync:AllowSharedApiKeyFallback must be false in production.");
        }

        if (bool.TryParse(configuration["Security:AllowLegacyPlainTextPasswords"], out var allowPlainText) && allowPlainText)
        {
            errors.Add("Security:AllowLegacyPlainTextPasswords must be false in production.");
        }

        if (!long.TryParse(configuration["Security:SyncUploadMaxBytes"], out var maxSyncUploadBytes)
            || maxSyncUploadBytes < 262144
            || maxSyncUploadBytes > 10485760)
        {
            errors.Add("Security:SyncUploadMaxBytes must be between 262144 and 10485760 bytes in production.");
        }

        var allowedHosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            errors.Add("AllowedHosts must be restricted in production.");
        }

        return errors;
    }

    public static void ThrowIfInvalid(IConfiguration configuration, IHostEnvironment environment)
    {
        var errors = Validate(configuration, environment);
        if (errors.Count > 0)
        {
            throw new DeploymentConfigurationException(
                "Production deployment configuration is invalid: " + string.Join(" ", errors));
        }
    }

    private static bool UsesLocalDevelopmentDatabase(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var dataSource = builder.DataSource ?? "";

            return dataSource.Contains("localdb", StringComparison.OrdinalIgnoreCase) ||
                   dataSource.Equals(".", StringComparison.OrdinalIgnoreCase) ||
                   dataSource.Equals("(local)", StringComparison.OrdinalIgnoreCase) ||
                   dataSource.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   dataSource.StartsWith("localhost\\", StringComparison.OrdinalIgnoreCase) ||
                   dataSource.StartsWith(".\\", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void RequirePrefixedSecret(IConfiguration configuration, string key, string prefix, List<string> errors)
    {
        var value = configuration[key]?.Trim();
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            errors.Add($"{key} must be configured.");
        }
    }

    private static void RequireMinimumLength(IConfiguration configuration, string key, int minimumLength, List<string> errors)
    {
        var value = configuration[key]?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length < minimumLength)
        {
            errors.Add($"{key} must be at least {minimumLength} characters.");
        }
    }
}
