namespace Common.Configuration
{
    public static class Constants
    {
        public static string ConfigConnectionStringName => "MindedExample";
    }

    public enum DatabaseType
    {
        SQLServer,
        LocalDb,
        SQLiteInMemory
    }
}
