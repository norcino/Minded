namespace MindedExample.Infrastructure.Configuration
{
    public static class Constants
    {
        public static string ConfigConnectionStringName => "MindedExample";

        /// <summary>
        /// Dedicated PostgreSQL connection string. When present it is preferred by the
        /// PostgreSQL database type, so switching provider only requires changing
        /// the DatabaseType configuration value. Falls back to <see cref="ConfigConnectionStringName"/>.
        /// </summary>
        public static string ConfigPostgreSqlConnectionStringName => "MindedExamplePostgreSQL";
    }

    public enum DatabaseType
    {
        SQLServer,
        LocalDb,
        SQLiteInMemory,
        PostgreSQL
    }
}