using System;
using System.IO;
using System.Text.Json;

namespace EquipmentBorrowing.Infrastructure.Persistence;

public static class DatabaseConfiguration
{
    public const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=equipment_borrowing;Username=postgres;Password=password";

    static DatabaseConfiguration()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public static string GetConnectionString()
    {
        // 1. Environment variable
        var envConn = Environment.GetEnvironmentVariable("EQUIPMENT_BORROWING_DB_CONN")
                   ?? Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConn))
            return envConn;

        // 2. Local appsettings.json if present
        try
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("EquipmentBorrowingDb", out var connStr))
                {
                    var val = connStr.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                        return val;
                }
            }
        }
        catch
        {
            // Fallback
        }

        // 3. Fallback local development connection
        return DefaultConnectionString;
    }
}
