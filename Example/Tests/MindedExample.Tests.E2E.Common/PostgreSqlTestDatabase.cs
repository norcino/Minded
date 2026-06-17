using System;
using System.Collections.Generic;
using Npgsql;

namespace MindedExample.Tests.E2E.Common
{
    /// <summary>
    /// Manages the per-run PostgreSQL database used by the E2E testing profile.
    /// Each test run works against a uniquely named database (base name + run id) so runs
    /// are isolated and re-runnable. Databases left behind by crashed runs are swept on
    /// first use, and the current run's database is dropped by the assembly cleanup hook.
    /// </summary>
    public static class PostgreSqlTestDatabase
    {
        private static readonly object s_lock = new object();
        private static string s_runConnectionString;

        /// <summary>Connection string from configuration, pointing at the base database name.</summary>
        public static string BaseConnectionString { get; private set; }

        /// <summary>Name of this run's database, e.g. 'minded_apitests_1a2b3c4d'.</summary>
        public static string RunDatabaseName { get; private set; }

        /// <summary>
        /// Returns the connection string for this run's unique database. On first call it
        /// also sweeps databases leaked by previous crashed runs (same base name prefix).
        /// </summary>
        public static string GetRunConnectionString(string baseConnectionString)
        {
            if (string.IsNullOrWhiteSpace(baseConnectionString))
                throw new InvalidOperationException(
                    "The PostgreSQL E2E profile requires a PostgreSQL connection string in testappsettings.json or the MINDEDTEST_ environment overrides.");

            lock (s_lock)
            {
                if (s_runConnectionString != null)
                    return s_runConnectionString;

                BaseConnectionString = baseConnectionString;

                var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
                var baseName = builder.Database;
                RunDatabaseName = $"{baseName}_{Guid.NewGuid():N}".Substring(0, baseName.Length + 9);
                builder.Database = RunDatabaseName;
                s_runConnectionString = builder.ConnectionString;

                DropDatabasesByPrefix(baseName + "_", excludeDatabase: RunDatabaseName);

                return s_runConnectionString;
            }
        }

        /// <summary>Drops this run's database. Safe to call when the profile was never used.</summary>
        public static void DropRunDatabase()
        {
            lock (s_lock)
            {
                if (BaseConnectionString == null || RunDatabaseName == null)
                    return;

                DropDatabasesByPrefix(RunDatabaseName, excludeDatabase: null);
            }
        }

        private static void DropDatabasesByPrefix(string prefix, string excludeDatabase)
        {
            var admin = new NpgsqlConnectionStringBuilder(BaseConnectionString) { Database = "postgres" };
            using var connection = new NpgsqlConnection(admin.ConnectionString);
            connection.Open();

            var names = new List<string>();
            using (var query = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datname LIKE @pattern", connection))
            {
                query.Parameters.AddWithValue("pattern", prefix + "%");
                using var reader = query.ExecuteReader();
                while (reader.Read())
                    names.Add(reader.GetString(0));
            }

            foreach (var name in names)
            {
                if (string.Equals(name, excludeDatabase, StringComparison.Ordinal))
                    continue;

                using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", connection);
                drop.ExecuteNonQuery();
            }
        }
    }
}
