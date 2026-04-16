namespace TrafficMonitor.Infrastructure.Configuration;

using System.Collections;
using Microsoft.Extensions.Configuration;

internal static class PostgresConnectionStringResolver
{
    private const string PostgresConnectionStringKey = "ConnectionStrings:Postgres";

    public static string Resolve(IConfiguration configuration, string contentRootPath)
    {
        var configuredConnectionString = configuration.GetConnectionString("Postgres");
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        return Resolve(LoadEnvironmentValues(contentRootPath));
    }

    public static string Resolve(IReadOnlyDictionary<string, string?> environmentValues)
    {
        ArgumentNullException.ThrowIfNull(environmentValues);

        if (environmentValues.TryGetValue("ConnectionStrings__Postgres", out var environmentConnectionString) &&
            !string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        if (environmentValues.TryGetValue("POSTGRES_DB", out var database) &&
            environmentValues.TryGetValue("POSTGRES_USER", out var username) &&
            environmentValues.TryGetValue("POSTGRES_PASSWORD", out var password))
        {
            return $"Host=localhost;Port=5432;Database={database};Username={username};Password={password}";
        }

        throw new InvalidOperationException($"{PostgresConnectionStringKey} is not configured.");
    }

    public static Dictionary<string, string?> LoadEnvironmentValues(string contentRootPath)
    {
        var repositoryRoot = FindRepositoryRoot(contentRootPath);
        var environmentFilePath = Path.Combine(repositoryRoot, ".env");
        var environmentValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
        {
            var key = environmentVariable.Key?.ToString();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            environmentValues[key] = environmentVariable.Value?.ToString();
        }

        if (!File.Exists(environmentFilePath))
        {
            return environmentValues;
        }

        foreach (var line in File.ReadAllLines(environmentFilePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            environmentValues[key] = value;
        }

        return environmentValues;
    }

    private static string FindRepositoryRoot(string contentRootPath)
    {
        var directoryInfo = new DirectoryInfo(contentRootPath);

        while (directoryInfo is not null)
        {
            if (File.Exists(Path.Combine(directoryInfo.FullName, "TrafficMonitor.slnx")))
            {
                return directoryInfo.FullName;
            }

            directoryInfo = directoryInfo.Parent;
        }

        return contentRootPath;
    }
}
