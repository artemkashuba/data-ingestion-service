namespace DataIngestService.Infrastructure.Configuration;

public static class ConfigurationKeys
{
    public static class ConnectionStrings
    {
        public const string DefaultConnection = "DefaultConnection";
    }

    public static class Sections
    {
        public const string Ingestion = "Ingestion";
    }

    public static class Database
    {
        public const string ApplyMigrationsOnStartup = "Database:ApplyMigrationsOnStartup";
    }
}
